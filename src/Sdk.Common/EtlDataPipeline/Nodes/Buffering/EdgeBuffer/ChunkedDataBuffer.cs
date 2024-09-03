using LiteDB;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;

internal interface IDisposableChunkedDataBuffer : IChunkedDataBuffer, IDisposable;

/// <summary>
///     This represents the buffer for one chunk
/// </summary>
internal interface IChunkedDataBuffer
{
    ChunkedDataBufferState State { get; }
    int AddDataPoint(DataPoint dataPoint);
    int AddDataPoints(List<DataPoint> dataPoints);
    IEnumerable<DataPoint> GetDataPoints();
}

/// <summary>
///     The buffer for one chunk (one lite database file)
/// </summary>
internal class ChunkedDataBuffer : IDisposableChunkedDataBuffer
{
    private readonly ILiteCollection<DataPoint> _data;
    private readonly LiteDatabase _database;
    private readonly ILogger<ChunkedDataBuffer> _logger;
    private readonly Action _onDataReceivedCallback;
    private bool _isDisposed;

    public ChunkedDataBuffer(ILogger<ChunkedDataBuffer> logger, ChunkMetadata metadata,
        ILiteDBFactory dbFactory, Action onDataReceivedCallback)
    {
        _logger = logger;
        Metadata = metadata;
        _onDataReceivedCallback = onDataReceivedCallback;
        _database = dbFactory.Create(metadata.FileName);
        _data = _database.GetCollection<DataPoint>(Constants.DataCollectionName);
    }

    public ChunkMetadata Metadata { get; }

    public ChunkedDataBufferState State => Metadata.State;


    /// <summary>
    ///     Adds a new data point to the buffer;
    ///     Returns the number of inserted data points (Id)
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

        Metadata.DataCount++;

        Task.Run(() => _onDataReceivedCallback());

        return Metadata.DataCount;
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

        Metadata.DataCount += operationResult.Result;

        Task.Run(() => _onDataReceivedCallback());

        return Metadata.DataCount;
    }

    public IEnumerable<DataPoint> GetDataPoints()
    {
        EnsureState(ChunkedDataBufferState.Closed);

        return _data.Find(Query.All("Id"));
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


    public void Open()
    {
        if (_isDisposed)
        {
            throw EdgeDataBufferException.Disposed();
        }

        Metadata.State = ChunkedDataBufferState.Open;
    }

    public void Close()
    {
        if (_isDisposed)
        {
            throw EdgeDataBufferException.Disposed();
        }

        Metadata.State = ChunkedDataBufferState.Closed;
    }

    private void EnsureState(ChunkedDataBufferState requiredState)
    {
        if (Metadata.State != requiredState)
        {
            throw EdgeDataBufferException.ChunkedBufferHasInvalidStateForOperation(Metadata.State, requiredState);
        }

        if (_isDisposed)
        {
            throw EdgeDataBufferException.Disposed();
        }
    }
}