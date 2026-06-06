using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HttpPing.Models;
using HttpPing.Services;

namespace HttpPing.ViewModels
{
    public class ProbeViewModel : INotifyPropertyChanged, IDisposable
    {
        private const int MaxHistory = 500;
        private const int MaxRecentDots = 40;
        private const int SparklinePoints = 50;

        private readonly HttpProbeService _probeService;
        private readonly AlertService _alertService;
        private readonly DispatcherTimer _timer;
        private CancellationTokenSource _cts;
        private bool _isPreviousUp;
        private bool _hasChecked;
        private readonly List<ProbeHistoryEntry> _history = new();
        private readonly ObservableCollection<bool> _recentResults = new();

        public ProbeViewModel(HttpProbeService probeService, AlertService alertService, int intervalSeconds = 5)
        {
            _probeService = probeService;
            _alertService = alertService;
            _isPreviousUp = false;
            _hasChecked = false;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(intervalSeconds) };
            _timer.Tick += async (s, e) => await DoCheckAsync();

            RemoveCommand = new RelayCommand(() => RemoveRequested?.Invoke(this));
            ToggleCommand = new RelayCommand(Toggle);
            CopyUrlCommand = new RelayCommand(CopyUrl);
            OpenInBrowserCommand = new RelayCommand(OpenInBrowser);
            ShowStatisticsCommand = new RelayCommand(() => StatisticsRequested?.Invoke(this));

            StatusCode = "---";
            StatusColor = new SolidColorBrush(Colors.Gray);
            Statistics = new ProbeStatistics();
            LastResponseMs = -1;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<ProbeViewModel> RemoveRequested;
        public event Action<ProbeViewModel> StatisticsRequested;

        // --- Properties ---

        private string _url = "";
        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        private string _alias = "";
        public string Alias
        {
            get => _alias;
            set { _alias = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string DisplayName => !string.IsNullOrEmpty(Alias) ? Alias : Url;

        private string _statusCode = "---";
        public string StatusCode
        {
            get => _statusCode;
            set { _statusCode = value; OnPropertyChanged(); }
        }

        private string _responseTime = "";
        public string ResponseTime
        {
            get => _responseTime;
            set { _responseTime = value; OnPropertyChanged(); }
        }

        private SolidColorBrush _statusColor;
        public SolidColorBrush StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        private string _lastCheckTime = "";
        public string LastCheckTime
        {
            get => _lastCheckTime;
            set { _lastCheckTime = value; OnPropertyChanged(); }
        }

        private string _detailInfo = "";
        public string DetailInfo
        {
            get => _detailInfo;
            set { _detailInfo = value; OnPropertyChanged(); }
        }

        private ProbeStatistics _statistics;
        public ProbeStatistics Statistics
        {
            get => _statistics;
            set { _statistics = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatsSummary)); }
        }

        public long LastResponseMs { get; private set; } = -1;

        public string StatsSummary => Statistics.TotalChecks > 0
            ? $"可用率 {Statistics.UptimePercent:F1}% | 平均 {Statistics.AvgResponseMs:F0}ms | 检测 {Statistics.TotalChecks}"
            : "等待首次检测...";

        public IReadOnlyList<ProbeHistoryEntry> History => _history;
        public ObservableCollection<bool> RecentResults => _recentResults;

        public IList<long> ResponseTimeHistory =>
            _history.Count == 0
                ? Array.Empty<long>()
                : _history.TakeLast(SparklinePoints).Select(e => e.ResponseTimeMs).ToList();

        public string StatusToolTip => _hasChecked
            ? $"状态码: {StatusCode}\n响应时间: {ResponseTime}\n最后检测: {LastCheckTime}\n{DetailInfo}"
            : "尚未检测";

        // --- Commands ---

        public ICommand RemoveCommand { get; }
        public ICommand ToggleCommand { get; }
        public ICommand CopyUrlCommand { get; }
        public ICommand OpenInBrowserCommand { get; }
        public ICommand ShowStatisticsCommand { get; }

