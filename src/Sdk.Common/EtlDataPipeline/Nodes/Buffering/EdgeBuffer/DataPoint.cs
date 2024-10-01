using LiteDB;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;

/// <summary>
///     One datapoint in the buffer
/// </summary>
internal class DataPoint
{
    [BsonId(true)] public int Id { get; set; }
    
    public required Dictionary<string, BsonValue> Data { get; set; }

    public DateTimeOffset BufferedAt { get; set; }


    internal static DataPoint CreateNew(Dictionary<string, BsonValue> data)
    {
        return new DataPoint
        {
            Data = data,
            BufferedAt = DateTimeOffset.UtcNow
        };
    }
}