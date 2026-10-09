using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using StreamSharp.Hubs;
using StreamSharp.Models;
using StreamSharp.Services.Abstract;

namespace StreamSharp.Controllers;

[ApiController]
[Route("api/video")]
[Produces(MediaTypeNames.Application.Json)]
public class VideoController(
    IVideoDownloaderService downloaderService,
    IDownloadJobManager jobManager,
    IHubContext<DownloadHub> hubContext,
    ILogger<VideoController> logger) : ControllerBase
{
    /// <summary>
    /// Inicia uma requisição de download de vídeo respeitando a fila de concorrência (Rate Limiting).
    /// </summary>
    [HttpPost("download")]
    [EnableRateLimiting("DownloadQueuePolicy")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, MediaTypeNames.Application.ProblemJson)]
    public IActionResult EnqueueDownload([FromBody] DownloadRequest request)
    {
        logger.LogInformation("Nova requisição de download recebida para URL: {Url}", request.Url);

        // 1. Cria o Job em memória com status "Queued"
        var job = jobManager.CreateJob(request.Url);

        // 2. Dispara a tarefa em background sem travar a resposta HTTP
        _ = Task.Run(async () =>
        {
            try
            {
                jobManager.UpdateProgress(job.Id, 0, "Iniciando download...");

                string filePath = await downloaderService.DownloadVideoAsync(
                    request.Url,
                    onProgress: (percent, message) =>
                    {
                        // Atualiza estado interno
                        jobManager.UpdateProgress(job.Id, percent, message);

                        // Notifica o SignalR Hub (envia para todos ou para o grupo com o jobId)
                        hubContext.Clients.Group(job.Id).SendAsync("ReceiveProgress", job.Id, percent, message);
                    });

                jobManager.MarkCompleted(job.Id, filePath);

                // Notifica a conclusão via SignalR
                await hubContext.Clients.Group(job.Id).SendAsync("DownloadCompleted", job.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no processamento em background do Job: {JobId}", job.Id);
                jobManager.MarkFailed(job.Id, ex.Message);
                await hubContext.Clients.Group(job.Id).SendAsync("DownloadFailed", job.Id, ex.Message);
            }
        });

        // 3. Retorna imediatamente HTTP 202 Accepted com o JobID para consulta
        return Accepted(new
        {
            jobId = job.Id,
            message = "Requisição aceita e adicionada à fila de download.",
            statusUrl = $"/api/video/status/{job.Id}",
            downloadUrl = $"/api/video/stream-file/{job.Id}"
        });
    }

    /// <summary>
    /// Consulta o progresso atual do download via HTTP Polling.
    /// </summary>
    [HttpGet("status/{fileId}")]
    [ProducesResponseType(typeof(DownloadJob), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public IActionResult GetStatus(string fileId)
    {
        var job = jobManager.GetJob(fileId);

        if (job is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Job Não Encontrado",
                Detail = "O ID de download fornecido é inválido ou expirou."
            });
        }

        return Ok(new
        {
            jobId = job.Id,
            status = job.Status.ToString(),
            progress = job.Progress,
            message = job.Message,
            isReady = job.Status == DownloadJobStatus.Completed,
            error = job.Error
        });
    }

    /// <summary>
    /// Transmite o arquivo .mp4 para o navegador assim que o status for 'Completed'.
    /// </summary>
    [HttpGet("stream-file/{fileId}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK, MediaTypeNames.Application.Octet)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public IActionResult StreamFile(string fileId)
    {
        var job = jobManager.GetJob(fileId);

        if (job is null || job.Status != DownloadJobStatus.Completed || string.IsNullOrEmpty(job.FilePath) || !System.IO.File.Exists(job.FilePath))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Arquivo Não Encontrado",
                Detail = "O arquivo solicitado ainda não está pronto, expirou ou não existe."
            });
        }

        var stream = new FileStream(
            job.FilePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 4096,
            options: FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        return File(stream, "video/mp4", fileDownloadName: $"{fileId}.mp4");
    }
}