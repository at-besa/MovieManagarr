namespace Backend.Services.Interfaces
{
    public interface IFileSystemMonitorService
    {
        void StartMonitoring(string directoryPath);
        void StopMonitoring();
        event EventHandler<string> OnFileDetected;
    }
}
