using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext context;

        public SettingsController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (config == null)
            {
                return Ok(new SettingsDto());
            }

            return Ok(new SettingsDto
            {
                TmdbApiKey = config.TmdbApiKey,
                SourceDir = config.SourceDir,
                TargetMovieDir = config.TargetMovieDir,
                TargetSeriesDir = config.TargetSeriesDir,
                MoviePattern = config.MoviePattern,
                SeriesPattern = config.SeriesPattern,
                SeasonFolderPattern = config.SeasonFolderPattern,
                IgnoreFolders = config.IgnoreFolders,
                DefaultHwAccel = config.DefaultHwAccel,
                FfmpegPath = config.FfmpegPath
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] SettingsDto dto)
        {
            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (config == null)
            {
                config = new ConfigurationSetting();
                context.Settings.Add(config);
            }

            config.TmdbApiKey = dto.TmdbApiKey;
            config.SourceDir = dto.SourceDir;
            config.TargetMovieDir = dto.TargetMovieDir;
            config.TargetSeriesDir = dto.TargetSeriesDir;
            // Only overwrite pattern if it's provided, to prevent accidentally wiping defaults
            if (!string.IsNullOrWhiteSpace(dto.MoviePattern)) config.MoviePattern = dto.MoviePattern;
            if (!string.IsNullOrWhiteSpace(dto.SeriesPattern)) config.SeriesPattern = dto.SeriesPattern;
            if (!string.IsNullOrWhiteSpace(dto.SeasonFolderPattern)) config.SeasonFolderPattern = dto.SeasonFolderPattern;
            if (dto.IgnoreFolders != null) config.IgnoreFolders = dto.IgnoreFolders;
            if (!string.IsNullOrWhiteSpace(dto.DefaultHwAccel)) config.DefaultHwAccel = dto.DefaultHwAccel;

            config.FfmpegPath = dto.FfmpegPath;

            await context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }

    public class SettingsDto
    {
        public string TmdbApiKey { get; set; } = string.Empty;
        public string SourceDir { get; set; } = string.Empty;
        public string TargetMovieDir { get; set; } = string.Empty;
        public string TargetSeriesDir { get; set; } = string.Empty;
        public string MoviePattern { get; set; } = string.Empty;
        public string SeriesPattern { get; set; } = string.Empty;
        public string SeasonFolderPattern { get; set; } = string.Empty;
        public string IgnoreFolders { get; set; } = string.Empty;
        public string DefaultHwAccel { get; set; } = "qsv";
        public string FfmpegPath { get; set; } = string.Empty;
    }
}
