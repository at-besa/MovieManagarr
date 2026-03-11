using Backend.Services.Interfaces;

namespace Backend.Services
{
    public class FileSystemMonitorService(IServiceScopeFactory scopeFactory) : IFileSystemMonitorService, IDisposable
    {
        private FileSystemWatcher? watcher;
        
        public event EventHandler<string>? OnFileDetected;

        public void StartMonitoring(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            watcher = new FileSystemWatcher(directoryPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            watcher.Created += OnCreated;
            watcher.Renamed += OnRenamed;
        }

        private void OnCreated(object sender, FileSystemEventArgs e) => TriggerEventIfVideo(e.FullPath);
        private void OnRenamed(object sender, RenamedEventArgs e) => TriggerEventIfVideo(e.FullPath);

        private void TriggerEventIfVideo(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            var videoExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv" };

            if (videoExtensions.Contains(ext))
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Backend.Data.AppDbContext>();
                var config = db.Settings.FirstOrDefault();
                
                if (config != null && !string.IsNullOrWhiteSpace(config.IgnoreFolders))
                {
                    var ignoreList = config.IgnoreFolders
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
                        
                    if (ignoreList.Any(ignore => filePath.Contains(ignore, StringComparison.OrdinalIgnoreCase)))
                    {
                        return; // Drop event silently
                    }
                }

                // Delay slightly to ensure file copy completes
                Task.Delay(5000).ContinueWith(_ => OnFileDetected?.Invoke(this, filePath));
            }
        }

        public void StopMonitoring()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}
