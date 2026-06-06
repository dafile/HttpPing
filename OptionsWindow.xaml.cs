using System.Windows;
using System.Windows.Controls;

namespace HttpPing
{
    public partial class OptionsWindow : Window
    {
        public int IntervalSeconds { get; private set; } = 5;
        public int TimeoutSeconds { get; private set; } = 10;
        public int ColumnCount { get; private set; } = 3;
        public bool PopupAlerts { get; private set; } = true;
        public bool SoundAlerts { get; private set; } = false;
        public bool MinimizeToTray { get; private set; } = false;
        public bool UseBalloon { get; private set; } = true;
        public int PopupDismissSeconds { get; private set; } = 8;

        public OptionsWindow(int interval, int timeout, int columns,
            bool popup, bool sound, bool minimizeToTray, bool useBalloon, int popupDismissSeconds)
        {
            InitializeComponent();
            IntervalBox.Text = interval.ToString();
            TimeoutBox.Text = timeout.ToString();
            ColumnBox.Text = columns.ToString();
            PopupCheck.IsChecked = popup;
            SoundCheck.IsChecked = sound;
            MinimizeToTrayCheck.IsChecked = minimizeToTray;
            BalloonCheck.IsChecked = useBalloon;

            // Select matching dismiss time
            PopupDismissSeconds = popupDismissSeconds;
            foreach (ComboBoxItem item in DismissCombo.Items)
            {
                if (int.Parse(item.Tag.ToString()) == popupDismissSeconds)
                {
                    DismissCombo.SelectedItem = item;
                    break;
                }
            }
            if (DismissCombo.SelectedItem == null)
                DismissCombo.SelectedIndex = 2; // default 5 min
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(IntervalBox.Text, out var interval) || interval < 1)
            {
                MessageBox.Show("检测间隔必须为正整数。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TimeoutBox.Text, out var timeout) || timeout < 1)
            {
                MessageBox.Show("超时时间必须为正整数。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(ColumnBox.Text, out var columns) || columns < 1 || columns > 12)
            {
                MessageBox.Show("列数必须在 1-12 之间。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IntervalSeconds = interval;
            TimeoutSeconds = timeout;
            ColumnCount = columns;
            PopupAlerts = PopupCheck.IsChecked == true;
            SoundAlerts = SoundCheck.IsChecked == true;
            MinimizeToTray = MinimizeToTrayCheck.IsChecked == true;
            UseBalloon = BalloonCheck.IsChecked == true;

            if (DismissCombo.SelectedItem is ComboBoxItem selected)
                PopupDismissSeconds = int.Parse(selected.Tag.ToString());

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