        // --- Public Methods ---

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            _alertService.RecordStartStop(Url, Alias, true);
            _timer.Start();
            _ = DoCheckAsync();
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            _timer.Stop();
            _cts?.Cancel();
            _alertService.RecordStartStop(Url, Alias, false);
            StatusColor = new SolidColorBrush(Colors.Gray);
            StatusCode = "---";
            ResponseTime = "";
            DetailInfo = "";
        }

        public void SetInterval(int seconds)
        {
            _timer.Interval = TimeSpan.FromSeconds(seconds);
        }

        // --- Private Methods ---

        private void Toggle()
        {
            if (IsRunning) Stop(); else Start();
        }

        private void CopyUrl()
        {
            try { Clipboard.SetText(Url); } catch { }
        }

        private void OpenInBrowser()
        {
            try
            {
                Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
            }
            catch { }
        }

        private async System.Threading.Tasks.Task DoCheckAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            StatusColor = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));

            var result = await _probeService.CheckAsync(Url, _cts.Token);

            LastCheckTime = result.Timestamp.ToString("HH:mm:ss");
            LastResponseMs = result.ResponseTimeMs;

            if (result.IsSuccess)
            {
                StatusCode = result.StatusCode.ToString();
                ResponseTime = $"{result.ResponseTimeMs}ms";
                DetailInfo = result.StatusDescription;

                if (result.StatusCode >= 200 && result.StatusCode < 300)
                    StatusColor = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
                else if (result.StatusCode >= 300 && result.StatusCode < 500)
                    StatusColor = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
                else
                    StatusColor = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));

                if (_hasChecked && !_isPreviousUp)
                    _alertService.OnStatusChanged(Url, Alias, true, result.StatusCode);
                _isPreviousUp = true;
            }
            else
            {
                StatusCode = "ERR";
                ResponseTime = $"{result.ResponseTimeMs}ms";
                DetailInfo = result.ErrorMessage;
                StatusColor = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));

                if (_hasChecked && _isPreviousUp)
                    _alertService.OnStatusChanged(Url, Alias, false, 0);
                _isPreviousUp = false;
            }

            _hasChecked = true;

            // Record history
            var entry = new ProbeHistoryEntry
            {
                Timestamp = result.Timestamp,
                IsSuccess = result.IsSuccess,
                StatusCode = result.StatusCode,
                ResponseTimeMs = result.ResponseTimeMs,
                ErrorMessage = result.ErrorMessage
            };
            _history.Add(entry);
            if (_history.Count > MaxHistory) _history.RemoveAt(0);

            _recentResults.Add(result.IsSuccess);
            if (_recentResults.Count > MaxRecentDots) _recentResults.RemoveAt(0);

            RecalculateStatistics();

            OnPropertyChanged(nameof(ResponseTimeHistory));
            OnPropertyChanged(nameof(StatsSummary));
            OnPropertyChanged(nameof(StatusToolTip));
            OnPropertyChanged(nameof(LastResponseMs));
        }

        private void RecalculateStatistics()
        {
            if (_history.Count == 0)
            {
                Statistics = new ProbeStatistics();
                return;
            }

            var successCount = _history.Count(h => h.IsSuccess);
            var failureCount = _history.Count - successCount;
            var responseTimes = _history.Where(h => h.IsSuccess).Select(h => h.ResponseTimeMs).ToList();

            Statistics = new ProbeStatistics
            {
                TotalChecks = _history.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                UptimePercent = _history.Count > 0 ? (double)successCount / _history.Count * 100 : 0,
                MinResponseMs = responseTimes.Count > 0 ? responseTimes.Min() : 0,
                MaxResponseMs = responseTimes.Count > 0 ? responseTimes.Max() : 0,
                AvgResponseMs = responseTimes.Count > 0 ? responseTimes.Average() : 0,
                LastFailureTime = _history.LastOrDefault(h => !h.IsSuccess)?.Timestamp
            };
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            _timer.Stop();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) { _execute = execute; }
#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute();
    }
}
