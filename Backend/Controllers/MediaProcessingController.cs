using Backend.Data;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaProcessingController(
        AppDbContext context, 
        IMediaProcessorService processor,
        IRenamerService renamer,
        ITmdbService tmdb) : ControllerBase
    {

        [HttpGet("queue")]
        public async Task<IActionResult> GetQueue()
        {
            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (config == null || string.IsNullOrWhiteSpace(config.SourceDir) || !Directory.Exists(config.SourceDir))
            {
                return Ok(new List<string>());
            }

            var extensions = new[] { ".mkv", ".mp4", ".avi", ".mov" };
            
            var ignoreList = config.IgnoreFolders?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList() ?? new List<string>();

            var files = Directory.GetFiles(config.SourceDir, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Where(f => !Path.GetFileName(f).ToLowerInvariant().Contains("sample"))
                .Where(f => !ignoreList.Any(ignore => f.Contains(ignore, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var resultList = new List<object>();
            foreach (var f in files)
            {
                var (title, year, season, episode) = renamer.ParseRawFilename(f);
                string posterUrl = "";
                int tmdbId = 0;

                if (!string.IsNullOrWhiteSpace(title))
                {
                    if (season.HasValue) 
                    {
                        var res = await tmdb.SearchSeriesAsync(title, year > 1900 ? year : null);
                        var first = res.FirstOrDefault();
                        if (first != null) { posterUrl = first.PosterUrl; tmdbId = first.Id; title = first.Title; year = first.Year; }
                    }
                    else 
                    {
                        var res = await tmdb.SearchMoviesAsync(title, year > 1900 ? year : null);
                        var first = res.FirstOrDefault();
                        if (first != null) { posterUrl = first.PosterUrl; tmdbId = first.Id; title = first.Title; year = first.Year; }
                    }
                }

                resultList.Add(new { 
                    file = f, 
                    autoMatch = new { title, year, season, episode, isSeries = season.HasValue, posterUrl, id = tmdbId } 
                });
            }

            return Ok(resultList);
        }

        [HttpPost("scan")]
        public async Task<IActionResult> TriggerScan()
        {
            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (config == null || string.IsNullOrWhiteSpace(config.SourceDir) || !Directory.Exists(config.SourceDir))
            {
                return BadRequest(new { message = "Source directory is not configured or does not exist." });
            }

            var extensions = new[] { ".mkv", ".mp4", ".avi", ".mov" };

            var ignoreList = config.IgnoreFolders?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList() ?? new List<string>();

            var files = Directory.GetFiles(config.SourceDir, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Where(f => !Path.GetFileName(f).ToLowerInvariant().Contains("sample"))
                .Where(f => !ignoreList.Any(ignore => f.Contains(ignore, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            int successCount = 0;
            foreach (var file in files)
            {
                // In a real application we might want to run this in a background worker, 
                // but for this demo, doing it synchronously allows us to return the count.
                bool success = await processor.ProcessFileAsync(file);
                if (success) successCount++;
            }

            return Ok(new { processed = successCount, total = files.Count });
        }

        [HttpGet("parse")]
        public IActionResult ParseFilename([FromQuery] string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return BadRequest("Filename is required.");

            // Pass the entire relative path so RenamerService can inspect the directory too
            var (title, year, season, episode) = renamer.ParseRawFilename(filename);
            
            return Ok(new { title, year, season, episode });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTmdb([FromQuery] string title, [FromQuery] int? year, [FromQuery] bool isSeries)
        {
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest("Title is required.");

            try
            {
                // Explicit ID search if title is entirely digits
                if (int.TryParse(title.Trim(), out int tmdbId))
                {
                    if (isSeries)
                    {
                        var result = await tmdb.GetSeriesByIdAsync(tmdbId);
                        return Ok(result == null ? new List<MediaMetadata>() : new List<MediaMetadata> { result });
                    }
                    else
                    {
                        var result = await tmdb.GetMovieByIdAsync(tmdbId);
                        return Ok(result == null ? new List<MediaMetadata>() : new List<MediaMetadata> { result });
                    }
                }

                if (isSeries)
                {
                    var results = await tmdb.SearchSeriesAsync(title, year > 1900 ? year : null);
                    return Ok(results);
                }
                else
                {
                    var results = await tmdb.SearchMoviesAsync(title, year > 1900 ? year : null);
                    return Ok(results);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public class ProcessRequestDto
        {
            public string FilePath { get; set; } = string.Empty;
            public Backend.Models.MediaMetadata? Metadata { get; set; }
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessSingleFile([FromBody] ProcessRequestDto dto)
        {
            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (config == null || string.IsNullOrWhiteSpace(config.SourceDir))
            {
                return BadRequest(new { message = "Source directory is not configured." });
            }

            var fullPath = Path.Combine(config.SourceDir, dto.FilePath);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound(new { message = "File not found locally." });
            }

            bool success = await processor.ProcessFileAsync(fullPath, dto.Metadata);
            if (success)
            {
                return Ok(new { success = true });
            }
            
            return BadRequest(new { message = "Failed to process file." });
        }
    }
}
