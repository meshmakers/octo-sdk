using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Sdk.ServiceClient.Tests.Fixtures;

public class ServiceCollectionFixture
{
    public ServiceCollectionFixture()
    {
        Services = new ServiceCollection();
        Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
    }

    public ServiceCollection Services { get; }

    public void UseXUnitLoggerFactory(ITestOutputHelper testOutputHelper)
    {
        Services.AddSingleton<ITestOutputHelper>(_ => testOutputHelper);
        Services.AddSingleton<ILoggerFactory>(sp =>
            new XUnitLoggerFactory(sp.GetRequiredService<ITestOutputHelper>()));
    }
}

internal class XUnitLoggerFactory(ITestOutputHelper output) : ILoggerFactory
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(output, categoryName);
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    private class XUnitLogger(ITestOutputHelper output, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            output.WriteLine($"[{categoryName}] {logLevel}: {formatter(state, exception)}");
        }
    }
}
