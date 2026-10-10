using System.Net;
using System.Reflection;
using Meshmakers.Octo.Sdk.ServiceClient;
using Microsoft.Extensions.Logging;

namespace Sdk.ServiceClient.Tests;

/// <summary>
///     AB#6418 — a failed connect/reconnect says WHY: exception type, message, HTTP status. Before,
///     the retry logged only "Common error during connect to SignalR hub X. Trying again.." and a
///     hub behind a 503 ingress was undiagnosable.
/// </summary>
public class SignalRClientRetryLoggingTests
{
    private readonly CapturingLogger _logger = new();
    private readonly SignalRClientOptions _options = new()
    {
        EndpointUri = "https://127.0.0.1:1", // nothing listens here: connection refused
        TenantId = "testTenant"
    };

    private SignalRClient<SignalRClientOptions> CreateClient() =>
        new(_options, _logger, A.Fake<IServiceClientAccessToken>(), "operatorHub");

    [Fact]
    public void DescribeCause_IncludesTypeMessageHttpStatusAndInnermostCause()
    {
        var ex = new HttpRequestException("Response status code does not indicate success: 503 (Service Unavailable).",
            new IOException("inner boom"), HttpStatusCode.ServiceUnavailable);

        var cause = SignalRClient<SignalRClientOptions>.DescribeCause(ex);

        Assert.Contains("HttpRequestException", cause);
        Assert.Contains("503", cause);
        Assert.Contains("ServiceUnavailable", cause);
        Assert.Contains("IOException: inner boom", cause);
    }

    [Fact]
    public void RetryableFailure_LogsCauseWithException_AndThrottlesTheSameCause()
    {
        var client = CreateClient();
        client.RetryFailureLogInterval = TimeSpan.FromHours(1);
        var ex503 = new HttpRequestException("503 from ingress", null, HttpStatusCode.ServiceUnavailable);

        Log(client, "Common error", "connect to", ex503);
        Log(client, "Common error", "connect to", ex503);
        Log(client, "Common error", "connect to", ex503);

        var warnings = _logger.Entries.Where(e => e.Level == LogLevel.Warning).ToArray();
        Assert.Single(warnings);
        Assert.Same(ex503, warnings[0].Exception);
        Assert.Contains("operatorHub", warnings[0].Message);
        Assert.Contains("503", warnings[0].Message);
        Assert.Contains("Trying again..", warnings[0].Message);
        Assert.Equal(2, _logger.Entries.Count(e => e.Level == LogLevel.Debug));
    }

    [Fact]
    public void RetryableFailure_ChangedCause_IsReportedImmediately_AndCountsSuppressed()
    {
        var client = CreateClient();
        client.RetryFailureLogInterval = TimeSpan.FromHours(1);

        Log(client, "Common error", "connect to", new HttpRequestException("503", null, HttpStatusCode.ServiceUnavailable));
        Log(client, "Common error", "connect to", new HttpRequestException("503", null, HttpStatusCode.ServiceUnavailable));
        Log(client, "Common error", "connect to", new HttpRequestException("404", null, HttpStatusCode.NotFound));

        var warnings = _logger.Entries.Where(e => e.Level == LogLevel.Warning).ToArray();
        Assert.Equal(2, warnings.Length);
        Assert.Contains("404", warnings[1].Message);
        Assert.Contains("1 identical failure(s)", warnings[1].Message);
    }

    [Fact]
    public void RetryableFailure_AfterInterval_ReportsTheSameCauseAgain()
    {
        var client = CreateClient();
        client.RetryFailureLogInterval = TimeSpan.Zero;
        var ex = new HttpRequestException("503", null, HttpStatusCode.ServiceUnavailable);

        Log(client, "Common error", "connect to", ex);
        Log(client, "Common error", "connect to", ex);

        Assert.Equal(2, _logger.Entries.Count(e => e.Level == LogLevel.Warning));
    }

    [Fact]
    public async Task StartAsync_HubUnreachable_LogsTheCauseOfTheFailedAttempt()
    {
        // End to end through the real start loop: connection refused must show up with its
        // exception type and message instead of the bare "Common error" line.
        var client = CreateClient();
        using var cts = new CancellationTokenSource();

        var start = client.StartAsync(_ => Task.CompletedTask, cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline && !_logger.Entries.Any(e => e.Level == LogLevel.Warning))
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        await cts.CancelAsync();
        await Record.ExceptionAsync(() => start);
        await client.StopAsync();

        var warning = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Trying again"));
        Assert.NotNull(warning.Exception);
        Assert.Contains("during connect to SignalR hub operatorHub:", warning.Message);
        Assert.Contains(warning.Exception!.GetType().Name, warning.Message);
    }

    private static void Log(SignalRClient<SignalRClientOptions> client, string kind, string phase, Exception ex)
    {
        var method = typeof(SignalRClient<SignalRClientOptions>)
            .GetMethod("LogRetryableFailure", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(client, [kind, phase, ex]);
    }

    private sealed class CapturingLogger : ILogger<SignalRClient<SignalRClientOptions>>
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_entries)
                {
                    return _entries.ToArray();
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_entries)
            {
                _entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
            }
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
