using System;
using System.Collections.ObjectModel;
using System.Media;
using System.Windows;
using System.Windows.Forms;
using HttpPing.Models;

namespace HttpPing.Services
{
    public class AlertService
    {
        public bool PopupEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = false;
        public bool UseBalloon { get; set; } = true;
        public int PopupDismissSeconds { get; set; } = 8;

        /// <summary>
        /// Set the NotifyIcon for balloon notifications. Set from MainWindow after tray icon is created.
        /// </summary>
        public NotifyIcon NotifyIcon { get; set; }

        /// <summary>
        /// All status history entries. MainWindow adds a StatusHistoryWindow that reads from this.
        /// </summary>
        public ObservableCollection<StatusHistoryEntry> History { get; } = new();

        /// <summary>
        /// Event fired when a status change occurs, so MainWindow can update the history window.
        /// </summary>
        public event Action<StatusHistoryEntry> OnHistoryEntryAdded;

        /// <summary>
        /// The single shared popup notification window (vmPing style).
        /// </summary>
        private NotificationWindow _popupWindow;

        public void OnStatusChanged(string url, string alias, bool isNowUp, int statusCode)
        {
            var displayName = !string.IsNullOrEmpty(alias) ? alias : url;

            // Record history entry
            var entry = new StatusHistoryEntry
            {
                Timestamp = DateTime.Now,
                Url = url,
                Alias = alias ?? "",
                ChangeType = isNowUp ? StatusChangeType.Up : StatusChangeType.Down
            };
            History.Add(entry);
            OnHistoryEntryAdded?.Invoke(entry);

            // 1. Balloon notification (system tray) — non-blocking
            if (UseBalloon && NotifyIcon != null)
            {
                try
                {
                    var statusText = isNowUp ? "恢复连接" : "连接断开";
                    NotifyIcon.ShowBalloonTip(5000, $"HttpPing - {statusText}",
                        $"{displayName} [{url}]", isNowUp ? ToolTipIcon.Info : ToolTipIcon.Warning);
                }
                catch { }
            }

            // 2. Popup notification — both reconnect AND disconnect (vmPing-style shared window)
            if (PopupEnabled)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (_popupWindow == null || !_popupWindow.IsLoaded)
                        {
                            _popupWindow = new NotificationWindow();
                            _popupWindow.SetDismissTimeout(PopupDismissSeconds);
                            _popupWindow.Show();
                        }
                        _popupWindow.AddEntry(displayName, isNowUp);
                        _popupWindow.Activate();
                    }
                    catch { }
                });
            }

            // 3. Sound notification
            if (SoundEnabled)
            {
                try
                {
                    if (isNowUp)
                        SystemSounds.Asterisk.Play();
                    else
                        SystemSounds.Exclamation.Play();
                }
                catch { }
            }
        }

        /// <summary>
        /// Record a start/stop event (user-initiated, not status change).
        /// </summary>
        public void RecordStartStop(string url, string alias, bool isStart)
        {
            var entry = new StatusHistoryEntry
            {
                Timestamp = DateTime.Now,
                Url = url,
                Alias = alias ?? "",
                ChangeType = isStart ? StatusChangeType.Started : StatusChangeType.Stopped
            };
            History.Add(entry);
            OnHistoryEntryAdded?.Invoke(entry);
        }
    }
}
