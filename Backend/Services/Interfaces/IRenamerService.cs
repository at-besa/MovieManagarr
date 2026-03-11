using Backend.Models;

namespace Backend.Services.Interfaces
{
    public interface IRenamerService
    {
        (string Title, int Year, int? Season, int? Episode) ParseRawFilename(string rawName);
        string BuildTargetFilename(MediaMetadata metadata, MediaAnalysisResult analysis, string pattern, int? season = null, int? episode = null, string episodeTitle = "");
        string SanitizeFilename(string name);
    }
}
