using Backend.Services.Interfaces;

namespace Backend.Services
{
    public class CleanupService : ICleanupService
    {
        public void CleanDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            var junkExtensions = new[] { ".txt", ".nfo", ".exe", ".url" };
            
            // Delete junk files
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
            foreach(var file in files)
            {
                var ext = Path.GetExtension(file).ToLower();
                if (junkExtensions.Contains(ext))
                {
                    try { File.Delete(file); } catch { /* Ignore locked files */ }
                }
            }

            // Delete empty directories
            var dirs = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories)
                                .OrderByDescending(d => d.Length); // Deepest first

            foreach(var dir in dirs)
            {
                try 
                { 
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                    }
                } 
                catch { /* Ignore locked folders */ }
            }
        }
    }
}
