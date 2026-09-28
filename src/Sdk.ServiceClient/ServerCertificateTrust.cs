using System.Net.Security;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     The one switch that decides whether this process validates the TLS certificates of the Octo
///     services it calls (AB#5303 items 1 and 2).
/// </summary>
/// <remarks>
///     <para>
///         🔴 <b>Process-wide on purpose, and static on purpose.</b> Whether a private or self-signed
///         certificate can be trusted is a property of the machine the process runs on, not of an
///         individual client object — and the SDK reaches its services through <b>four</b> unrelated
///         HTTP stacks (SignalR, RestSharp, GraphQL.Client, and bare <c>HttpClient</c> plus Duende's
///         discovery cache), each with its own injection point and none of them sharing options. A
///         per-options flag would have to be threaded through all four and could disagree with
///         itself; one gate cannot.
///     </para>
///     <para>
///         🔴 <b>What this replaces.</b> Two mechanisms, neither of which worked as its name says.
///         <c>SignalRClient</c> attached <c>ServerCertificateCustomValidationCallback => true</c> to
///         every hub connection unconditionally — no <c>#if</c>, no option, in every environment
///         including production, under a comment reading <i>"always verify the SSL certificate"</i>.
///         Meanwhile <c>AdapterOptions.IgnoreCertificateValidation</c> set
///         <c>ServicePointManager.ServerCertificateValidationCallback</c>, which
///         <c>SocketsHttpHandler</c> has ignored since .NET Core: setting it changed nothing, as a
///         running pool member demonstrated. So the hub connection was unverified everywhere and the
///         switch meant to control that was inert — the reason a member could reach the controller
///         over a dev certificate while its OIDC discovery failed on the very same one.
///     </para>
///     <para>
///         <b>Production is refused, unknown is allowed.</b> The gate reads
///         <c>ASPNETCORE_ENVIRONMENT</c> / <c>DOTNET_ENVIRONMENT</c> and declines only when one of
///         them explicitly says Production. An unset environment — which is most adapter pods — is
///         allowed and logs a warning naming the switch, because refusing there would silently take
///         out every dev cluster that never set the variable, and a bypass nobody can see is the
///         defect this whole item is about.
///     </para>
/// </remarks>
public static class ServerCertificateTrust
{
    private const string ProductionEnvironmentName = "Production";

    private static readonly Lock Gate = new();
    private static bool _allowAnyServerCertificate;

    /// <summary>
    ///     Whether this process currently accepts any server certificate. False unless a host has
    ///     called <see cref="AllowAnyServerCertificate" /> and the environment permitted it.
    /// </summary>
    public static bool AllowsAnyServerCertificate
    {
        get
        {
            lock (Gate)
            {
                return _allowAnyServerCertificate;
            }
        }
    }

    /// <summary>
    ///     Turns certificate validation off for every HTTP stack in this process. Called by a host
    ///     that was configured with <c>IgnoreCertificateValidation</c>.
    /// </summary>
    /// <param name="logger">Receives the warning, or the refusal. Optional.</param>
    /// <returns>
    ///     <c>true</c> when the bypass is now in force; <c>false</c> when the environment refused it,
    ///     in which case the process keeps validating.
    /// </returns>
    public static bool AllowAnyServerCertificate(ILogger? logger = null)
    {
        var environment = CurrentEnvironmentName();

        if (string.Equals(environment, ProductionEnvironmentName, StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogError(
                "IgnoreCertificateValidation is set, but the environment is '{Environment}'. Refusing to " +
                "disable TLS certificate validation; the process keeps validating. Install the certificate " +
                "authority in the container's trust store instead (AB#5303).", environment);
            return false;
        }

        lock (Gate)
        {
            _allowAnyServerCertificate = true;
        }

        logger?.LogWarning(
            "TLS certificate validation is DISABLED for every outgoing Octo service call in this process " +
            "(IgnoreCertificateValidation, environment '{Environment}'). Never set this outside development.",
            environment ?? "<unset>");

        return true;
    }

    /// <summary>
    ///     Resets the gate. Test-only: the gate is process-wide state, and a test that turns it on
    ///     would otherwise leak into every test that runs after it.
    /// </summary>
    internal static void ResetForTests()
    {
        lock (Gate)
        {
            _allowAnyServerCertificate = false;
        }
    }

    /// <summary>
    ///     A handler honouring the gate. Always a fresh instance — handlers carry connection pools and
    ///     sharing one across clients with different lifetimes is its own defect.
    /// </summary>
    public static HttpMessageHandler CreateHandler()
    {
        var handler = new SocketsHttpHandler();

        if (AllowsAnyServerCertificate)
        {
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        }

        return handler;
    }

    /// <summary>An <see cref="HttpClient" /> over <see cref="CreateHandler" />.</summary>
    public static HttpClient CreateHttpClient()
    {
        return new HttpClient(CreateHandler(), true);
    }

    /// <summary>
    ///     Applies the gate to a handler somebody else built — the shape SignalR and RestSharp hand
    ///     us. Leaves the handler untouched while the gate is closed, so the platform default applies.
    /// </summary>
    public static HttpMessageHandler Apply(HttpMessageHandler handler)
    {
        if (!AllowsAnyServerCertificate)
        {
            return handler;
        }

        switch (handler)
        {
            case SocketsHttpHandler sockets:
                sockets.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                break;
            case HttpClientHandler http:
                http.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                break;
        }

        return handler;
    }

    /// <summary>The validation callback for stacks that take one instead of a handler (RestSharp).</summary>
    public static RemoteCertificateValidationCallback? ValidationCallback =>
        AllowsAnyServerCertificate ? (_, _, _, _) => true : null;

    private static string? CurrentEnvironmentName()
    {
        var aspNet = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return !string.IsNullOrWhiteSpace(aspNet)
            ? aspNet
            : Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    }
}
