using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Agent.Services;
using Surveil.Contracts;
using Surveil.Processing;
using Surveil.Utils;

namespace Surveil.Runner;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== SurveilWin Runner ===");
        Console.WriteLine("Press Ctrl+C to stop.\n");

        // -----------------------------------------------------------------------
        // Bootstrap services
        // -----------------------------------------------------------------------
        var cfgSvc    = new ConfigService();
        var cfg       = cfgSvc.Config;
        var capture   = new CaptureService();
        var ocr       = new OcrService();
        var act       = new ActivityService();
        var policy    = new PolicyService(cfg);
        var retention = new RetentionService(cfg);

        string modelPath = ResolveModelPath(cfg.ModelPath);
        using var embed = new EmbeddingService(modelPath);

        // Run startup retention cleanup.
        retention.Cleanup();

        // Summarizer with session ID and optional full-trace mode.
        var summarizer = new SlidingSummarizer(fullTrace: cfg.FullTraceMode);

        summarizer.OnSummary += summary =>
        {
            Directory.CreateDirectory(cfg.SessionsDir);
            string path = Path.Combine(cfg.SessionsDir,
                $"summary_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path,
                JsonSerializer.Serialize(summary,
                    new JsonSerializerOptions { WriteIndented = true }));
            Log.Info($"Summary saved → {path}");
        };

        Log.Info($"Session  : {summarizer.SessionId}");
        Log.Info($"FPS      : {cfg.CaptureFps}  (adaptive: {cfg.AdaptiveFps})");
        Log.Info($"OCR      : {cfg.EnableOcr}");
        Log.Info($"Embeddings: {cfg.EnableEmbeddings}");
        Log.Info($"Thumbnails: {cfg.SaveThumbnails}");
        Log.Info($"FullTrace : {cfg.FullTraceMode}");

        // -----------------------------------------------------------------------
        // Capture loop
        // -----------------------------------------------------------------------
        int baseDelayMs = (int)(1000.0 / Math.Clamp(cfg.CaptureFps, 0.1, 10.0));

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        int frameCount = 0;

        while (!cts.IsCancellationRequested)
        {
            try
            {
                string processName = capture.GetActiveProcessName();

                if (!policy.ShouldCapture(processName))
                {
                    await Task.Delay(baseDelayMs, cts.Token);
                    continue;
                }

                // Capture the screen under the cursor (multi-monitor aware).
                var (bmp, monitor) = capture.CaptureCursorScreen();

                if (bmp is not null)
                {
                    using (bmp)
                    {
                        var title = capture.GetForegroundWindowTitle();
                        bool idle = act.IsIdle(TimeSpan.FromSeconds(cfg.IdleThresholdSeconds));
                        var (cx, cy) = capture.GetCursorPosition();

                        string ocrText = (cfg.EnableOcr && policy.EnableOcr)
                            ? await ocr.ExtractAsync(bmp, cfg.OcrLanguage)
                            : string.Empty;

                        float[]? emb = (cfg.EnableEmbeddings && policy.EnableEmbeddings)
                            ? embed.Encode(bmp)
                            : null;

                        string? thumbPath = null;
                        if (cfg.SaveThumbnails && policy.SaveThumbnails)
                        {
                            Directory.CreateDirectory(cfg.SessionsDir);
                            thumbPath = Path.Combine(cfg.SessionsDir,
                                $"thumb_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.jpg");
                            bmp.Save(thumbPath);
                        }

                        var feature = new FrameFeature(
                            Timestamp:     DateTime.UtcNow,
                            ActiveApp:     processName,
                            WindowTitle:   title,
                            IsIdle:        idle,
                            OcrText:       ocrText,
                            Embedding:     emb,
                            ThumbnailPath: thumbPath,
                            MonitorDevice: monitor?.DeviceName,
                            MonitorIndex:  monitor?.Index ?? 0,
                            CursorX:       cx,
                            CursorY:       cy
                        );

                        summarizer.Add(feature);
                        frameCount++;

                        Log.Info(
                            $"Frame {frameCount,4} | {processName,-20} | " +
                            $"Monitor: {monitor?.DeviceName ?? "??",-16} | " +
                            $"Cursor: ({cx},{cy}) | Idle: {idle}");
                    }
                }

                // Adaptive FPS: slow down when idle.
                int delay = (cfg.AdaptiveFps && act.IsIdle(TimeSpan.FromSeconds(cfg.IdleThresholdSeconds)))
                    ? baseDelayMs * 2
                    : baseDelayMs;

                await Task.Delay(delay, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error($"Loop error: {ex.Message}");
                await Task.Delay(baseDelayMs);
            }
        }

        Log.Info($"Runner stopped. {frameCount} frames captured.");
        return 0;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    private static string ResolveModelPath(string configured)
    {
        var candidates = new[]
        {
            Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured),
            Path.Combine(AppContext.BaseDirectory, "models", "onnx", "clip-vit-b32.onnx"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "models", "onnx", "clip-vit-b32.onnx")
        };
        return Array.Find(candidates, File.Exists) ?? candidates[0];
    }
}
