using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.SimulationNodes.Generators;

internal interface IValueGenerator
{
    object? Generate(IEtlContext etlContext, JObject configuration);
}