using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Surveil.Agent.Services;
using Surveil.Contracts;
using Surveil.Processing;
using Surveil.Utils;

namespace Surveil.Runner
{
    internal static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var capture = new CaptureService();
            var ocr = new OcrService();
            var act = new ActivityService();

            // Try common locations for the ONNX model (optional; app works without it)
            string modelPath = Path.Combine(AppContext.BaseDirectory, "models", "onnx", "clip-vit-b32.onnx");
            if (!File.Exists(modelPath))
            {
                modelPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "models", "onnx", "clip-vit-b32.onnx");
            }
            using var embed = new EmbeddingService(modelPath);

            var summarizer = new SlidingSummarizer();
            summarizer.OnSummary += s =>
            {
                var dir = Path.Combine("data", "sessions");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"summary_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(path,
                    System.Text.Json.JsonSerializer.Serialize(
                        s,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                    ));
                Log.Info($"Summary saved: {path}");
            };

            Console.WriteLine("Surveil Runner started. Press Ctrl+C to stop. Sampling ~1 fps...");

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

            while (!cts.IsCancellationRequested)
            {
                using var bmp = capture.CaptureForeground();
                if (bmp != null)
                {
                    var title = capture.GetForegroundWindowTitle();
                    bool idle = act.IsIdle(TimeSpan.FromSeconds(60));
                    string text = await ocr.ExtractAsync(bmp);
                    var emb = embed.Encode(bmp);

                    var dir = Path.Combine("data", "sessions");
                    Directory.CreateDirectory(dir);
                    string thumb = Path.Combine(dir, $"thumb_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.jpg");
                    bmp.Save(thumb); // format not critical for MVP

                    var feature = new FrameFeature(DateTime.UtcNow, "ForegroundApp", title, idle, text, emb, thumb);
                    summarizer.Add(feature);
                }

                await Task.Delay(1000);
            }

            return 0;
        }
    }
}