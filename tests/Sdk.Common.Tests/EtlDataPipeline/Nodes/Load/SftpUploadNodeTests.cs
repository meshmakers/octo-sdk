using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Load;

public class SftpUploadNodeTests
{
    private readonly IDataContext _dataContext;
    private readonly INodeContext _nodeContext;
    private readonly NodeDelegate _next;
    private readonly ISftpClientService _sftpClient;
    private readonly ISftpClientServiceFactory _sftpClientServiceFactory;
    private readonly SftpNodeConfiguration _config;
    private readonly SftpUploadNode _sut;

    public SftpUploadNodeTests()
    {
        _dataContext = A.Fake<IDataContext>();
        _nodeContext = A.Fake<INodeContext>();
        _next = A.Fake<NodeDelegate>();
        _sftpClient = A.Fake<ISftpClientService>();
        _sftpClientServiceFactory = A.Fake<ISftpClientServiceFactory>();

        _config = new SftpNodeConfiguration
        {
            TargetPath = "Remote/Target/Path.csv",
            User = "testUser",
            Password = "testPass",
            Host = "localhost",
            Port = 22,
            LocalDirectory = Path.Combine(Path.GetTempPath(), "sftp-test")
        };

        A.CallTo(() => _nodeContext.GetNodeConfiguration<SftpNodeConfiguration>())
            .Returns(_config);
        
        A.CallTo(() => _sftpClientServiceFactory.CreateClient(_config))
            .Returns(_sftpClient);

        _sut = new SftpUploadNode(_next, _sftpClientServiceFactory);
    }

    [Fact]
    public async Task ProcessObjectAsync_SuccessfulUpload_CallsSftpUploadAndNext()
    {
        // Arrange
        var localFilePath = Path.Combine(Path.GetTempPath(), "test-file.csv");
        File.WriteAllText(localFilePath, "test content");
        
        A.CallTo(() => _dataContext.GetSimpleValueByPath<string>(_config.TargetPath))
            .Returns(localFilePath);

        try
        {
            // Act
            await _sut.ProcessObjectAsync(_dataContext, _nodeContext);
            
            // Assert
            A.CallTo(() => _sftpClient.UploadSingleFile(localFilePath, _config.TargetPath, false))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _next(_dataContext, _nodeContext))
                .MustHaveHappenedOnceExactly();
        }
        finally
        {
            // Cleanup
            if (File.Exists(localFilePath))
                File.Delete(localFilePath);
        }
    }

    [Fact]
    public async Task ProcessObjectAsync_NullDataContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.ProcessObjectAsync(null, _nodeContext));
        
        A.CallTo(() => _sftpClient.UploadSingleFile(A<string>._, A<string>._, A<bool>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_NullNodeContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.ProcessObjectAsync(_dataContext, null));
        
        A.CallTo(() => _sftpClient.UploadSingleFile(A<string>._, A<string>._, A<bool>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyLocalFilePath_ThrowsPipelineExecutionException()
    {
        // Arrange
        A.CallTo(() => _dataContext.GetSimpleValueByPath<string>(_config.TargetPath))
            .Returns(string.Empty);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PipelineExecutionException>(() => 
            _sut.ProcessObjectAsync(_dataContext, _nodeContext));
        
        Assert.Contains("not found", exception.Message);
        
        A.CallTo(() => _nodeContext.Error("SftpUploadNode: Local file path is null or empty. Aborting upload."))
            .MustHaveHappenedOnceExactly();
        
        A.CallTo(() => _sftpClient.UploadSingleFile(A<string>._, A<string>._, A<bool>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_FileNotExists_ThrowsPipelineExecutionException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent.csv");
        
        A.CallTo(() => _dataContext.GetSimpleValueByPath<string>(_config.TargetPath))
            .Returns(nonExistentFile);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PipelineExecutionException>(() => 
            _sut.ProcessObjectAsync(_dataContext, _nodeContext));
        
        Assert.Contains($"Local file '{nonExistentFile}' not found", exception.Message);
        
        A.CallTo(() => _nodeContext.Error(A<string>.That.Contains("File '{FilePath}' not found"), nonExistentFile))
            .MustHaveHappenedOnceExactly();
        
        A.CallTo(() => _sftpClient.UploadSingleFile(A<string>._, A<string>._, A<bool>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_SftpUploadThrowsException_PropagatesException()
    {
        // Arrange
        var localFilePath = Path.Combine(Path.GetTempPath(), "test-file.csv");
        File.WriteAllText(localFilePath, "test content");
        
        A.CallTo(() => _dataContext.GetSimpleValueByPath<string>(_config.TargetPath))
            .Returns(localFilePath);
        
        var expectedException = new InvalidOperationException("SFTP connection failed");
        A.CallTo(() => _sftpClient.UploadSingleFile(localFilePath, _config.TargetPath, false))
            .Throws(expectedException);

        try
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.ProcessObjectAsync(_dataContext, _nodeContext));
            
            Assert.Same(expectedException, exception);
            
            A.CallTo(() => _nodeContext.Error(expectedException, 
                "SftpUploadNode: Error occurred while uploading file to SFTP."))
                .MustHaveHappenedOnceExactly();
            
            A.CallTo(() => _next(A<IDataContext>._, A<INodeContext>._))
                .MustNotHaveHappened();
        }
        finally
        {
            // Cleanup
            if (File.Exists(localFilePath))
                File.Delete(localFilePath);
        }
    }

    [Fact]
    public async Task ProcessObjectAsync_LogsInformationMessages()
    {
        // Arrange
        var localFilePath = Path.Combine(Path.GetTempPath(), "test-file.csv");
        File.WriteAllText(localFilePath, "test content");
        
        A.CallTo(() => _dataContext.GetSimpleValueByPath<string>(_config.TargetPath))
            .Returns(localFilePath);

        try
        {
            // Act
            await _sut.ProcessObjectAsync(_dataContext, _nodeContext);

            // Assert
            A.CallTo(() => _nodeContext.Info(A<string>._, A<object>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _nodeContext.Info(A<string>._, A<object>._))
                .MustHaveHappened();
        }
        finally
        {
            // Cleanup
            if (File.Exists(localFilePath))
                File.Delete(localFilePath);
        }
    }

    [Fact]
    public void Constructor_NullNext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SftpUploadNode(null!, _sftpClientServiceFactory));
    }

    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SftpUploadNode(_next, null!));
    }

    [Fact]
    public async Task ProcessObjectAsync_VerifiesDeleteLocalFileParameter_IsFalse()
    {
        // Arrange
        var localFilePath = Path.Combine(Path.GetTempPath(), "test-file.csv");
        File.WriteAllText(localFilePath, "test content");
        
        A.CallTo(() => _dataContext.GetSimpleValueByPath<string>(_config.TargetPath))
            .Returns(localFilePath);

        try
        {
            // Act
            await _sut.ProcessObjectAsync(_dataContext, _nodeContext);
            
            // Assert
            A.CallTo(() => _sftpClient.UploadSingleFile(localFilePath, _config.TargetPath, false))
                .MustHaveHappenedOnceExactly();
        }
        finally
        {
            // Cleanup
            if (File.Exists(localFilePath))
                File.Delete(localFilePath);
        }
    }
}