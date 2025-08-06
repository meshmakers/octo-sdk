using Meshmakers.Common.Shared;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;

/// <summary>
/// Loads (uploads) files to an SFTP server
/// </summary>
[NodeName("SftpUpload", 1)]
[NodeConfiguration(typeof(SftpNodeConfiguration))]
public class SftpUploadNode : IPipelineNode
{
    private readonly NodeDelegate _next;
    private readonly ISftpClientServiceFactory _sftpClientServiceFactory;

    /// <summary>
    /// Creates a new instance of SftpUploadNode
    /// </summary>
    /// <param name="next">The next node in the pipeline</param>
    /// <param name="sftpClientServiceFactory">The factory for creating SFTP client services</param>
    /// <exception cref="ArgumentNullException">Thrown when next or sftpClientServiceFactory is null</exception>
    public SftpUploadNode(NodeDelegate next, ISftpClientServiceFactory sftpClientServiceFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _sftpClientServiceFactory = sftpClientServiceFactory ?? throw new ArgumentNullException(nameof(sftpClientServiceFactory));
    }
    
    /// <summary>
    /// Processes the upload operation
    /// </summary>
    /// <param name="dataContext">The data context</param>
    /// <param name="nodeContext">The node context</param>
    /// <exception cref="ArgumentNullException">Thrown when dataContext or nodeContext is null</exception>
    /// <exception cref="PipelineExecutionException">Thrown when configuration is invalid or file not found</exception>
    public async Task ProcessObjectAsync(IDataContext? dataContext, INodeContext? nodeContext)
    {
        ArgumentValidation.Validate(nameof(dataContext), dataContext);
        ArgumentValidation.Validate(nameof(nodeContext), nodeContext);

        nodeContext!.Info("SftpUploadNode: Starting file upload to SFTP...");

        try
        {
            var c = nodeContext.GetNodeConfiguration<SftpNodeConfiguration>();
            
            var remoteFilePath = c.TargetPath;
            var localFilePath = dataContext!.GetSimpleValueByPath<string>(c.TargetPath);
            
            if (string.IsNullOrWhiteSpace(localFilePath))
            {
                nodeContext.Error("SftpUploadNode: Local file path is null or empty. Aborting upload.");
                throw PipelineExecutionException.PathNotFound(nodeContext.NodePath, c.TargetPath);
            }
            
            if (!File.Exists(localFilePath))
            {
                nodeContext.Error("SftpUploadNode: File '{FilePath}' not found. Aborting upload.", localFilePath!);
                throw new PipelineExecutionException($"Local file '{localFilePath}' not found.");
            }
            
            using var sftp = _sftpClientServiceFactory.CreateClient(c);
            sftp.UploadSingleFile(localFilePath!, remoteFilePath, false);

            nodeContext.Info("SftpUploadNode: Successfully uploaded file to SFTP: {RemotePath}", remoteFilePath);
        }
        catch (Exception ex)
        {
            nodeContext.Error(ex, "SftpUploadNode: Error occurred while uploading file to SFTP.");
            throw;
        }

        await _next(dataContext, nodeContext);
    }
}