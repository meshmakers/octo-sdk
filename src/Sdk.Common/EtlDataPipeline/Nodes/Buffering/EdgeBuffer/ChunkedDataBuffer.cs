using LiteDB;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;

internal interface IDisposableChunkedDataBuffer : IChunkedDataBuffer, IDisposable;

/// <summary>
/// This represents the buffer for one chunk
/// </summary>
internal interface IChunkedDataBuffer
{
    ChunkedDataBufferState State { get; }
    int AddDataPoint(DataPoint dataPoint);
    int AddDataPoints(List<DataPoint> dataPoints);
    IEnumerable<DataPoint> GetDataPoints();
}

/// <summary>
/// The buffer for one chunk (one lite database file)
/// </summary>
internal class ChunkedDataBuffer : IDisposableChunkedDataBuffer
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<DataPoint> _data;
    private bool _isDisposed;
    private readonly ILogger<ChunkedDataBuffer> _logger;
    private readonly ChunkMetadata _metadata;
    private readonly Action _onDataReceivedCallback;

    public ChunkedDataBufferState State => _metadata.State;

    public ChunkMetadata Metadata => _metadata;

    public ChunkedDataBuffer(ILogger<ChunkedDataBuffer> logger, ChunkMetadata metadata,
        ILiteDBFactory dbFactory, Action onDataReceivedCallback)
    {
        _logger = logger;
        _metadata = metadata;
        _onDataReceivedCallback = onDataReceivedCallback;
        _database = dbFactory.Create(metadata.FileName);
        _data = _database.GetCollection<DataPoint>(Constants.DataCollectionName);
    }


    /// <summary>
    /// Adds a new data point to the buffer;
    /// Returns the number of inserted data points (Id)
    /// </summary>
    /// <param name="dataPoint"></param>
    /// <returns></returns>
    public int AddDataPoint(DataPoint dataPoint)
    {
        EnsureState(ChunkedDataBufferState.Open);

        dataPoint.BufferedAt = DateTimeOffset.UtcNow;


        var operationResult = _database.WithTransaction(_logger, () => _data.Insert(dataPoint));

        if (!operationResult.Success)
        {
            _logger.LogError("Error inserting data point");
            return 0;
        }

        _metadata.DataCount++;

        Task.Run(() => _onDataReceivedCallback());

        return _metadata.DataCount;
    }

    public int AddDataPoints(List<DataPoint> dataPoints)
    {
        EnsureState(ChunkedDataBufferState.Open);

        var now = DateTimeOffset.UtcNow;

        dataPoints.ForEach(x => x.BufferedAt = now);

        var operationResult = _database.WithTransaction(_logger, () => _data.InsertBulk(dataPoints));


        if (!operationResult.Success)
        {
            _logger.LogError("Error inserting data points");
            return 0;
        }

        _metadata.DataCount += operationResult.Result;

        Task.Run(() => _onDataReceivedCallback());

        return _metadata.DataCount;
    }

    public IEnumerable<DataPoint> GetDataPoints()
    {
        EnsureState(ChunkedDataBufferState.Closed);

        var skip = 0;
        const int take = Constants.RetrievalChunkSize;

        while (true)
        {
            bool hadData = false;
            var dataPoints = _data.Find(Query.All("Id"), skip, take);
            foreach (var dataPoint in dataPoints)
            {
                hadData = true;
                yield return dataPoint;
            }

            if (!hadData)
            {
                break;
            }

            skip += take;
        }
    }


    public void Open()
    {
        if (_isDisposed)
        {
            throw EdgeDataBufferException.Disposed();
        }

        _metadata.State = ChunkedDataBufferState.Open;
    }

    public void Close()
    {
        if (_isDisposed)
        {
            throw EdgeDataBufferException.Disposed();
        }

        _metadata.State = ChunkedDataBufferState.Closed;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _database.Dispose();
        _isDisposed = true;
    }

    private void EnsureState(ChunkedDataBufferState requiredState)
    {
        if (_metadata.State != requiredState)
        {
            throw EdgeDataBufferException.ChunkedBufferHasInvalidStateForOperation(_metadata.State, requiredState);
        }

        if (_isDisposed)
        {
            throw EdgeDataBufferException.Disposed();
        }
    }
}