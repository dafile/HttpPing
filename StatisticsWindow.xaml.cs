using System.Windows;
using HttpPing.ViewModels;

namespace HttpPing
{
    public partial class StatisticsWindow : Window
    {
        private readonly ProbeViewModel _viewModel;

        public StatisticsWindow(ProbeViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            LoadData();
        }

        private void LoadData()
        {
            ProbeNameText.Text = _viewModel.DisplayName;

            var stats = _viewModel.Statistics;
            if (stats != null)
            {
                TotalChecksText.Text = stats.TotalChecks.ToString();
                SuccessCountText.Text = stats.SuccessCount.ToString();
                FailureCountText.Text = stats.FailureCount.ToString();
                UptimeText.Text = $"{stats.UptimePercent:F1}%";
                MinResponseText.Text = stats.TotalChecks > 0 ? $"{stats.MinResponseMs}ms" : "-";
                MaxResponseText.Text = stats.TotalChecks > 0 ? $"{stats.MaxResponseMs}ms" : "-";
                AvgResponseText.Text = stats.TotalChecks > 0 ? $"{stats.AvgResponseMs:F0}ms" : "-";
                LastFailureText.Text = stats.LastFailureTimeText;
            }

            var last = _viewModel.LastResponseMs;
            LastResponseText.Text = last >= 0 ? $"{last}ms" : "-";
            HistoryCountText.Text = _viewModel.History.Count.ToString();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
