using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

/// <summary>
/// Interface for data context of load stage
/// </summary>
public interface ILoadDataContext : IDataContext
{
    /// <summary>
    /// The current pipeline object. This is the object that is being processed by the pipeline in the transform stage.
    /// </summary>
    public JToken Target { get; }
}