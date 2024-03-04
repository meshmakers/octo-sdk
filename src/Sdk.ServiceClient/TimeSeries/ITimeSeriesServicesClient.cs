namespace Meshmakers.Octo.Sdk.ServiceClient.TimeSeries;

/// <summary>
///     Interface for the TimeSeries services.
/// </summary>
public interface ITimeSeriesServicesClient : IServiceClient
{
    /// <summary>
    ///     Enables the timeseries for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task EnableAsync(string tenantId);

    /// <summary>
    ///     Disables timeseries  for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task DisableAsync(string tenantId);
}