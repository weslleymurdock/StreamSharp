namespace StreamSharp.Services.Abstract;

public interface IRenderAwakenerService
{
    /// <summary>
    /// Consulta continuamente o endpoint /api/health da API até obter um status Healthy
    /// ou até o CancellationToken ser acionado.
    /// </summary>
    /// <param name="onAttempt">Callback invocado a cada ciclo informando a tentativa atual e a mensagem.</param>
    /// <param name="cancellationToken">Token para cancelar a rotina de polling.</param>
    /// <returns>Retorna true se a API respondeu com status 'Healthy', caso contrário false.</returns>
    Task<bool> EnsureApiIsAwakeAsync(Action<int, string>? onAttempt = null, CancellationToken cancellationToken = default);
}
