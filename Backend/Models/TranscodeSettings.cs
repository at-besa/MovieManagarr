namespace Backend.Models
{
    public class TranscodeSettings
    {
        /// <summary>
        /// Quality preset: "fast" (CRF 28), "balanced" (CRF 23), "quality" (CRF 18)
        /// </summary>
        public string QualityPreset { get; set; } = "balanced";

        /// <summary>
        /// Optional explicit CRF override (0-51). If set, overrides QualityPreset.
        /// </summary>
        public int? CustomCrf { get; set; }

        /// <summary>
        /// Hardware acceleration: "qsv", "nvenc", "amf", "software"
        /// </summary>
        public string HwAcceleration { get; set; } = "qsv";

        /// <summary>
        /// Output codec. Default HEVC.
        /// </summary>
        public string OutputCodec { get; set; } = "hevc";

        /// <summary>
        /// Whether to copy the audio stream as-is (no re-encode).
        /// </summary>
        public bool CopyAudio { get; set; } = true;

        /// <summary>
        /// Optional target resolution override, e.g. "1080p", "720p". Null = keep original.
        /// </summary>
        public string? TargetResolution { get; set; }

        /// <summary>
        /// FFmpeg encoder preset string: "veryfast", "medium", "slow"
        /// </summary>
        public string GetFfmpegPreset()
        {
            return QualityPreset switch
            {
                "fast" => "veryfast",
                "quality" => "slow",
                _ => "medium"
            };
        }

        /// <summary>
        /// Resolved CRF value.
        /// </summary>
        public int GetCrf()
        {
            if (CustomCrf.HasValue) return Math.Clamp(CustomCrf.Value, 0, 51);
            return QualityPreset switch
            {
                "fast" => 28,
                "quality" => 18,
                _ => 23
            };
        }

        /// <summary>
        /// FFmpeg encoder name based on HW + codec selection.
        /// </summary>
        public string GetEncoderName()
        {
            return HwAcceleration switch
            {
                "nvenc" => "hevc_nvenc",
                "amf" => "hevc_amf",
                "qsv" => "hevc_qsv",
                _ => "libx265"
            };
        }
    }
}
