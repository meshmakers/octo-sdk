using Bogus;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Sdk.Plug.Simulation.Generators;

internal class FirstNameGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Person.FirstName;
    }
}


internal class LastNameGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Person.LastName;
    }
}

