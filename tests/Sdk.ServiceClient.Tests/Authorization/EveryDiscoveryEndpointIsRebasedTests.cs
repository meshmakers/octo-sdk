using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Sdk.ServiceClient.Tests.Authorization;

/// <summary>
///     AB#5081 — every read of a discovery-document endpoint must go through <c>Rebase(...)</c>.
/// </summary>
/// <remarks>
///     🔴 <b>This exists because the unit tests around <c>Rebase</c> could not catch the bug they were
///     written for.</b> <see cref="EndpointRebaseTests" /> pins the helper in isolation, which says
///     nothing about whether the call sites use it — and they did not: the first AB#5081 commit
///     wrapped the two endpoint reads in <c>AuthorizationClient</c> and left all five in
///     <c>AuthenticatorClient</c> unwrapped, including <c>TokenEndpoint</c>, the one every token
///     acquisition goes through. A green suite shipped a split-horizon fix that still could not
///     acquire a token.
///     <para>
///         The failure mode is invisible outside split horizon: without an
///         <c>AdditionalValidIssuers</c> allow-list <c>Rebase</c> returns its argument unchanged, so
///         a forgotten wrapper behaves identically in every normal deployment and every existing
///         test. Only a container talking to a host-run identity service notices — as
///         "connection refused", far from the missing call.
///     </para>
///     <para>
///         Scanning source rather than IL is deliberate: the property being enforced is a source
///         convention ("write <c>Rebase(disco.X)</c>, not <c>disco.X</c>"), the compiler inlines
///         nothing that would make it observable at runtime, and a reflection-based check would have
///         to reconstruct which arguments reached which HTTP request. The cost is that the file paths
///         below are load-bearing: move or rename either client and this test fails loudly rather
///         than silently passing, which is the intended direction.
///     </para>
/// </remarks>
public class EveryDiscoveryEndpointIsRebasedTests
{
    /// <summary>Reads of the discovery document, e.g. <c>disco.TokenEndpoint</c>.</summary>
    private static readonly Regex EndpointRead = new(@"\bdisco\.\w*Endpoint\b", RegexOptions.Compiled);

    /// <summary>The same read, correctly wrapped.</summary>
    private static readonly Regex WrappedEndpointRead = new(@"\bRebase\(\s*disco\.\w*Endpoint\b", RegexOptions.Compiled);

    public static TheoryData<string> Clients =>
    [
        "Authorization/AuthorizationClient.cs",
        "Authentication/AuthenticatorClient.cs"
    ];

    [Theory]
    [MemberData(nameof(Clients))]
    public void EveryDiscoveryEndpointRead_IsWrappedInRebase(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(SourceRoot(), relativePath));

        var reads = EndpointRead.Matches(source).Count;
        var wrapped = WrappedEndpointRead.Matches(source).Count;

        Assert.True(reads > 0, $"No discovery endpoint reads found in {relativePath} — has the file moved?");
        Assert.True(
            reads == wrapped,
            $"{relativePath} reads {reads} discovery endpoint(s) but only wraps {wrapped} in Rebase(...). " +
            "An unwrapped read works everywhere except a split-horizon deployment, where it fails at " +
            "connect time with 'connection refused' (AB#5081).");
    }

    /// <summary>
    ///     <c>src/Sdk.ServiceClient</c>, located from this file rather than from the working
    ///     directory — the test runner's cwd is the output folder and differs between IDE and CI.
    /// </summary>
    private static string SourceRoot([CallerFilePath] string thisFile = "")
    {
        // <repo>/tests/Sdk.ServiceClient.Tests/Authorization/<this file>
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", ".."));
        return Path.Combine(repoRoot, "src", "Sdk.ServiceClient");
    }
}
