using System.Collections.Concurrent;
using StreamSharp.Models;
using StreamSharp.Services.Abstract;

namespace StreamSharp.Services.Concrete;

public class DownloadJobManager : IDownloadJobManager
{
    private readonly ConcurrentDictionary<string, DownloadJob> _jobs = new();

    public DownloadJob CreateJob(string url)
    {
        string jobId = Guid.NewGuid().ToString("N");
        var job = new DownloadJob
        {
            Id = jobId,
            Url = url,
            Status = DownloadJobStatus.Queued,
            Progress = 0,
            Message = "Aguardando na fila de download..."
        };

        _jobs[jobId] = job;
        return job;
    }

    public DownloadJob? GetJob(string jobId) => _jobs.TryGetValue(jobId, out var job) ? job : null;

    public void UpdateProgress(string jobId, double progress, string message)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = DownloadJobStatus.Downloading;
            job.Progress = progress;
            job.Message = message;
        }
    }

    public void MarkCompleted(string jobId, string filePath)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = DownloadJobStatus.Completed;
            job.Progress = 100;
            job.Message = "Download concluído com sucesso!";
            job.FilePath = filePath;
        }
    }

    public void MarkFailed(string jobId, string error)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = DownloadJobStatus.Failed;
            job.Message = "Ocorreu um erro no download.";
            job.Error = error;
        }
    }
}