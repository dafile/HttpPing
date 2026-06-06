using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using HttpPing.Controls;
using HttpPing.Models;
using HttpPing.Services;
using HttpPing.ViewModels;

namespace HttpPing
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<ProbeViewModel> _probes = new ObservableCollection<ProbeViewModel>();
        private readonly HttpProbeService _probeService;
        private readonly AlertService _alertService;
        private AppConfig _config;
        private int _intervalSeconds = 5;
        private int _timeoutSeconds = 10;
        private int _columnCount = 3;

        // System tray
        private NotifyIcon _notifyIcon;
        private bool _minimizeToTray;

        // Status history window
        private StatusHistoryWindow _historyWindow;

        public MainWindow()
        {
            InitializeComponent();

            _config = ConfigManager.Load();
            _intervalSeconds = _config.Settings.IntervalSeconds;
            _timeoutSeconds = _config.Settings.TimeoutSeconds;
            _columnCount = _config.Settings.ColumnCount;
            _minimizeToTray = _config.Settings.MinimizeToTray;

            _alertService = new AlertService();
            _alertService.OnHistoryEntryAdded += OnHistoryEntryAdded;
            _alertService.PopupDismissSeconds = _config.Settings.PopupDismissSeconds;
            _probeService = new HttpProbeService(_timeoutSeconds);

            Topmost = _config.Settings.AlwaysOnTop;

            InitTrayIcon();
            LoadLastSession();
            BuildFavoritesMenu();
            UpdateStatusBar();
        }

        #region System Tray

        private void InitTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "HttpPing - HTTP 响应监控",
                Visible = false
            };

            try
            {
                // Load icon from WPF resource, convert to byte[] then to System.Drawing.Icon
                var iconUri = new Uri("pack://application:,,,/location_http_14327.ico", UriKind.Absolute);
                var resourceStream = System.Windows.Application.GetResourceStream(iconUri);
                if (resourceStream != null)
                {
                    using var ms = new System.IO.MemoryStream();
                    resourceStream.Stream.CopyTo(ms);
                    ms.Position = 0;
                    _notifyIcon.Icon = new System.Drawing.Icon(ms);
                }
                else
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();

            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示窗口", null, (s, e) => RestoreFromTray());
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("退出", null, (s, e) =>
            {
                _notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });
            _notifyIcon.ContextMenuStrip = trayMenu;

            _alertService.NotifyIcon = _notifyIcon;
            _alertService.PopupEnabled = _config.Settings.PopupAlerts;
            _alertService.SoundEnabled = _config.Settings.SoundAlerts;
            _alertService.UseBalloon = _config.Settings.UseBalloonNotifications;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _minimizeToTray)
            {
                Hide();
                _notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(2000, "HttpPing", "程序已最小化到系统托盘", ToolTipIcon.Info);
            }
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            _notifyIcon.Visible = false;
            Activate();
        }

        #endregion

        #region Status History

        private void OnHistoryEntryAdded(StatusHistoryEntry entry)
        {
            // Forward to history window if it's open
            _historyWindow?.AddEntry(entry);
        }

        private void ShowHistory_Click(object sender, RoutedEventArgs e)
        {
            if (_historyWindow == null || !_historyWindow.IsLoaded)
            {
                _historyWindow = new StatusHistoryWindow { Owner = this };
                _historyWindow.Closed += (s, args) => _historyWindow = null;

                // Load existing history
                foreach (var entry in _alertService.History)
                    _historyWindow.AddEntry(entry);

                _historyWindow.Show();
            }
            else
            {
                _historyWindow.Activate();
                _historyWindow.WindowState = WindowState.Normal;
            }
        }

        #endregion

        #region Probe Management

        private void LoadLastSession()
        {
            if (_config.LastSession == null || _config.LastSession.Count == 0) return;

            foreach (var cfg in _config.LastSession)
            {
                AddProbe(cfg.Url, cfg.Alias, autoStart: false);
            }
        }

        private void AddProbe(string url, string alias = "", bool autoStart = true)
        {
            var vm = new ProbeViewModel(_probeService, _alertService, _intervalSeconds)
            {
                Url = url,
                Alias = alias
            };
            vm.RemoveRequested += RemoveProbe;
            vm.StatisticsRequested += ShowStatistics;
            vm.PropertyChanged += Probe_PropertyChanged;
            _probes.Add(vm);

            RebuildGrid();
            UpdateStatusBar();

            if (autoStart) vm.Start();
        }

        private void RemoveProbe(ProbeViewModel probe)
        {
            probe.PropertyChanged -= Probe_PropertyChanged;
            probe.Stop();
            probe.Dispose();
            _probes.Remove(probe);
            RebuildGrid();
            UpdateStatusBar();
        }

        private void ShowStatistics(ProbeViewModel probe)
        {
            var dlg = new StatisticsWindow(probe) { Owner = this };
            dlg.ShowDialog();
        }

        private void Probe_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProbeViewModel.StatusCode) ||
                e.PropertyName == nameof(ProbeViewModel.Statistics))
            {
                UpdateStatusBar();
            }
        }

        private void RebuildGrid()
        {
            ProbeGrid.Children.Clear();
            ProbeGrid.RowDefinitions.Clear();
            ProbeGrid.ColumnDefinitions.Clear();

            if (_probes.Count == 0) return;

            var cols = Math.Max(1, _columnCount);
            var rows = (int)Math.Ceiling((double)_probes.Count / cols);

            for (int c = 0; c < cols; c++)
                ProbeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int r = 0; r < rows; r++)
                ProbeGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < _probes.Count; i++)
            {
                var tile = new ProbeTile { DataContext = _probes[i] };
                Grid.SetRow(tile, i / cols);
                Grid.SetColumn(tile, i % cols);
                ProbeGrid.Children.Add(tile);
            }
        }

        #endregion

        #region Status Bar

        private void UpdateStatusBar()
        {
            TotalProbesText.Text = _probes.Count.ToString();

            var upCount = _probes.Count(p => p.StatusCode != "---" && p.StatusCode != "ERR" &&
                                              p.StatusColor?.Color == System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
            var downCount = _probes.Count(p => p.StatusCode == "ERR" ||
                                                (p.StatusColor?.Color == System.Windows.Media.Color.FromRgb(0xF4, 0x43, 0x36)));

            UpCountText.Text = upCount.ToString();
            DownCountText.Text = downCount.ToString();

            var activeProbes = _probes.Where(p => p.LastResponseMs >= 0).ToList();
            if (activeProbes.Count > 0)
            {
                var avgMs = activeProbes.Average(p => p.LastResponseMs);
                AvgResponseText.Text = $"{avgMs:F0}ms";

                var stats = activeProbes.Select(p => p.Statistics).Where(s => s.TotalChecks > 0).ToList();
                if (stats.Count > 0)
                {
                    var globalUptime = stats.Average(s => s.UptimePercent);
                    GlobalUptimeText.Text = $"{globalUptime:F1}%";
                }
                else
                {
                    GlobalUptimeText.Text = "-";
                }
            }
            else
            {
                AvgResponseText.Text = "-";
                GlobalUptimeText.Text = "-";
            }
        }

        #endregion

        #region Menu Handlers

        private void AddHost_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddHostDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                AddProbe(dlg.EnteredUrl, dlg.EnteredAlias);
            }
        }

        private void StartAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in _probes) p.Start();
        }

        private void StopAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in _probes) p.Stop();
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OptionsWindow(_intervalSeconds, _timeoutSeconds, _columnCount,
                _alertService.PopupEnabled, _alertService.SoundEnabled,
                _minimizeToTray, _alertService.UseBalloon,
                _alertService.PopupDismissSeconds) { Owner = this };

            if (dlg.ShowDialog() == true)
            {
                _intervalSeconds = dlg.IntervalSeconds;
                _timeoutSeconds = dlg.TimeoutSeconds;
                _columnCount = dlg.ColumnCount;
                _alertService.PopupEnabled = dlg.PopupAlerts;
                _alertService.SoundEnabled = dlg.SoundAlerts;
                _minimizeToTray = dlg.MinimizeToTray;
                _alertService.UseBalloon = dlg.UseBalloon;
                _alertService.PopupDismissSeconds = dlg.PopupDismissSeconds;

                foreach (var p in _probes) p.SetInterval(_intervalSeconds);
                RebuildGrid();
            }
        }

        private void SaveFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (_probes.Count == 0)
            {
                System.Windows.MessageBox.Show("当前没有主机可保存。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var name = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入收藏夹名称:", "保存收藏夹", "我的服务器");
            if (string.IsNullOrWhiteSpace(name)) return;

            var existing = _config.Favorites.FirstOrDefault(f => f.Name == name);
            if (existing != null)
            {
                if (System.Windows.MessageBox.Show($"收藏夹 \"{name}\" 已存在，是否覆盖？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
                _config.Favorites.Remove(existing);
            }

            var fav = new FavoriteSet { Name = name };
            foreach (var p in _probes)
                fav.Probes.Add(new ProbeConfig { Url = p.Url, Alias = p.Alias });
            _config.Favorites.Add(fav);

            ConfigManager.Save(_config);
            BuildFavoritesMenu();
            System.Windows.MessageBox.Show($"收藏夹 \"{name}\" 已保存。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BuildFavoritesMenu()
        {
            FavoritesMenu.Items.Clear();
            if (_config.Favorites.Count == 0)
            {
                var empty = new MenuItem { Header = "(无收藏)", IsEnabled = false, Foreground = System.Windows.Media.Brushes.Gray };
                FavoritesMenu.Items.Add(empty);
                return;
            }

            foreach (var fav in _config.Favorites)
            {
                var item = new MenuItem { Header = fav.Name, Foreground = System.Windows.Media.Brushes.White };
                item.Click += (s, ev) => LoadFavorite(fav);
                FavoritesMenu.Items.Add(item);
            }

            FavoritesMenu.Items.Add(new Separator());
            var manage = new MenuItem { Header = "管理收藏...", Foreground = System.Windows.Media.Brushes.White };
            manage.Click += ManageFavorites_Click;
            FavoritesMenu.Items.Add(manage);
        }

        private void LoadFavorite(FavoriteSet fav)
        {
            foreach (var p in _probes) { p.PropertyChanged -= Probe_PropertyChanged; p.Stop(); p.Dispose(); }
            _probes.Clear();

            foreach (var cfg in fav.Probes)
                AddProbe(cfg.Url, cfg.Alias);
        }

        private void ManageFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (_config.Favorites.Count == 0)
            {
                System.Windows.MessageBox.Show("没有收藏夹。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var names = string.Join("\n", _config.Favorites.Select(f => $"  • {f.Name} ({f.Probes.Count} 个主机)"));
            var result = System.Windows.MessageBox.Show(
                $"当前收藏夹:\n{names}\n\n是否删除某个收藏夹？\n输入名称取消。",
                "管理收藏夹", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (result == MessageBoxResult.OK)
            {
                var toDelete = Microsoft.VisualBasic.Interaction.InputBox(
                    "输入要删除的收藏夹名称 (留空取消):", "删除收藏夹", "");
                if (!string.IsNullOrWhiteSpace(toDelete))
                {
                    var fav = _config.Favorites.FirstOrDefault(f => f.Name == toDelete);
                    if (fav != null)
                    {
                        _config.Favorites.Remove(fav);
                        ConfigManager.Save(_config);
                        BuildFavoritesMenu();
                        System.Windows.MessageBox.Show($"收藏夹 \"{toDelete}\" 已删除。", "提示");
                    }
                }
            }
        }

        #endregion

        #region Window Closing

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _config.Settings.IntervalSeconds = _intervalSeconds;
            _config.Settings.TimeoutSeconds = _timeoutSeconds;
            _config.Settings.ColumnCount = _columnCount;
            _config.Settings.PopupAlerts = _alertService.PopupEnabled;
            _config.Settings.SoundAlerts = _alertService.SoundEnabled;
            _config.Settings.MinimizeToTray = _minimizeToTray;
            _config.Settings.UseBalloonNotifications = _alertService.UseBalloon;
            _config.Settings.PopupDismissSeconds = _alertService.PopupDismissSeconds;

            _config.LastSession = _probes.Select(p => new ProbeConfig
            {
                Url = p.Url,
                Alias = p.Alias
            }).ToList();

            ConfigManager.Save(_config);

            foreach (var p in _probes) p.Dispose();
            _probeService.Dispose();

            _historyWindow?.Close();

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        #endregion
    }
}
