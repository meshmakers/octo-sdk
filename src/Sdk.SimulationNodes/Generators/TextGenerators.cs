using Bogus;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.SimulationNodes.Generators;

internal class WordLoremGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        var wordCount = 1;
        if (configuration.TryGetValue("count", out var amplitudeToken))
        {
            wordCount = amplitudeToken.Value<int>();
        }

        var words = new Faker().Lorem.Words(wordCount);
        return string.Join(" ", words);
    }
}