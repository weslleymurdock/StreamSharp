using System.ComponentModel.DataAnnotations;

namespace StreamSharp.Models;

/// <summary>
/// Modelo de entrada para requisição de download de vídeo.
/// </summary>
public class DownloadRequest
{
    /// <summary>
    /// URL completa do vídeo ou iframe a ser baixado.
    /// </summary>
    [Required(ErrorMessage = "A URL do vídeo é obrigatória.")]
    [Display(Name = "URL do Vídeo")]
    [Url(ErrorMessage = "Forneça uma URL válida contendo http:// ou https://.")]
    public string Url { get; set; } = string.Empty;
}


public enum DownloadJobStatus
{
    Queued,
    Downloading,
    Completed,
    Failed
}

public class DownloadJob
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DownloadJobStatus Status { get; set; } = DownloadJobStatus.Queued;
    public double Progress { get; set; }
    public string Message { get; set; } = "Na fila de processamento...";
    public string? FilePath { get; set; }
    public string? Error { get; set; }
}