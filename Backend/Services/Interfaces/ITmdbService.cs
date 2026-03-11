using Backend.Models;

namespace Backend.Services.Interfaces
{
    public interface ITmdbService
    {
        Task<List<MediaMetadata>> SearchMoviesAsync(string query, int? year = null);
        Task<List<MediaMetadata>> SearchSeriesAsync(string query, int? year = null);
        Task<MediaMetadata?> GetMovieByIdAsync(int id);
        Task<MediaMetadata?> GetSeriesByIdAsync(int id);
        Task<string> GetEpisodeTitleAsync(int seriesId, int seasonNumber, int episodeNumber);
    }
}
