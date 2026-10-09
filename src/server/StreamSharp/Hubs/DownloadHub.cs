using Microsoft.AspNetCore.SignalR;
using StreamSharp.Services.Abstract;

namespace StreamSharp.Hubs;

public class DownloadHub : Hub
{
    /// <summary>
    /// Registra a conexão do cliente para receber atualizações de um Job específico.
    /// </summary>
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
    }
}
