using Backend.Models;

namespace Backend.Services.Interfaces
{
    public interface IMediaAnalysisService
    {
        Task<MediaAnalysisResult> AnalyzeFileAsync(string filePath, string ffmpegPath = null);
    }
}
