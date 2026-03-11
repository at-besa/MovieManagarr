using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileSystemController : ControllerBase
    {
        [HttpGet("browse")]
        public IActionResult Browse([FromQuery] string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    var drives = DriveInfo.GetDrives()
                                          .Where(d => d.IsReady)
                                          .Select(d => d.Name)
                                          .ToList();
                    
                    return Ok(new
                    {
                        currentPath = "",
                        directories = drives
                    });
                }

                if (!Directory.Exists(path))
                {
                    return NotFound(new { message = "Directory not found." });
                }

                var dirs = Directory.GetDirectories(path)
                                    .Where(d => !new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.Hidden) &&
                                                !new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.System))
                                    .OrderBy(d => d)
                                    .ToList();

                return Ok(new
                {
                    currentPath = path,
                    directories = dirs
                });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { message = "Access to the path is denied." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
