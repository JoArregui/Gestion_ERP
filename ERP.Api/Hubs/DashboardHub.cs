using Microsoft.AspNetCore.SignalR;

namespace ERP.Api.Hubs
{
    // Este Hub gestiona las conexiones de los clientes
    public class DashboardHub : Hub
    {
        public async Task SendUpdate()
        {
            // Notifica a todos los clientes conectados que hay datos nuevos
            await Clients.All.SendAsync("ReceiveDashboardUpdate");
        }
    }
}