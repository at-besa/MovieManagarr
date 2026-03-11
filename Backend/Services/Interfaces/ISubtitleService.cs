namespace Backend.Services.Interfaces
{
    public interface ISubtitleService
    {
        Task<bool> FetchSubtitlesAsync(string videoFilePath, string imdbId, string languageCode);
    }
}
