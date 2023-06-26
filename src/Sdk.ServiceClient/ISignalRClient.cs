using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
/// Interface of the SignalR client.
/// </summary>
/// <typeparam name="TOptions"></typeparam>
public interface ISignalRClient<out TOptions> where TOptions : SignalRClientOptions
{
    /// <summary>
    /// Client access token.
    /// </summary>
    IServiceClientAccessToken ClientAccessToken { get; }
    /// <summary>
    /// Options of the SignalR client.
    /// </summary>
    TOptions Options { get; }
    
    /// <summary>
    /// Returns the service URI of the SignalR hub
    /// </summary>
    Uri? ServiceUri { get; }
    
    /// <summary>
    /// Returns true if the SignalR client is connected to the hub and false otherwise.
    /// </summary>
    /// <remarks>
    /// False is only returned if the connection is interrupted or if the client is not started.
    /// See <see cref="HubConnectionState"/> for more details.
    /// </remarks>
    public bool IsAlive { get; }
    
    /// <summary>
    /// Starts the communication with the SignalR hub.
    /// </summary>
    /// <returns></returns>
    Task StartAsync();
    
    /// <summary>
    /// Stops the communication with the SignalR hub.
    /// </summary>
    /// <returns></returns>
    Task StopAsync();
}