using System;

namespace HttpPing.Models
{
    public class ProbeStatistics
    {
        public int TotalChecks { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public double UptimePercent { get; set; }
        public long MinResponseMs { get; set; }
        public long MaxResponseMs { get; set; }
        public double AvgResponseMs { get; set; }
        public DateTime? LastFailureTime { get; set; }
        public string LastFailureTimeText => LastFailureTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
    }
}
