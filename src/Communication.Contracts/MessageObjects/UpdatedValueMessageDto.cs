using Meshmakers.Octo.ConstructionKit.Contracts;


// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
///     Data transfer object for the updated value message.
/// </summary>
public record UpdatedValueMessageDto
{
    /// <summary>
    ///     The tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = null!;

    /// <summary>
    ///     The adapter object identifier.
    /// </summary>
    public OctoObjectId AdapterRtId { get; set; }

    /// <summary>
    ///     The mapping object identifier.
    /// </summary>
    public OctoObjectId DataPipelineRtId { get; set; }

    /// <summary>
    ///     The value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    ///     The date time a value is received at the plug
    /// </summary>
    public DateTime AdapterReceivedDateTime { get; set; }

    /// <summary>
    ///     The data time a value was externally received (e. g. at PLC)
    /// </summary>
    public DateTime? ExternalReceivedDateTime { get; set; }
}