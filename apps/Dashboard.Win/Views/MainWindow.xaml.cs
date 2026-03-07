using System;
using System.IO;
using System.Timers;
using System.Windows;
using Surveil.Agent.Services;

namespace Dashboard.Win.Views
{
    public partial class MainWindow : Window
    {
        private readonly ApiClient _api;
        private string? _currentShiftId;
        private System.Timers.Timer? _uiTimer;
        private DateTimeOffset _shiftStartTime;

        public MainWindow()
        {
            InitializeComponent();

            var cfgPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            string apiBase = "http://localhost:8080";
            if (File.Exists(cfgPath))
            {
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(cfgPath));
                    if (doc.RootElement.TryGetProperty("ApiBaseUrl", out var prop))
                        apiBase = prop.GetString() ?? apiBase;
                }
                catch { }
            }

            _api = new ApiClient(apiBase);

            if (_api.LoadSavedTokens())
                ShowShiftPanel();
        }

        private void ShowLoginPanel()
        {
            LoginPanel.Visibility = Visibility.Visible;
            ShiftPanel.Visibility = Visibility.Collapsed;
            MonitoringPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowShiftPanel()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            ShiftPanel.Visibility = Visibility.Visible;
            MonitoringPanel.Visibility = Visibility.Collapsed;
            UserNameText.Text = "Signed in";
            ShiftStatusText.Text = "Click 'Start Shift' to begin monitoring.";
        }

        private void ShowMonitoringPanel()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            ShiftPanel.Visibility = Visibility.Collapsed;
            MonitoringPanel.Visibility = Visibility.Visible;
            StartUiTimer();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            LoginErrorText.Visibility = Visibility.Collapsed;
            var email = EmailBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                LoginErrorText.Text = "Please enter your email and password.";
                LoginErrorText.Visibility = Visibility.Visible;
                LoginButton.IsEnabled = true;
                return;
            }

            var ok = await _api.LoginAsync(email, password);
            if (ok)
            {
                PasswordBox.Clear();
                ShowShiftPanel();
            }
            else
            {
                LoginErrorText.Text = "Invalid email or password.";
                LoginErrorText.Visibility = Visibility.Visible;
            }
            LoginButton.IsEnabled = true;
        }

        private async void StartShiftButton_Click(object sender, RoutedEventArgs e)
        {
            StartShiftButton.IsEnabled = false;
            ShiftStatusText.Text = "Starting shift...";

            var shiftId = await _api.StartShiftAsync();
            if (shiftId == null)
            {
                ShiftStatusText.Text = "Failed to start shift. Check connection.";
                StartShiftButton.IsEnabled = true;
                return;
            }

            _currentShiftId = shiftId;
            _shiftStartTime = DateTimeOffset.Now;
            ShowMonitoringPanel();
            AppendLog($"Shift started: {shiftId}");
        }

        private async void EndShiftButton_Click(object sender, RoutedEventArgs e)
        {
            EndShiftButton.IsEnabled = false;
            if (_currentShiftId != null)
            {
                await _api.EndShiftAsync(_currentShiftId);
                _currentShiftId = null;
            }
            StopUiTimer();
            ShowShiftPanel();
            ShiftStatusText.Text = "Shift ended. Ready for next shift.";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _api.Logout();
            ShowLoginPanel();
        }

        private void StartUiTimer()
        {
            _uiTimer = new System.Timers.Timer(1000);
            _uiTimer.Elapsed += OnUiTick;
            _uiTimer.Start();
        }

        private void StopUiTimer()
        {
            _uiTimer?.Stop();
            _uiTimer?.Dispose();
            _uiTimer = null;
        }

        private void OnUiTick(object? sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var elapsed = DateTimeOffset.Now - _shiftStartTime;
                ShiftTimeText.Text = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            });
        }

        public void UpdateStatus(string app, int frames, string uploadStatus)
        {
            Dispatcher.Invoke(() =>
            {
                LastAppText.Text = $"Last app: {app}";
                FrameCountText.Text = $"Frames: {frames}";
                UploadStatusText.Text = $"Upload: {uploadStatus}";
            });
        }

        public void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var ts = DateTime.Now.ToString("HH:mm:ss");
                ActivityLog.AppendText($"[{ts}] {message}\n");
                ActivityLog.ScrollToEnd();
            });
        }

        public string? CurrentShiftId => _currentShiftId;
        public ApiClient ApiClient => _api;
    }
}
