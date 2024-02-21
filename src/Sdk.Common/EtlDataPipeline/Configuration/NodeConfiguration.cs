using YamlDotNet.Serialization;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

/// <summary>
/// Base class of configuration nodes
/// </summary>
public class NodeConfiguration
{
    /// <summary>
    /// Gets or sets an optional description
    /// </summary>
    [YamlMember(Order = 0)]
    public string? Description { get; set; }
}