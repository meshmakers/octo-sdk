using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace Meshmakers.Octo.Communication.Plugs.Client;

public class DeviceManagementClient
{
    private readonly HubConnection _hubConnection;

    public DeviceManagementClient()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5015/meshtest/plugHub", options =>
            {
                options.Headers["Authorization"] = "Bearer your-access-token";
                options.Headers["CustomHeader"] = "CustomValue";
            })
            .Build();
    }
    
    public async Task RegisterPlugAsync(OctoObjectId plugObjectId)
    {
        await _hubConnection.StartAsync();

        await _hubConnection.SendAsync("RegisterPlug", plugObjectId);
    }
}