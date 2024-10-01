using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;

/// <summary>
///     Class responsible for managing the chunks (LiteDatabase files)
/// </summary>
internal interface IEdgeDataBuffer
{
    /// <summary>
    ///     Returns the latest chunk or crates a new one.
    /// </summary>
    /// <returns></returns>
    IChunkedDataBuffer GetOrCreateOpenChunk();

    /// <summary>
    ///     Closes a chunk and makes it available for reading.
    /// </summary>
    /// <param name="dispose"></param>
    void TryCloseCurrentChunk(bool dispose = false);

    /// <summary>
    ///     Returns all closed chunks
    /// </summary>
    /// <returns></returns>
    IEnumerable<IDisposableChunkedDataBuffer> GetClosedChunks();

    /// <summary>
    ///     Delete a chunk
    /// </summary>
    /// <param name="chunk"></param>
    void DeleteChunk(IChunkedDataBuffer chunk);

    void MarkAsSent(IChunkedDataBuffer chunk);
}

/// <summary>
///     Class responsible for managing the chunks (LiteDatabase files)
/// </summary>
internal class EdgeDataBuffer : IEdgeDataBuffer, IDisposable
{
    private readonly EdgeDataBufferConfiguration _config;
    private readonly ILiteDBFactory _dbFactory;
    private readonly ILogger<EdgeDataBuffer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILiteCollection<ChunkMetadata> _metadataCollection;
    private readonly LiteDatabase _metadataDatabase;
    private Tuple<ChunkedDataBuffer, ChunkMetadata>? _currentChunk;
    private bool _isDisposed;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public EdgeDataBuffer(ILoggerFactory loggerFactory, ILiteDBFactory dbFactory,
        IOptions<EdgeDataBufferConfiguration> configuration)
    {
        _logger = loggerFactory.CreateLogger<EdgeDataBuffer>();
        _loggerFactory = loggerFactory;
        _dbFactory = dbFactory;
        _config = configuration.Value;

        var metaDataPath = Path.Combine(_config.StoragePath, Constants.MetaDataDatabaseName);
        _logger.LogDebug("Creating or opening metadata database: {MetaDataPath}", metaDataPath);
        try
        {
            _metadataDatabase = dbFactory.Create(metaDataPath);
            _metadataDatabase.UtcDate = true;
            _metadataCollection = _metadataDatabase.GetCollection<ChunkMetadata>();
        }
        catch (EdgeDataBufferException e)
        {
            _logger.LogDebug(e, "Error creating metadata database");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (_currentChunk != null)
            {
                _metadataCollection.Update(_currentChunk.Item2);
                _currentChunk?.Item1.Dispose();
            }

            _metadataDatabase.Dispose();
            _isDisposed = true;
        }
    }

    public IChunkedDataBuffer GetOrCreateOpenChunk()
    {
        EnsureState();

        try
        {
            _semaphore.Wait();
            
            if (_currentChunk != null)
            {
                return _currentChunk.Item1;
            }

            var chunkMetadata = _metadataCollection.FindOne(x => x.State == ChunkedDataBufferState.Open);
            if (chunkMetadata == null)
            {
                CreateNewChunk();
            }
            else
            {
                LoadCurrentChunk(chunkMetadata);
            }

            return _currentChunk!.Item1;

        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void TryCloseCurrentChunk(bool dispose = false)
    {
        EnsureState();

        if (_currentChunk == null)
        {
            return;
        }


        var chunkMetadata = _currentChunk.Item2;
        chunkMetadata.ClosedAt = DateTimeOffset.UtcNow;
        chunkMetadata.State = ChunkedDataBufferState.Closed;

        _logger.LogDebug("Closing current chunk: '{ChunkId}'", chunkMetadata.Id);


        _metadataCollection.Update(chunkMetadata);

        _currentChunk.Item1.Close();
        if (dispose)
        {
            _currentChunk.Item1.Dispose();
        }

        _currentChunk = null;
    }

    public IEnumerable<IDisposableChunkedDataBuffer> GetClosedChunks()
    {
        EnsureState();

        var chunks = _metadataCollection.Find(x => x.State == ChunkedDataBufferState.Closed && x.DeletedAt == null);

        var logger = _loggerFactory.CreateLogger<ChunkedDataBuffer>();
        foreach (var chunk in chunks)
        {
            yield return new ChunkedDataBuffer(logger, chunk, _dbFactory, BufferReceivedData);
        }
    }

    public void DeleteChunk(IChunkedDataBuffer chunk)
    {
        EnsureState();

        var c = (ChunkedDataBuffer)chunk;

        _logger.LogDebug("Deleting chunk: '{ChunkId}'", c.Metadata.Id);

        c.Metadata.DeletedAt = DateTimeOffset.UtcNow;
        _metadataCollection.Update(c.Metadata);

        c.Dispose();

        _dbFactory.Delete(c.Metadata.FileName);
    }

    public void MarkAsSent(IChunkedDataBuffer chunk)
    {
        EnsureState();

        var c = (ChunkedDataBuffer)chunk;
        _logger.LogDebug("Marking chunk as sent: '{ChunkId}'", c.Metadata.Id);
        c.Metadata.SentAt = DateTimeOffset.UtcNow;
        _metadataCollection.Update(c.Metadata);
    }

    private void LoadCurrentChunk(ChunkMetadata chunkMetadata)
    {
        var logger = _loggerFactory.CreateLogger<ChunkedDataBuffer>();
        var chunkedBuffer = new ChunkedDataBuffer(logger, chunkMetadata, _dbFactory, BufferReceivedData);
        _currentChunk = new Tuple<ChunkedDataBuffer, ChunkMetadata>(chunkedBuffer, chunkMetadata);
    }

    private void CreateNewChunk()
    {
        var id = Guid.NewGuid();
        var fileName = Path.ChangeExtension(id.ToString(), Constants.DatabaseExtension);
        var chunkMetadata = new ChunkMetadata
        {
            Id = id,
            FileName = Path.Combine(_config.StoragePath, fileName),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _logger.LogDebug("Creating new Chunk: '{ChunkId}'", chunkMetadata.Id);

        var logger = _loggerFactory.CreateLogger<ChunkedDataBuffer>();

        var chunkedBuffer = new ChunkedDataBuffer(logger, chunkMetadata, _dbFactory, BufferReceivedData);
        _currentChunk = new Tuple<ChunkedDataBuffer, ChunkMetadata>(chunkedBuffer, chunkMetadata);
        _metadataCollection.Insert(chunkMetadata);
    }

    private void EnsureState()
    {
        if (_metadataCollection == null)
        {
            throw EdgeDataBufferException.MetadataUninitialized();
        }

        if (_isDisposed)
        {
            throw EdgeDataBufferException.Disposed();
        }
    }

    private void BufferReceivedData()
    {
        _metadataDatabase.WithTransaction(_logger, () => _metadataCollection.Update(_currentChunk!.Item2));
    }
}