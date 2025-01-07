using Microsoft.Extensions.Hosting;

namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
/// The hosted service for the execution of an adapter.
/// </summary>
/// <param name="adapterExecutionService"></param>
public class HostedAdapterExecutionService(AdapterExecutionService adapterExecutionService) : IHostedService
{
    /// <summary>
    /// Start the adapter execution service
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await adapterExecutionService.StartAsync(cancellationToken);
    }

    
    /// <summary>
    /// Stop the adapter execution service
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await adapterExecutionService.StopAsync(cancellationToken);
    }
}