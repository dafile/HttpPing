using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using HttpPing.Models;

namespace HttpPing
{
    public partial class StatusHistoryWindow : Window
    {
        private readonly ObservableCollection<StatusHistoryEntry> _allEntries = new();
        private ICollectionView _filteredView;

        public StatusHistoryWindow()
        {
            InitializeComponent();

            _filteredView = CollectionViewSource.GetDefaultView(_allEntries);
            _filteredView.Filter = FilterEntry;
            HistoryGrid.ItemsSource = _filteredView;
        }

        public void AddEntry(StatusHistoryEntry entry)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _allEntries.Add(entry);
                _filteredView.Refresh();
            });
        }

        public void ClearEntries()
        {
            _allEntries.Clear();
            _filteredView.Refresh();
        }

        private bool FilterEntry(object obj)
        {
            if (obj is not StatusHistoryEntry entry) return false;

            // Filter by status type
            if (!ShowUpCheck.IsChecked.HasValue || !ShowUpCheck.IsChecked.Value)
            {
                if (entry.ChangeType == StatusChangeType.Up) return false;
            }
            if (!ShowDownCheck.IsChecked.HasValue || !ShowDownCheck.IsChecked.Value)
            {
                if (entry.ChangeType == StatusChangeType.Down) return false;
            }
            if (!ShowStartStopCheck.IsChecked.HasValue || !ShowStartStopCheck.IsChecked.Value)
            {
                if (entry.ChangeType == StatusChangeType.Started ||
                    entry.ChangeType == StatusChangeType.Stopped)
                    return false;
            }

            // Filter by text
            var filter = FilterBox.Text?.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                var search = filter.ToLowerInvariant();
                if (!entry.Url.ToLowerInvariant().Contains(search) &&
                    !entry.Alias.ToLowerInvariant().Contains(search) &&
                    !entry.StatusText.Contains(search))
                    return false;
            }

            return true;
        }

        private void FilterBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _filteredView?.Refresh();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            _filteredView?.Refresh();
        }
    }
}
