using Backend.Data;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class MediaProcessorService(IServiceScopeFactory scopeFactory, IFileSystemMonitorService monitor) : IMediaProcessorService
    {
        private bool isMonitoringInitialized = false;

        public void StartMonitoring(string directoryPath)
        {
            if (!isMonitoringInitialized)
            {
                monitor.OnFileDetected += async (s, filePath) => await ProcessFileAsync(filePath);
                isMonitoringInitialized = true;
            }
            monitor.StartMonitoring(directoryPath);
        }

        public async Task<bool> ProcessFileAsync(string filePath, MediaMetadata? manualMatch = null)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var config = await db.Settings.FirstOrDefaultAsync();

                if (config == null || string.IsNullOrEmpty(config.SourceDir) || string.IsNullOrEmpty(config.TargetMovieDir) || string.IsNullOrEmpty(config.TargetSeriesDir))
                {
                    Console.WriteLine("MediaProcessor Error: Directories not fully configured.");
                    return false;
                }

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var ext = Path.GetExtension(filePath);

                var renamer = scope.ServiceProvider.GetRequiredService<IRenamerService>();
                var (Title, Year, Season, Episode) = renamer.ParseRawFilename(fileName);

                var tmdb = scope.ServiceProvider.GetRequiredService<ITmdbService>();
                var metadata = manualMatch;
                if (metadata == null)
                {
                    if (Season.HasValue)
                    {
                        var results = await tmdb.SearchSeriesAsync(Title, Year > 1900 ? Year : null);
                        metadata = results.FirstOrDefault();
                    }
                    else
                    {
                        var results = await tmdb.SearchMoviesAsync(Title, Year > 1900 ? Year : null);
                        metadata = results.FirstOrDefault();
                    }
                }

                if (metadata == null)
                {
                    Console.WriteLine($"MediaProcessor Note: No metadata match found for {fileName}. Manual intervention required.");
                    return false;
                }

                var analyzer = scope.ServiceProvider.GetRequiredService<IMediaAnalysisService>();
                var analysis = await analyzer.AnalyzeFileAsync(filePath, config.FfmpegPath);

                string episodeTitle = "";
                if (Season.HasValue && Episode.HasValue)
                {
                    episodeTitle = await tmdb.GetEpisodeTitleAsync(metadata.Id, Season.Value, Episode.Value);
                }

                var targetPattern = Season.HasValue ? config.SeriesPattern : config.MoviePattern;
                
                var newFilename = renamer.BuildTargetFilename(metadata, analysis, targetPattern, Season, Episode, episodeTitle) + ext;
                
                var targetDir = Season.HasValue ? config.TargetSeriesDir : config.TargetMovieDir;
                var showDir = Path.Combine(targetDir, metadata.Title + " (" + metadata.Year + ")");
                
                if (Season.HasValue)
                {
                    string seasonFolderName = config.SeasonFolderPattern?.Replace("{S}", Season.Value.ToString("D2")) ?? $"Season {Season.Value:D2}";
                    var seasonDir = Path.Combine(showDir, seasonFolderName);
                    Directory.CreateDirectory(seasonDir);
                    File.Move(filePath, Path.Combine(seasonDir, newFilename), true);
                } 
                else
                {
                    Directory.CreateDirectory(showDir);
                    File.Move(filePath, Path.Combine(showDir, newFilename), true);
                }

                var cleanup = scope.ServiceProvider.GetRequiredService<ICleanupService>();
                cleanup.CleanDirectory(Path.GetDirectoryName(filePath)!);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MediaProcessor Exception: {ex.Message}");
                return false;
            }
        }
    }
}
