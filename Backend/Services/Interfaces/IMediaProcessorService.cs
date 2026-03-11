using Backend.Models;

namespace Backend.Services.Interfaces
{
    public interface IMediaProcessorService
    {
        Task<bool> ProcessFileAsync(string filePath, MediaMetadata? manualMatch = null);
        void StartMonitoring(string directoryPath);
    }
}
