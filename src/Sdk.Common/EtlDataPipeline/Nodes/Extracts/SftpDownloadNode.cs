using Meshmakers.Common.Shared;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;

/// <summary>
/// Extracts (downloads) files from an SFTP server
/// </summary>
[NodeName("SftpDownload", 1)]
[NodeConfiguration(typeof(SftpNodeConfiguration))]
public class SftpDownloadNode : IPipelineNode
{
    private readonly NodeDelegate _next;
    private readonly ISftpClientServiceFactory _sftpClientServiceFactory;

    /// <summary>
    /// Creates a new instance of SftpDownloadNode
    /// </summary>
    /// <param name="next">The next node in the pipeline</param>
    /// <param name="sftpClientServiceFactory">The factory for creating SFTP client services</param>
    /// <exception cref="ArgumentNullException">Thrown when next or sftpClientServiceFactory is null</exception>
    public SftpDownloadNode(NodeDelegate next, ISftpClientServiceFactory sftpClientServiceFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _sftpClientServiceFactory = sftpClientServiceFactory ?? throw new ArgumentNullException(nameof(sftpClientServiceFactory));
    }
    
    /// <summary>
    /// Processes the download operation
    /// </summary>
    /// <param name="dataContext">The data context</param>
    /// <param name="nodeContext">The node context</param>
    /// <exception cref="ArgumentNullException">Thrown when dataContext or nodeContext is null</exception>
    /// <exception cref="PipelineExecutionException">Thrown when configuration is invalid</exception>
    public async Task ProcessObjectAsync(IDataContext? dataContext, INodeContext? nodeContext)
    {
        ArgumentValidation.Validate(nameof(dataContext), dataContext);
        ArgumentValidation.Validate(nameof(nodeContext), nodeContext);

        nodeContext!.Info("SftpDownloadNode: Starting download from SFTP...");

        try
        {
            var c = nodeContext.GetNodeConfiguration<SftpNodeConfiguration>();
            
            var remoteFilePath = c.TargetPath;
            var localDirectory = c.LocalDirectory;
            
            if (string.IsNullOrWhiteSpace(remoteFilePath))
            {
                throw PipelineExecutionException.PathNotFound(nodeContext.NodePath, c.TargetPath);
            }

            if (string.IsNullOrWhiteSpace(localDirectory))
            {
                throw new PipelineExecutionException("Local directory is null or empty.");
            }
            
            Directory.CreateDirectory(localDirectory);

            using var sftp = _sftpClientServiceFactory.CreateClient(c);
            sftp.DownloadAndDeleteSingleFile(remoteFilePath, localDirectory);

            nodeContext.Info("SftpDownloadNode: Successfully downloaded '{RemoteFilePath}' to '{LocalDirectory}'.", 
                remoteFilePath, localDirectory);
        }
        catch (Exception ex)
        {
            nodeContext.Error(ex, "SftpDownloadNode: Error occurred while downloading from SFTP.");
            throw;
        }

        await _next(dataContext!, nodeContext);
    }
}