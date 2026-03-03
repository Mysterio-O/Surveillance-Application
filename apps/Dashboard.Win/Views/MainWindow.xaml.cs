using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Surveil.Agent.Services;
using Surveil.Processing;
using Surveil.Utils;

namespace Dashboard.Win.Views
{
    public partial class MainWindow : Window
    {
        private readonly CaptureService _capture = new();
        private readonly OcrService _ocr = new();
        private readonly ActivityService _act = new();
        private readonly EmbeddingService _emb;
        private readonly SlidingSummarizer _summarizer = new();
        private CancellationTokenSource? _cts;

        public MainWindow()
        {
            InitializeComponent();

            // Optional ONNX model path; the app works without it
            string modelPath = Path.Combine(AppContext.BaseDirectory, "models", "onnx", "clip-vit-b32.onnx");
            if (!File.Exists(modelPath))
            {
                modelPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "models", "onnx", "clip-vit-b32.onnx");
            }
            _emb = new EmbeddingService(modelPath);

            _summarizer.OnSummary += s =>
            {
                Dispatcher.Invoke(() =>
                    SummaryList.Items.Insert(0, $"{s.Start:t}-{s.End:t} | {s.Narrative} (conf {s.Confidence:0.00})"));
            };
        }

        private async Task LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using var bmp = _capture.CaptureForeground();
                if (bmp != null)
                {
                    var title = _capture.GetForegroundWindowTitle();
                    var idle = _act.IsIdle(TimeSpan.FromSeconds(60));
                    string text = await _ocr.ExtractAsync(bmp);
                    var emb = _emb.Encode(bmp);

                    string dir = Path.Combine("data", "sessions");
                    Directory.CreateDirectory(dir);
                    string thumb = Path.Combine(dir, $"thumb_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.jpg");
                    bmp.Save(thumb);

                    var feature = new Surveil.Contracts.FrameFeature(DateTime.UtcNow, "ForegroundApp", title, idle, text, emb, thumb);
                    _summarizer.Add(feature);
                }

                await Task.Delay(1000, token);
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = LoopAsync(_cts.Token);
            Log.Info("Capture started.");
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            Log.Info("Capture paused.");
        }
    }
}