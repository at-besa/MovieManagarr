using System.Text.RegularExpressions;
using Backend.Models;
using Backend.Services.Interfaces;

namespace Backend.Services
{
    public class RenamerService : IRenamerService
    {
        public (string Title, int Year, int? Season, int? Episode) ParseRawFilename(string rawPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(rawPath);
            var dirName = Path.GetFileName(Path.GetDirectoryName(rawPath));

            string cleanName = fileName.Replace(".", " ").Replace("_", " ");
            
            // Try to extract year from directory first, as it's often more reliable for movies like "The Matrix (1999)\movie.mkv"
            int? parsedYear = null;
            if (!string.IsNullOrWhiteSpace(dirName))
            {
                var dirYearMatch = Regex.Match(dirName, @"(?:\b|\()((?:19|20)\d{2})(?:\b|\))");
                if (dirYearMatch.Success)
                {
                    parsedYear = int.Parse(dirYearMatch.Groups[1].Value);
                }
            }

            // Clean common release group tags from name
            cleanName = Regex.Replace(cleanName, @"\b(1080p|720p|2160p|4k|x264|h264|x265|h265|hevc|web-dl|webrip|hdrip|bluray|remux|aac|ac3|dts)\b.*", "", RegexOptions.IgnoreCase).Trim();
            cleanName = Regex.Replace(cleanName, @"\[.*?\]|\{.*?\}", "").Trim(); // Remove brackets like [YTS.MX]

            // S01E01 Match
            var tvMatch = Regex.Match(cleanName, @"^(.*?)(?:\s|-|\.)*[sS](\d{1,2})[eE](\d{1,2})", RegexOptions.IgnoreCase);
            if (tvMatch.Success)
            {
                var title = tvMatch.Groups[1].Value.Trim('-');
                int s = int.Parse(tvMatch.Groups[2].Value);
                int e = int.Parse(tvMatch.Groups[3].Value);
                
                var yearMatchTv = Regex.Match(title, @"^(.*?)\s*\(?((?:19|20)\d{2})\)?$");
                if (yearMatchTv.Success)
                {
                    return (CleanTitle(yearMatchTv.Groups[1].Value), int.Parse(yearMatchTv.Groups[2].Value), s, e);
                }
                return (CleanTitle(title), parsedYear ?? 0, s, e);
            }

            // Movie Year Match
            var movieMatch = Regex.Match(cleanName, @"^(.*?)\s*\(?((?:19|20)\d{2})\)?(?:$|\s)");
            if (movieMatch.Success)
            {
                return (CleanTitle(movieMatch.Groups[1].Value), int.Parse(movieMatch.Groups[2].Value), null, null);
            }

            // Fallback
            return (CleanTitle(cleanName), parsedYear ?? 0, null, null);
        }

        private string CleanTitle(string title)
        {
            // Remove lingering trailing hyphens or empty parentheses
            title = Regex.Replace(title, @"\(\s*\)", "");
            
            // Remove rogue structural characters that break search
            title = Regex.Replace(title, @"[\[\]{}<>_]", " ");
            
            // Collapse multiple spaces
            title = Regex.Replace(title, @"\s+", " ");

            return title.Trim(' ', '-', '.');
        }

        public string BuildTargetFilename(MediaMetadata metadata, MediaAnalysisResult analysis, string pattern, int? season = null, int? episode = null, string episodeTitle = "")
        {
            var target = pattern;

            target = target.Replace("{Title}", metadata.Title);
            target = target.Replace("{Year}", metadata.Year.ToString());
            target = target.Replace("{Resolution}", analysis.Resolution);
            target = target.Replace("{VideoCodec}", analysis.VideoCodec);
            target = target.Replace("{AudioCodec}", analysis.AudioCodec);

            if (metadata.MediaType == "Series" && season.HasValue && episode.HasValue)
            {
                target = target.Replace("{S}", season.Value.ToString("D2"));
                target = target.Replace("{E}", episode.Value.ToString("D2"));
                target = target.Replace("{EpisodeTitle}", episodeTitle);
            }

            return SanitizeFilename(target);
        }

        public string SanitizeFilename(string name)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalidChars)
            {
                name = name.Replace(c.ToString(), "");
            }
            return name.Trim();
        }
    }
}
