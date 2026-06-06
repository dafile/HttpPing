using System;

namespace HttpPing.Models
{
    public class ProbeHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public string ErrorMessage { get; set; } = "";
    }
}
