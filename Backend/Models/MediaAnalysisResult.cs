namespace Backend.Models
{
    public class MediaAnalysisResult
    {
        public string Resolution { get; set; } = string.Empty; // e.g., "1080p", "4k"
        public string VideoCodec { get; set; } = string.Empty; // e.g., "x264", "HEVC"
        public string AudioCodec { get; set; } = string.Empty; // e.g., "AAC", "AC3"
        public int AudioChannels { get; set; } // e.g., 2, 6 (for 5.1)
        public long FileSizeBytes { get; set; }
        public double DurationSeconds { get; set; }
        public long VideoBitrate { get; set; }
        public long AudioBitrate { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsSuccess { get; set; } = true;
    }
}
