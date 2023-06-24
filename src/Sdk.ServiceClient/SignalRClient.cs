using System;
using System.Net.Http;
using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.ServiceClient;

public class SignalRClient<TOptions> where TOptions : SignalRClientOptions
{
    private readonly string _hubName;
    private HubConnection? _hubConnection;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public SignalRClient(IOptions<TOptions> clientOptions,
        IServiceClientAccessToken serviceClientAccessToken, string hubName)
        : this(clientOptions.Value, serviceClientAccessToken, hubName)
    {
    }

    public SignalRClient(TOptions clientOptions,
        IServiceClientAccessToken serviceClientAccessToken, string hubName)
    {
        _hubName = hubName;
        ClientAccessToken = serviceClientAccessToken;
        Options = clientOptions;
    }
    
    public IServiceClientAccessToken ClientAccessToken { get; }

    public TOptions Options { get; }
    
    public Uri? ServiceUri { get; private set; }

    // ReSharper disable once MemberCanBePrivate.Global
    protected HubConnection HubConnection
    {
        get { return _hubConnection ??= CreateHubConnection(); }
    }

    public async Task StartAsync()
    {
        _logger.Info("Starting SignalR client...");

        await HubConnection.StartAsync();
        
        _logger.Info("SignalR client started. ConnectionId: {ConnectionId}", HubConnection.ConnectionId);
    }
    
    public async Task StopAsync()
    {
        _logger.Info("Stopping SignalR client...");
        
        await HubConnection.StopAsync();
        
        _logger.Info("SignalR client stopped.");
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
                options.HttpMessageHandlerFactory = (message) =>
                {
                    if (message is HttpClientHandler clientHandler)
                        // always verify the SSL certificate
                        clientHandler.ServerCertificateCustomValidationCallback +=
                            (_, _, _, _) => true;
                    return message;
                };
                // TODO: Handle authentication
                options.Headers["Authorization"] = "Bearer your-access-token";
                
                // Add optional headers to requests
                foreach (var header in Options.Headers)
                {
                    options.Headers[header.Key] = header.Value;
                }
            })
            .Build();
        
        hubConnection.Closed += async _ =>
        {
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await hubConnection.StartAsync();
        };
        
        return hubConnection;
    }
}