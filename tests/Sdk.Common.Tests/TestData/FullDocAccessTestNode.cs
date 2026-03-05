using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

namespace Sdk.Common.Tests.TestData;

/// <summary>
/// Result interface for tracking what FullDocAccessTestNode found.
/// </summary>
internal interface IFullDocAccessResult
{
    void RecordFound(bool found);
}

internal class FullDocAccessResult : IFullDocAccessResult
{
    public void RecordFound(bool found) { }
}

/// <summary>
/// Configuration for a test node that reads from a source path via Current.SelectToken
/// (simulating what real pipeline nodes like FormatStringNode, MathNode etc. do).
/// </summary>
[NodeName("FullDocAccessTest", 1)]
internal record FullDocAccessTestNodeConfiguration : TargetPathNodeConfiguration
{
    public string SourcePath { get; set; } = "$.full";
}

/// <summary>
/// Test node that reads data via dataContext.Current.SelectToken(path) — the same
/// pattern used by many real pipeline nodes. This is used to reproduce the bug
/// where $.full is in _sharedData but not accessible via Current.SelectToken.
/// </summary>
[NodeConfiguration(typeof(FullDocAccessTestNodeConfiguration))]
internal class FullDocAccessTestNode(NodeDelegate next, IFullDocAccessResult result) : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<FullDocAccessTestNodeConfiguration>();

        // This is the exact pattern used by real pipeline nodes like:
        // - FormatStringNode (line 50): dataContext.Current?.SelectToken(jsonPath)
        // - MathNode (line 111): dataContext.Current.SelectTokens(c.Path)
        // - SetPrimitiveValueNode (line 302): dataContext.Current?.SelectToken(config.ValuePath!)
        var token = dataContext.Current?.SelectToken(c.SourcePath);

        result.RecordFound(token != null);

        // Write the found value (or null) to the target path
        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode, token);

        await next(dataContext, nodeContext);
    }
}
