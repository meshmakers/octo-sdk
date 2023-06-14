using System.Threading;
using System.Threading.Tasks;

namespace Meshmakers.Octo.Sdk.PlugExecutor;

/// <summary>
/// Interface of task/job based plugs.
/// </summary>
/// <remarks>
/// Use this interface for plugs that are executed in a low frequency, like once an hour, once a day or once a week.
/// </remarks>
public interface IPlugJob
{
    Task ExecuteAsync(PlugStartup tenantId, CancellationToken stoppingToken);
}