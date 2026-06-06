using System;

namespace HttpPing.Models
{
    public enum StatusChangeType
    {
        Started,    // 已启动
        Stopped,    // 已停止
        Up,         // 恢复连接
        Down        // 断开连接
    }

    public class StatusHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string Url { get; set; } = "";
        public string Alias { get; set; } = "";
        public StatusChangeType ChangeType { get; set; }

        public string DisplayName => !string.IsNullOrEmpty(Alias) ? Alias : Url;

        public string StatusText => ChangeType switch
        {
            StatusChangeType.Started => "● 已启动监控",
            StatusChangeType.Stopped => "● 已停止监控",
            StatusChangeType.Up => "✓ 恢复连接",
            StatusChangeType.Down => "✗ 连接断开",
            _ => ""
        };
    }
}
