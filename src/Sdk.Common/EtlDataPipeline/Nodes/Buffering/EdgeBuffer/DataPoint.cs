using LiteDB;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;

/// <summary>
///     One datapoint in the buffer
/// </summary>
internal class DataPoint
{
    [BsonId(true)] public int Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }
    public required Dictionary<string, object> Data { get; set; }

    public DateTimeOffset BufferedAt { get; set; }

    public bool IsSent { get; set; }
    public DateTimeOffset SentAt { get; set; }

    public bool IsReceived { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }

    public bool IsProcessed { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }


    internal static DataPoint CreateNew(Dictionary<string, object> data, DateTimeOffset timestamp)
    {
        return new DataPoint
        {
            Timestamp = timestamp,
            Data = data,
            BufferedAt = DateTimeOffset.UtcNow
        };
    }
}