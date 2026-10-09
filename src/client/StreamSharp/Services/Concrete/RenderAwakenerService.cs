using System.Net.Http.Json;

namespace StreamSharp.Services.Concrete;

public class RenderAwakenerService(
    IHttpClientFactory httpClientFactory,
    ILogger<RenderAwakenerService> logger) : Abstract.IRenderAwakenerService
{
    private const int MaxAttempts = 30; // 30 tentativas * 3s = até 90 segundos de tolerância para o cold start do Render
    private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromSeconds(3);

    public async Task<bool> EnsureApiIsAwakeAsync(Action<int, string>? onAttempt = null, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("StreamSharpApi");
        int currentAttempt = 0;

        logger.LogInformation("Iniciando verificação de disponibilidade (Awakeness) da Web API.");

        while (currentAttempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
        {
            currentAttempt++;
            string attemptMessage = $"Tentativa {currentAttempt} de {MaxAttempts}: Aguardando resposta do servidor...";

            logger.LogInformation("{Message}", attemptMessage);
            onAttempt?.Invoke(currentAttempt, attemptMessage);

            try
            {
                using var response = await client.GetAsync("api/health", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var health = await response.Content.ReadFromJsonAsync<Models.HealthCheckResponse>(cancellationToken: cancellationToken);

                    if (health is not null && health.IsHealthy)
                    {
                        logger.LogInformation("Servidor online e pronto! Status: Healthy. Ciclos necessários: {Attempts}", currentAttempt);
                        onAttempt?.Invoke(currentAttempt, "Servidor conectado e pronto para uso!");
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Enquanto o Render estiver subindo o container Linux, recusas de conexão e timeouts são normais
                logger.LogWarning("API indisponível na tentativa {Attempt}. Detalhe: {Error}", currentAttempt, ex.Message);
            }

            try
            {
                await Task.Delay(DelayBetweenAttempts, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Processo de verificação cancelado.");
                return false;
            }
        }

        logger.LogError("O servidor não respondeu dentro do tempo limite.");
        onAttempt?.Invoke(currentAttempt, "Não foi possível conectar ao servidor. Tente recarregar a página.");
        return false;
    }
}
