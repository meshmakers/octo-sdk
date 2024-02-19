using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes;

internal class NodeLookupService
{
    private Dictionary<string, IObjectPipelineNode> _objectPipelineNodes;
    
    public NodeLookupService(IEnumerable<IObjectPipelineNode> objectPipelineNodes, IEnumerable<ISignalPipelineNode> signalPipelineNodes)
    {
        var node = objectPipelineNodes.GetType().GetConfigurationQualifiedName();
        _objectPipelineNodes = objectPipelineNodes.ToDictionary(x => x.GetType().GetConfigurationQualifiedName(), x => x);
    }
}