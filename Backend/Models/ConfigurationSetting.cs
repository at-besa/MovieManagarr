namespace Backend.Models
{
    public class ConfigurationSetting
    {
        public int Id { get; set; }
        public string TmdbApiKey { get; set; } = string.Empty;
        public string SourceDir { get; set; } = string.Empty;
        public string TargetMovieDir { get; set; } = string.Empty;
        public string TargetSeriesDir { get; set; } = string.Empty;
        public string MoviePattern { get; set; } = "{Title} ({Year}) - [{Resolution} {VideoCodec}]";
        public string SeriesPattern { get; set; } = "{Title} ({Year}) S{S}E{E} - [{Resolution} {VideoCodec}]";
        public string SeasonFolderPattern { get; set; } = "Season {S}";
        public string IgnoreFolders { get; set; } = "Games,Apps,Software";
        public string DefaultHwAccel { get; set; } = "qsv";
        public string FfmpegPath { get; set; } = string.Empty;
    }
}
