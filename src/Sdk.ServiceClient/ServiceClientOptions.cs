namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Configuration options of data source client
/// </summary>
public class ServiceClientOptions
{
    /// <summary>
    ///     The root uri of core services
    /// </summary>
    public string? EndpointUri { get; set; }

    /// <summary>
    ///     Maximum request duration in milliseconds.
    /// </summary>
    public int MaxTimeout { get; set; } = 100000;

    /// <summary>
    ///     Default of <see cref="StreamReadIdleTimeout" />: two minutes.
    /// </summary>
    public static readonly TimeSpan DefaultStreamReadIdleTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Longest time a streamed response body (tenant dump download, file download) may deliver no data before the
    ///     read fails with a <see cref="TimeoutException" /> (AB#8920). The clock restarts with every read that
    ///     returns data, so a slow but progressing download is never cut off; only a connection that stalls (for
    ///     example one that died without the client noticing) is. Default <see cref="DefaultStreamReadIdleTimeout" />;
    ///     zero or negative = no idle timeout.
    /// </summary>
    public TimeSpan StreamReadIdleTimeout { get; set; } = DefaultStreamReadIdleTimeout;
}
