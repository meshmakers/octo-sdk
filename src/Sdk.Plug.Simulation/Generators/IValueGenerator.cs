using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Sdk.Plug.Simulation.Generators;

public interface IValueGenerator
{
    object? Generate(IEtlContext etlContext, JObject configuration);
}