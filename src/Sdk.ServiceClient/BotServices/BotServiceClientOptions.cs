namespace Meshmakers.Octo.Sdk.ServiceClient.BotServices;

/// <summary>
///     Options for the <see cref="BotServicesClient" />.
/// </summary>
/// <remarks>
///     Deliberately without a <c>TenantId</c> (AB#5060). Every tenant-addressed operation of this client
///     already takes its target tenant as the first method argument, and that target changes from call to
///     call — a parent tenant's administrator backs up a child tenant with their own token. An ambient
///     tenant on the options could not express that: the base URI is built once and cached
///     (<see cref="ServiceClient.ServiceUri" />), and the client is registered as a singleton in
///     <c>octo-cli</c>, so the first tenant seen would be frozen for the process lifetime.
/// </remarks>
public class BotServiceClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     Default size of one part of a tus upload: 50 MiB, the part size Refinery Studio uses. It stays clearly
    ///     below the ingress body limit of 100m on staging-1, prod-1 and prod-2.
    /// </summary>
    public const long DefaultTusPartSizeBytes = 50L * 1024 * 1024;

    /// <summary>
    ///     Smallest accepted part size (1 MiB); smaller parts only multiply the round trips.
    /// </summary>
    public const long MinTusPartSizeBytes = 1L * 1024 * 1024;

    /// <summary>
    ///     Largest accepted part size (90 MiB): the ingress body limit of the managed environments is 100m, and the
    ///     request needs headroom below it.
    /// </summary>
    public const long MaxTusPartSizeBytes = 90L * 1024 * 1024;

    private long _tusPartSizeBytes = DefaultTusPartSizeBytes;

    /// <summary>
    ///     The maximum number of bytes sent in one request of a tus upload (restore of a backup, import of archive
    ///     data). The default is 50 MiB (<see cref="DefaultTusPartSizeBytes" />). A value outside
    ///     <see cref="MinTusPartSizeBytes" /> and <see cref="MaxTusPartSizeBytes" /> is rejected, because a
    ///     part above the body limit of the ingress is refused with 413 and never reaches the bot service.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the accepted range.</exception>
    public long TusPartSizeBytes
    {
        get => _tusPartSizeBytes;
        set
        {
            if (value is < MinTusPartSizeBytes or > MaxTusPartSizeBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    $"The tus part size must be between {MinTusPartSizeBytes} bytes (1 MiB) and " +
                    $"{MaxTusPartSizeBytes} bytes (90 MiB); the ingress body limit of the managed environments is " +
                    "100 MiB, a larger part would be refused with 413. The default is 50 MiB.");
            }

            _tusPartSizeBytes = value;
        }
    }
}
