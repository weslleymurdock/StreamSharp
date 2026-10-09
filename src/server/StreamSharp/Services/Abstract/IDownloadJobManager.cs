using StreamSharp.Models;

namespace StreamSharp.Services.Abstract;

public interface IDownloadJobManager
{
    DownloadJob CreateJob(string url);
    DownloadJob? GetJob(string jobId);
    void UpdateProgress(string jobId, double progress, string message);
    void MarkCompleted(string jobId, string filePath);
    void MarkFailed(string jobId, string error);
}