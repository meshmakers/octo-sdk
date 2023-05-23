using System;
using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.Client.PlugControllerServices;

public class SignalRClient
{
    private readonly string _hubName;
    private HubConnection? _hubConnection;

    public SignalRClient(IOptions<PlugControllerClientOptions> plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken, string hubName)
        : this(plugControllerServiceClientOptions.Value, plugControllerServiceAccessToken, hubName)
    {
    }

    public SignalRClient(PlugControllerClientOptions plugControllerServiceClientOptions,
        IPlugControllerServiceClientAccessToken plugControllerServiceAccessToken, string hubName)
    {
        _hubName = hubName;
        AccessToken = plugControllerServiceAccessToken;
        Options = plugControllerServiceClientOptions;
    }
    
    public IPlugControllerServiceClientAccessToken AccessToken { get; }

    public PlugControllerClientOptions Options { get; }
    
    public Uri? ServiceUri { get; private set; }

    // ReSharper disable once MemberCanBePrivate.Global
    protected HubConnection HubConnection
    {
        get { return _hubConnection ??= CreateHubConnection(); }
    }

    public async Task StartAsync()
    {
        await HubConnection.StartAsync();
    }
    
    public async Task StopAsync()
    {
        await HubConnection.StopAsync();
    }
    
    private HubConnection CreateHubConnection()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Plug Controller service URI is not configured.");
        }

        if (string.IsNullOrWhiteSpace(Options.TenantId))
        {
            throw new ServiceConfigurationMissingException("TenantId is not configured.");
        }

        ServiceUri = new Uri(Options.EndpointUri).Append(Options.TenantId).Append(_hubName);
        
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(ServiceUri, options =>
            {
                options.Headers["Authorization"] = "Bearer your-access-token";
                options.Headers["CustomHeader"] = "CustomValue";
            })
            .Build();
        
        return hubConnection;
    }
}