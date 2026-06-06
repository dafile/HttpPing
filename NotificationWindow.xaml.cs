using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace HttpPing
{
    public class NotificationItem
    {
        public DateTime Timestamp { get; set; }
        public string DisplayName { get; set; } = "";
        public bool IsUp { get; set; }
        public string StatusText => IsUp ? "恢复连接" : "连接断开";
        public string Glyph => IsUp ? "✔" : "✖";
        public SolidColorBrush StatusColor => IsUp
            ? new SolidColorBrush(Color.FromRgb(0x85, 0x99, 0x00))
            : new SolidColorBrush(Color.FromRgb(0xDC, 0x32, 0x2F));
        public string TimestampText => $"[{Timestamp:HH:mm:ss}]";
    }

    public partial class NotificationWindow : Window
    {
        private readonly DispatcherTimer _autoDismissTimer;
        private readonly ObservableCollection<NotificationItem> _items = new();
        private int _dismissSeconds = 8;

        public NotificationWindow()
        {
            InitializeComponent();

            _autoDismissTimer = new DispatcherTimer();
            _autoDismissTimer.Tick += AutoDismissTimer_Tick;

            StatusList.ItemsSource = _items;
            ((INotifyCollectionChanged)StatusList.Items).CollectionChanged += StatusList_CollectionChanged;
        }

        /// <summary>
        /// Set dismiss timeout. -1 = never auto-dismiss.
        /// </summary>
        public void SetDismissTimeout(int seconds)
        {
            _dismissSeconds = seconds;
            if (seconds < 0)
            {
                _autoDismissTimer.Stop();
            }
            else
            {
                _autoDismissTimer.Interval = TimeSpan.FromSeconds(seconds);
            }
        }

        /// <summary>
        /// Add a status change entry. Called from AlertService.
        /// </summary>
        public void AddEntry(string displayName, bool isUp)
        {
            Dispatcher.Invoke(() =>
            {
                _items.Add(new NotificationItem
                {
                    Timestamp = DateTime.Now,
                    DisplayName = displayName,
                    IsUp = isUp
                });

                ScaleWindowSize();
                ScrollToEnd();

                // Show and restart auto-dismiss
                if (!IsVisible) Show();
                RestartDismissTimer();
            });
        }

        private void RestartDismissTimer()
        {
            if (_dismissSeconds < 0) return; // never auto-dismiss
            _autoDismissTimer.Stop();
            _autoDismissTimer.Interval = TimeSpan.FromSeconds(_dismissSeconds);
            _autoDismissTimer.Start();
        }

        private void StatusList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RestartDismissTimer();
            ScaleWindowSize();
            ScrollToEnd();
        }

        private void AutoDismissTimer_Tick(object sender, EventArgs e)
        {
            _autoDismissTimer.Stop();
            Close();
        }

        private void ScaleWindowSize()
        {
            switch (_items.Count)
            {
                case 1: Height = 95; break;
                case 2: Height = 110; break;
                case 3: Height = 126; break;
                case 4: Height = 147; break;
                case 5: Height = 172; break;
                default: Height = 172; break;
            }
            PositionWindow(Width);
        }

        private void PositionWindow(double width)
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - width;
            Top = workArea.Bottom - Height;
        }

        private void ScrollToEnd()
        {
            if (StatusList.Items.Count > 0)
            {
                if (VisualTreeHelper.GetChild(StatusList, 0) is Decorator border)
                {
                    if (border.Child is ScrollViewer scroll)
                    {
                        scroll.ScrollToEnd();
                    }
                }
            }
        }

        private void Window_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            if (Application.Current.MainWindow.Visibility != Visibility.Visible)
                Application.Current.MainWindow.Visibility = Visibility.Visible;
            Application.Current.MainWindow.Focus();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _autoDismissTimer.Stop();
            Close();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositionWindow(e.NewSize.Width);
        }
    }
}
