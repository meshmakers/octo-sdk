// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
///     Message object that is used to transfer data from adapter to core services.
/// </summary>
public record PipelineDataReceived : PipelineData
{
    /// <summary>
    ///     The data time a value was externally received (e. g. at PLC)
    /// </summary>
    public DateTime? ExternalReceivedDateTime { get; set; }
}