using Backend.Services.Interfaces;

namespace Backend.Services
{
    public class FileSystemMonitorService : IFileSystemMonitorService, IDisposable
    {
        private FileSystemWatcher? _watcher;
        private readonly IServiceScopeFactory _scopeFactory;
        
        public event EventHandler<string>? OnFileDetected;

        public FileSystemMonitorService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void StartMonitoring(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            _watcher = new FileSystemWatcher(directoryPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            _watcher.Created += OnCreated;
            _watcher.Renamed += OnRenamed;
        }

        private void OnCreated(object sender, FileSystemEventArgs e) => TriggerEventIfVideo(e.FullPath);
        private void OnRenamed(object sender, RenamedEventArgs e) => TriggerEventIfVideo(e.FullPath);

        private void TriggerEventIfVideo(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            var videoExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv" };

            if (videoExtensions.Contains(ext))
            {
                using var scope = _scopeFactory.CreateScope();
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
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}
