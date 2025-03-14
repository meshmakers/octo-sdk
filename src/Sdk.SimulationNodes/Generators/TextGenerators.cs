using Bogus;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.SimulationNodes.Generators;

internal class WordLoremGenerator : IValueGenerator
{
    public object Generate(IEtlContext etlContext, Faker faker, JObject configuration)
    {
        var wordCount = configuration.GetValue("count", 1);

        var words = faker.Lorem.Words(wordCount);
        return string.Join(" ", words);
    }
}