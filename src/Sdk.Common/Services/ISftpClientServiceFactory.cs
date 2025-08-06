namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Factory interface for creating ISftpClientService instances
/// </summary>
public interface ISftpClientServiceFactory
{
    /// <summary>
    /// Creates a new instance of ISftpClientService with the specified configuration
    /// </summary>
    /// <param name="configuration">The SFTP configuration to use</param>
    /// <returns>A new ISftpClientService instance</returns>
    ISftpClientService CreateClient(SftpNodeConfiguration configuration);
}