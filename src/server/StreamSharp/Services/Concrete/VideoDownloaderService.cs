using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.EventStream;
using Microsoft.Extensions.Logging;
using StreamSharp.Services.Abstract;

namespace StreamSharp.Services.Concrete;

/// <summary>
/// Implementação do serviço de download de vídeos utilizando yt-dlp e FFmpeg via CliWrap.
/// </summary>
/// <param name="logger">Instância do logger para registro de eventos e erros.</param>
public class VideoDownloaderService(ILogger<VideoDownloaderService> logger) : IVideoDownloaderService
{
    private static readonly Regex ProgressRegex = new(@"(\d{1,3}(?:\.\d+)?)%", RegexOptions.Compiled);

    /// <inheritdoc />
    public async Task<string> DownloadVideoAsync(
        string videoUrl,
        Action<double, string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(videoUrl) || !Uri.TryCreate(videoUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException("A URL fornecida é inválida ou está vazia.", nameof(videoUrl));
            }

            string tempFileName = Guid.NewGuid().ToString("N");
            string outputFilePath = Path.Combine(Path.GetTempPath(), $"{tempFileName}.mp4");

            var arguments = new[]
            {
                videoUrl,
                "-o", outputFilePath,
                "--merge-output-format", "mp4",
                "--no-playlist",
                "--newline" // Força o yt-dlp a emitir cada atualização de progresso em uma nova linha
            };

            // Escuta os eventos do processo em tempo real com CliWrap EventStream
            var cmd = Cli.Wrap("yt-dlp").WithArguments(arguments);

            await foreach (var cmdEvent in cmd.ListenAsync(cancellationToken))
            {
                if (cmdEvent is StandardOutputCommandEvent stdOut)
                {
                    string line = stdOut.Text;

                    // Captura o percentual via Regex
                    var match = ProgressRegex.Match(line);
                    if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double percent))
                    {
                        onProgress?.Invoke(percent, line);
                    }
                    else if (line.Contains("[Merger]") || line.Contains("Merging formats"))
                    {
                        onProgress?.Invoke(99.0, "Juntando áudio e vídeo (Costura)...");
                    }
                }
            }

            if (!File.Exists(outputFilePath))
            {
                throw new FileNotFoundException("Arquivo de vídeo resultante não foi gerado.", outputFilePath);
            }

            return outputFilePath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao baixar vídeo da URL: {VideoUrl}", videoUrl);
            throw;
        }
    }
}