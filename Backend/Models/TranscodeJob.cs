namespace Backend.Models
{
    public class TranscodeJob
    {
        public string JobId { get; set; } = Guid.NewGuid().ToString("N");
        public string SourcePath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public double ProgressPercent { get; set; }
        public double Fps { get; set; }
        public double Speed { get; set; }
        public string CurrentBitrate { get; set; } = string.Empty;
        public TimeSpan? Eta { get; set; }
        public long EncodedFrames { get; set; }
        public string Status { get; set; } = "Queued"; // Queued, Running, Done, Failed
        public string? ErrorMessage { get; set; }
        public long EstimatedOutputBytes { get; set; }
        public long ActualOutputBytes { get; set; }
        public TranscodeSettings Settings { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }
}
