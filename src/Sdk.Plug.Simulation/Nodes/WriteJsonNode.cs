using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Sdk.Plug.Simulation.Nodes;

[NodeName("WriteJson", 1)]
internal record WriteJsonConfiguration : NodeConfiguration
{
    public string JsonString { get; set; } = null!;
}


[NodeConfiguration(typeof(WriteJsonConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
internal class WriteJsonNode(NodeDelegate next) : IPipelineNode
{
    public Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<WriteJsonConfiguration>();
        
        dataContext.Current = JObject.Parse(c.JsonString);

        return next(dataContext);
    }
}