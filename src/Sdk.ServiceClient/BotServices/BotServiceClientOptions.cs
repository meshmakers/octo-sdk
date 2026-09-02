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
}
