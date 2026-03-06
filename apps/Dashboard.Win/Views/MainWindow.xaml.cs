using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Surveil.Agent.Services;
using Surveil.Contracts;
using Surveil.Processing;
using Surveil.Utils;

namespace Dashboard.Win.Views
{
    public partial class MainWindow : Window
    {
        // -----------------------------------------------------------------------
        // Services
        // -----------------------------------------------------------------------
        private readonly ConfigService    _cfgSvc  = new();
        private readonly CaptureService   _capture = new();
        private readonly OcrService       _ocr     = new();
        private readonly ActivityService  _act     = new();
        private readonly EmbeddingService _emb;
        private          PolicyService    _policy;
        private          RetentionService _retention;
        private          SlidingSummarizer _summarizer;

        private CancellationTokenSource? _cts;
        private int _frameCount;

        // -----------------------------------------------------------------------
        // Construction
        // -----------------------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();

            var cfg = _cfgSvc.Config;

            // Resolve ONNX model path – check several well-known locations.
            string modelPath = ResolveModelPath(cfg.ModelPath);
            _emb = new EmbeddingService(modelPath);

            _policy    = new PolicyService(cfg);
            _retention = new RetentionService(cfg);

            // Initialise summarizer with session ID from config.
            _summarizer = new SlidingSummarizer(fullTrace: cfg.FullTraceMode);

            _summarizer.OnSummary += OnSummaryArrived;

            // Populate UI from config.
            ChkThumbnails.IsChecked = cfg.SaveThumbnails;
            ChkFullTrace.IsChecked  = cfg.FullTraceMode;
            ChkOcr.IsChecked        = cfg.EnableOcr;
            TxtSession.Text         = $"Session: {_summarizer.SessionId}";
            TxtFps.Text             = $"{cfg.CaptureFps:0.0}";

            // Populate monitor list in settings panel.
            RefreshMonitorList();

            // Run startup retention cleanup.
            _retention.Cleanup();
        }

        // -----------------------------------------------------------------------
        // Capture loop
        // -----------------------------------------------------------------------
        private async Task LoopAsync(CancellationToken token)
        {
            var cfg = _cfgSvc.Config;
            int delayMs = (int)(1000.0 / Math.Clamp(cfg.CaptureFps, 0.1, 10.0));

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await CaptureFrameAsync(cfg);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error($"Capture loop error: {ex.Message}");
                }

                // Adaptive FPS: double the delay when idle.
                int effectiveDelay = (cfg.AdaptiveFps && _act.IsIdle(TimeSpan.FromSeconds(cfg.IdleThresholdSeconds)))
                    ? delayMs * 2
                    : delayMs;

                await Task.Delay(effectiveDelay, token);
            }
        }

        private async Task CaptureFrameAsync(AppConfig cfg)
        {
            string processName = _capture.GetActiveProcessName();

            // Policy check – skip denied/unlisted apps.
            if (!_policy.ShouldCapture(processName)) return;

            // Capture the screen where the cursor currently is.
            var (bmp, monitor) = _capture.CaptureCursorScreen();
            if (bmp is null) return;

            using (bmp)
            {
                var title = _capture.GetForegroundWindowTitle();
                bool idle = _act.IsIdle(TimeSpan.FromSeconds(cfg.IdleThresholdSeconds));
                var (cx, cy) = _capture.GetCursorPosition();

                // OCR (optional).
                string ocrText = (cfg.EnableOcr && _policy.EnableOcr)
                    ? await _ocr.ExtractAsync(bmp, cfg.OcrLanguage)
                    : string.Empty;

                // Embeddings (optional).
                float[]? emb = (cfg.EnableEmbeddings && _policy.EnableEmbeddings)
                    ? _emb.Encode(bmp)
                    : null;

                // Thumbnail (optional).
                string? thumbPath = null;
                if (cfg.SaveThumbnails && _policy.SaveThumbnails)
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

                _summarizer.Add(feature);
                _frameCount++;

                // Refresh live status on UI thread.
                Dispatcher.Invoke(() =>
                {
                    TxtApp.Text        = processName;
                    TxtMonitor.Text    = monitor?.DeviceName ?? "—";
                    TxtFrameCount.Text = $"Frames: {_frameCount}";
                });
            }
        }

        // -----------------------------------------------------------------------
        // Summary handler
        // -----------------------------------------------------------------------
        private void OnSummaryArrived(Summary s)
        {
            // Persist to disk.
            var dir = _cfgSvc.Config.SessionsDir;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir,
                $"summary_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path,
                System.Text.Json.JsonSerializer.Serialize(s,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            // Append to UI list.
            Dispatcher.Invoke(() =>
            {
                string monPart = s.Frames?.Length > 0
                    ? string.Empty
                    : string.Empty;
                SummaryList.Items.Insert(0,
                    $"{s.Start:HH:mm:ss}–{s.End:HH:mm:ss}  conf={s.Confidence:0.00}  {s.Narrative}");
            });
        }

        // -----------------------------------------------------------------------
        // Button handlers
        // -----------------------------------------------------------------------
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = LoopAsync(_cts.Token);

            BtnStart.IsEnabled = false;
            BtnPause.IsEnabled = true;
            StatusDot.Background = new SolidColorBrush(Color.FromRgb(0xa6, 0xe3, 0xa1)); // green
            TxtStatus.Text = "Running";
            Log.Info($"Capture started (session: {_summarizer.SessionId}).");
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();

            BtnStart.IsEnabled = true;
            BtnPause.IsEnabled = false;
            StatusDot.Background = new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5a)); // grey
            TxtStatus.Text = "Paused";
            Log.Info("Capture paused.");
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------
        private static string ResolveModelPath(string configured)
        {
            var candidates = new[]
            {
                Path.IsPathRooted(configured) ? configured : Path.Combine(AppContext.BaseDirectory, configured),
                Path.Combine(AppContext.BaseDirectory, "models", "onnx", "clip-vit-b32.onnx"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "models", "onnx", "clip-vit-b32.onnx")
            };
            return Array.Find(candidates, File.Exists) ?? candidates[0];
        }

        private void RefreshMonitorList()
        {
            var monitors = _capture.GetAllMonitors();
            MonitorList.Items.Clear();
            foreach (var m in monitors)
            {
                string primary = m.IsPrimary ? " (primary)" : string.Empty;
                MonitorList.Items.Add(
                    $"[{m.Index}] {m.DeviceName}{primary}  {m.Bounds.Width}×{m.Bounds.Height}");
            }
        }
    }
}
