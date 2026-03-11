using Backend.Data;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using TMDbLib.Client;

namespace Backend.Services
{
    public class TmdbService : ITmdbService
    {
        private readonly AppDbContext _context;

        public TmdbService(AppDbContext context)
        {
            _context = context;
        }

        private async Task<TMDbClient> GetClientAsync()
        {
            var config = await _context.Settings.FirstOrDefaultAsync();
            var apiKey = config?.TmdbApiKey ?? "";
            return new TMDbClient(apiKey);
        }

        public async Task<List<MediaMetadata>> SearchMoviesAsync(string query, int? year = null)
        {
            var client = await GetClientAsync();
            if (string.IsNullOrEmpty(client.ApiKey)) return new List<MediaMetadata>();

            var results = await client.SearchMovieAsync(query, language: "de-DE", page: 0, includeAdult: false, year: year ?? 0);
            if (results?.Results == null) return new List<MediaMetadata>();

            return results.Results.Select(r => new MediaMetadata
            {
                Id = r.Id,
                Title = r.Title ?? "Unknown Title",
                Overview = r.Overview ?? "",
                Year = r.ReleaseDate?.Year ?? 0,
                MediaType = "Movie",
                PosterUrl = string.IsNullOrEmpty(r.PosterPath) ? "" : $"https://image.tmdb.org/t/p/w500{r.PosterPath}"
            }).ToList();
        }

        public async Task<List<MediaMetadata>> SearchSeriesAsync(string query, int? year = null)
        {
            var client = await GetClientAsync();
            if (string.IsNullOrEmpty(client.ApiKey)) return new List<MediaMetadata>();

            var results = await client.SearchTvShowAsync(query, language: "de-DE", page: 0, includeAdult: false, firstAirDateYear: year ?? 0);
            if (results?.Results == null) return new List<MediaMetadata>();

            return results.Results.Select(r => new MediaMetadata
            {
                Id = r.Id,
                Title = r.Name ?? "Unknown Title",
                Overview = r.Overview ?? "",
                Year = r.FirstAirDate?.Year ?? 0,
                MediaType = "Series",
                PosterUrl = string.IsNullOrEmpty(r.PosterPath) ? "" : $"https://image.tmdb.org/t/p/w500{r.PosterPath}"
            }).ToList();
        }

        public async Task<MediaMetadata?> GetMovieByIdAsync(int id)
        {
            var client = await GetClientAsync();
            if (string.IsNullOrEmpty(client.ApiKey)) return null;

            var r = await client.GetMovieAsync(id, language: "de-DE");
            if (r == null) return null;

            return new MediaMetadata
            {
                Id = r.Id,
                Title = r.Title ?? "Unknown Title",
                Overview = r.Overview ?? "",
                Year = r.ReleaseDate?.Year ?? 0,
                MediaType = "Movie",
                PosterUrl = string.IsNullOrEmpty(r.PosterPath) ? "" : $"https://image.tmdb.org/t/p/w500{r.PosterPath}"
            };
        }

        public async Task<MediaMetadata?> GetSeriesByIdAsync(int id)
        {
            var client = await GetClientAsync();
            if (string.IsNullOrEmpty(client.ApiKey)) return null;

            var r = await client.GetTvShowAsync(id, language: "de-DE");
            if (r == null) return null;

            return new MediaMetadata
            {
                Id = r.Id,
                Title = r.Name ?? "Unknown Title",
                Overview = r.Overview ?? "",
                Year = r.FirstAirDate?.Year ?? 0,
                MediaType = "Series",
                PosterUrl = string.IsNullOrEmpty(r.PosterPath) ? "" : $"https://image.tmdb.org/t/p/w500{r.PosterPath}"
            };
        }

        public async Task<string> GetEpisodeTitleAsync(int seriesId, int seasonNumber, int episodeNumber)
        {
            var client = await GetClientAsync();
            if (string.IsNullOrEmpty(client.ApiKey)) return string.Empty;

            var r = await client.GetTvEpisodeAsync(seriesId, seasonNumber, episodeNumber, language: "de-DE");
            if (r == null || string.IsNullOrWhiteSpace(r.Name)) return string.Empty;

            return r.Name;
        }
    }
}
