using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly AppDbContext context;

        public SystemController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetSystemInfo()
        {
            var config = await context.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();

            string dbStatus = "Offline";
            try
            {
                var canConnect = await context.Database.CanConnectAsync();
                if (canConnect) dbStatus = "Online";
            }
            catch { }

            string ffmpegStatus = "Not Found";
            string ffmpegVersion = "";
            var ffmpegPath = string.IsNullOrWhiteSpace(config?.FfmpegPath) ? "ffmpeg" : config.FfmpegPath;

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        ffmpegStatus = "Online";
                        ffmpegVersion = output;
                    }
                }
            }
            catch { }

            // Using Process.GetCurrentProcess().StartTime can throw access denied in some environments,
            // but usually safe for the current app's own process.
            DateTime? startTime = null;
            try
            {
                startTime = Process.GetCurrentProcess().StartTime;
            }
            catch { }

            return Ok(new
            {
                Version = "1.0.0",
                OsVersion = RuntimeInformation.OSDescription,
                DotNetVersion = RuntimeInformation.FrameworkDescription,
                StartTime = startTime,
                DatabaseStatus = dbStatus,
                FfmpegStatus = ffmpegStatus,
                FfmpegVersion = ffmpegVersion
            });
        }
    }
}
