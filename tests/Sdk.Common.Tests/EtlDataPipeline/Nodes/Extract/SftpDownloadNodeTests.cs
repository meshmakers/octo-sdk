using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Extract;

public class SftpDownloadNodeTests
{
    private readonly IDataContext _dataContext;
    private readonly INodeContext _nodeContext;
    private readonly NodeDelegate _next;
    private readonly ISftpClientService _sftpClient;
    private readonly ISftpClientServiceFactory _sftpClientServiceFactory;
    private readonly SftpNodeConfiguration _config;
    private readonly SftpDownloadNode _sut;

    public SftpDownloadNodeTests()
    {
        _dataContext = A.Fake<IDataContext>();
        _nodeContext = A.Fake<INodeContext>();
        _next = A.Fake<NodeDelegate>();
        _sftpClient = A.Fake<ISftpClientService>();
        _sftpClientServiceFactory = A.Fake<ISftpClientServiceFactory>();

        _config = new SftpNodeConfiguration
        {
            TargetPath = "/remote/path/file.csv",
            User = "testUser",
            Password = "testPass",
            Host = "localhost",
            Port = 22,
            LocalDirectory = Path.Combine(Path.GetTempPath(), "sftp-test", Guid.NewGuid().ToString())
        };

        A.CallTo(() => _nodeContext.GetNodeConfiguration<SftpNodeConfiguration>())
            .Returns(_config);
        
        A.CallTo(() => _sftpClientServiceFactory.CreateClient(_config))
            .Returns(_sftpClient);

        _sut = new SftpDownloadNode(_next, _sftpClientServiceFactory);
    }

    [Fact]
    public async Task ProcessObjectAsync_SuccessfulDownload_CallsSftpDownloadAndNext()
    {
        // Act
        await _sut.ProcessObjectAsync(_dataContext, _nodeContext);

        // Assert
        A.CallTo(() => _sftpClient.DownloadAndDeleteSingleFile(_config.TargetPath, _config.LocalDirectory))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _next(_dataContext, _nodeContext))
            .MustHaveHappenedOnceExactly();
        
        // Verify directory was created
        Assert.True(Directory.Exists(_config.LocalDirectory));
        
        // Cleanup
        if (Directory.Exists(_config.LocalDirectory))
            Directory.Delete(_config.LocalDirectory, true);
    }

    [Fact]
    public async Task ProcessObjectAsync_CreatesLocalDirectoryIfNotExists()
    {
        // Arrange
        Assert.False(Directory.Exists(_config.LocalDirectory));

        // Act
        await _sut.ProcessObjectAsync(_dataContext, _nodeContext);

        // Assert
        Assert.True(Directory.Exists(_config.LocalDirectory));
        
        // Cleanup
        Directory.Delete(_config.LocalDirectory, true);
    }

    [Fact]
    public async Task ProcessObjectAsync_NullDataContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.ProcessObjectAsync(null, _nodeContext));
        
        A.CallTo(() => _sftpClient.DownloadAndDeleteSingleFile(A<string>._, A<string>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_NullNodeContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.ProcessObjectAsync(_dataContext, null));
        
        A.CallTo(() => _sftpClient.DownloadAndDeleteSingleFile(A<string>._, A<string>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyRemoteFilePath_ThrowsPipelineExecutionException()
    {
        // Arrange
        _config.TargetPath = string.Empty;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PipelineExecutionException>(() => 
            _sut.ProcessObjectAsync(_dataContext, _nodeContext));
        
        Assert.Contains("not found", exception.Message);
        
        A.CallTo(() => _sftpClient.DownloadAndDeleteSingleFile(A<string>._, A<string>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyLocalDirectory_ThrowsPipelineExecutionException()
    {
        // Arrange
        _config.LocalDirectory = string.Empty;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PipelineExecutionException>(() => 
            _sut.ProcessObjectAsync(_dataContext, _nodeContext));
        
        Assert.Contains("Local directory is null or empty", exception.Message);
        
        A.CallTo(() => _sftpClient.DownloadAndDeleteSingleFile(A<string>._, A<string>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_SftpDownloadThrowsException_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("SFTP connection failed");
        A.CallTo(() => _sftpClient.DownloadAndDeleteSingleFile(_config.TargetPath, _config.LocalDirectory))
            .Throws(expectedException);

        try
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.ProcessObjectAsync(_dataContext, _nodeContext));
            
            Assert.Same(expectedException, exception);
            
            A.CallTo(() => _nodeContext.Error(expectedException, 
                "SftpDownloadNode: Error occurred while downloading from SFTP."))
                .MustHaveHappenedOnceExactly();
            
            A.CallTo(() => _next(A<IDataContext>._, A<INodeContext>._))
                .MustNotHaveHappened();
        }
        finally
        {
            // Cleanup - directory might have been created before exception
            if (Directory.Exists(_config.LocalDirectory))
                Directory.Delete(_config.LocalDirectory, true);
        }
    }

    [Fact]
    public async Task ProcessObjectAsync_LogsInformationMessages()
    {
        // Act
        await _sut.ProcessObjectAsync(_dataContext, _nodeContext);

        // Assert
        A.CallTo(() => _nodeContext.Info("SftpDownloadNode: Starting download from SFTP..."))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _nodeContext.Info(
            "SftpDownloadNode: Successfully downloaded '{RemoteFilePath}' to '{LocalDirectory}'.", 
            _config.TargetPath, _config.LocalDirectory))
            .MustHaveHappenedOnceExactly();
        
        // Cleanup
        if (Directory.Exists(_config.LocalDirectory))
            Directory.Delete(_config.LocalDirectory, true);
    }

    [Fact]
    public void Constructor_NullNext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SftpDownloadNode(null!, _sftpClientServiceFactory));
    }

    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SftpDownloadNode(_next, null!));
    }
}