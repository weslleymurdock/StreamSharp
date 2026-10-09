namespace StreamSharp.Services.Abstract;

/// <summary>
/// Contrato para o serviço responsável por realizar o download de vídeos.
/// </summary>
public interface IVideoDownloaderService
{
    /// <summary>
    /// Faz o download do vídeo a partir da URL fornecida.
    /// </summary>
    /// <param name="videoUrl">A URL do vídeo ou iframe a ser baixado.</param>
    /// <param name="onProgress">Ação a ser chamada durante o progresso do download.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O caminho físico completo do arquivo salvo no servidor.</returns>
    Task<string> DownloadVideoAsync(
        string videoUrl,
        Action<double, string>? onProgress = null,
        CancellationToken cancellationToken = default);
}