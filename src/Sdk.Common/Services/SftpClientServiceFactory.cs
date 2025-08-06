using Meshmakers.Common.Shared;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Factory for creating ISftpClientService instances
/// </summary>
public class SftpClientServiceFactory : ISftpClientServiceFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of SftpClientServiceFactory
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating loggers</param>
    public SftpClientServiceFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public ISftpClientService CreateClient(SftpNodeConfiguration configuration)
    {
        ArgumentValidation.Validate(nameof(configuration), configuration);

        var logger = _loggerFactory.CreateLogger<SftpClientService>();
        return new SftpClientService(configuration, logger);
    }
}