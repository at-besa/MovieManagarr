using Backend.Data;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/transcode")]
    public class TranscodingController : ControllerBase
    {
        private readonly AppDbContext context;
        private readonly IMediaAnalysisService analyzer;
        private readonly ITranscodingService transcoder;

        public TranscodingController(AppDbContext context, IMediaAnalysisService analyzer, ITranscodingService transcoder)
        {
            this.context = context;
            this.analyzer = analyzer;
            this.transcoder = transcoder;
        }

        /// <summary>
        /// Lists all video files from target movie and series directories for transcode selection.
        /// </summary>
        [HttpGet("files")]
        public async Task<IActionResult> GetFiles()
        {
            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (config == null) return Ok(new List<object>());

            var extensions = new[] { ".mkv", ".mp4", ".avi", ".mov" };
            var files = new List<object>();

            foreach (var dir in new[] { config.SourceDir, config.TargetMovieDir, config.TargetSeriesDir })
            {
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) continue;

                foreach (var f in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    if (!extensions.Contains(Path.GetExtension(f).ToLowerInvariant())) continue;
                    if (f.Contains("sample", StringComparison.OrdinalIgnoreCase)) continue;

                    var fi = new FileInfo(f);
                    files.Add(new
                    {
                        path = f,
                        relativePath = Path.GetRelativePath(dir, f),
                        directory = dir == config.SourceDir ? "Source" : (dir == config.TargetMovieDir ? "Movies" : "Series"),
                        fileName = fi.Name,
                        sizeBytes = fi.Length
                    });
                }
            }

            return Ok(files);
        }

        /// <summary>
        /// Analyze a file's media properties including bitrate, duration, codec details.
        /// </summary>
        [HttpGet("analyze")]
        public async Task<IActionResult> Analyze([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return BadRequest(new { message = "File not found." });

            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            var analysis = await analyzer.AnalyzeFileAsync(path, config?.FfmpegPath);

            return Ok(analysis);
        }

        /// <summary>
        /// Estimate output file size based on source analysis and transcode settings.
        /// </summary>
        [HttpPost("estimate")]
        public async Task<IActionResult> Estimate([FromBody] EstimateRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FilePath) || !System.IO.File.Exists(dto.FilePath))
                return BadRequest(new { message = "File not found." });

            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            var analysis = await analyzer.AnalyzeFileAsync(dto.FilePath, config?.FfmpegPath);
            long estimated = transcoder.EstimateOutputSize(analysis, dto.Settings);

            return Ok(new
            {
                estimatedBytes = estimated,
                originalBytes = analysis.FileSizeBytes,
                savingsPercent = analysis.FileSizeBytes > 0
                    ? Math.Round((1.0 - (double)estimated / analysis.FileSizeBytes) * 100, 1)
                    : 0
            });
        }

        /// <summary>
        /// Start a background transcode job.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartTranscode([FromBody] StartTranscodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FilePath) || !System.IO.File.Exists(dto.FilePath))
                return BadRequest(new { message = "File not found." });

            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            
            // Apply default HW accel if not specified
            if (string.IsNullOrWhiteSpace(dto.Settings.HwAcceleration))
            {
                dto.Settings.HwAcceleration = config?.DefaultHwAccel ?? "qsv";
            }

            // Get size estimate before starting
            var analysis = await analyzer.AnalyzeFileAsync(dto.FilePath, config?.FfmpegPath);
            var job = await transcoder.StartTranscodeAsync(dto.FilePath, dto.Settings, config?.FfmpegPath ?? "");
            job.EstimatedOutputBytes = transcoder.EstimateOutputSize(analysis, dto.Settings);

            return Ok(new { jobId = job.JobId, estimatedBytes = job.EstimatedOutputBytes });
        }

        /// <summary>
        /// Get the real-time status of a transcode job.
        /// </summary>
        [HttpGet("status/{jobId}")]
        public IActionResult GetStatus(string jobId)
        {
            var job = transcoder.GetJobStatus(jobId);
            if (job == null) return NotFound(new { message = "Job not found." });

            return Ok(new
            {
                job.JobId,
                job.Status,
                job.ProgressPercent,
                job.EstimatedOutputBytes,
                job.ActualOutputBytes,
                job.ErrorMessage,
                job.StartedAt,
                job.CompletedAt
            });
        }

        /// <summary>
        /// Cancels a running transcode job.
        /// </summary>
        [HttpPost("cancel/{jobId}")]
        public IActionResult CancelJob(string jobId)
        {
            var success = transcoder.CancelJob(jobId);
            if (!success) return BadRequest(new { message = "Job not found or already completed/canceled." });

            return Ok(new { message = "Job canceled successfully." });
        }

        /// <summary>
        /// SSE endpoint for real-time progress streaming
        /// </summary>
        [HttpGet("progress/{jobId}")]
        public async Task GetProgress(string jobId)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            while (true)
            {
                var job = transcoder.GetJobStatus(jobId);
                if (job == null)
                {
                    await Response.WriteAsync($"data: {{\"error\": \"Job not found\"}}\n\n");
                    break;
                }

                var data = System.Text.Json.JsonSerializer.Serialize(new
                {
                    progressPercent = job.ProgressPercent,
                    status = job.Status,
                    actualOutputBytes = job.ActualOutputBytes,
                    errorMessage = job.ErrorMessage
                });

                await Response.WriteAsync($"data: {data}\n\n");
                await Response.Body.FlushAsync();

                if (job.Status == "Done" || job.Status == "Failed") break;

                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Generate a 15s preview clip of the file
        /// </summary>
        [HttpPost("preview")]
        public async Task<IActionResult> GeneratePreview([FromBody] PreviewRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FilePath) || !System.IO.File.Exists(dto.FilePath))
                return BadRequest(new { message = "File not found." });

            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();

            try
            {
                var filename = await transcoder.GeneratePreviewClipAsync(dto.FilePath, config?.FfmpegPath ?? "");
                return Ok(new { previewUrl = $"/api/transcode/preview/{filename}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Preview generation failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Serve generated preview clip
        /// </summary>
        [HttpGet("preview/{filename}")]
        public IActionResult ServePreview(string filename)
        {
            var previewDir = Path.Combine(Path.GetTempPath(), "MovieManager_Previews");
            var filePath = Path.Combine(previewDir, filename);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Preview file not found." });

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(stream, "video/mp4", enableRangeProcessing: true);
        }
    }

    public class EstimateRequestDto
    {
        public string FilePath { get; set; } = string.Empty;
        public TranscodeSettings Settings { get; set; } = new();
    }

    public class StartTranscodeDto
    {
        public string FilePath { get; set; } = string.Empty;
        public TranscodeSettings Settings { get; set; } = new();
    }

    public class PreviewRequestDto
    {
        public string FilePath { get; set; } = string.Empty;
    }
}
