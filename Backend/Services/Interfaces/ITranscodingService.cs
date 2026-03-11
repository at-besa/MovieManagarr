using Backend.Models;

namespace Backend.Services.Interfaces
{
    public interface ITranscodingService
    {
        Task<TranscodeJob> StartTranscodeAsync(string filePath, TranscodeSettings settings, string ffmpegPath = "");
        TranscodeJob? GetJobStatus(string jobId);
        Task<string> GeneratePreviewClipAsync(string filePath, string ffmpegPath = "");
        long EstimateOutputSize(MediaAnalysisResult analysis, TranscodeSettings settings);
        bool CancelJob(string jobId);
    }
}
