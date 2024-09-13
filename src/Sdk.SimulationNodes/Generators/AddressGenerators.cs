using Bogus;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.SimulationNodes.Generators;

internal class CityGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Address.City();
    }
}

internal class ZipCodeGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        var format = configuration["format"]?.Value<string>();
        return new Faker().Address.ZipCode(format ?? "####");
    }
}


internal class StreetNameGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Address.StreetName();
    }
}

internal class StreetAddressGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Address.StreetAddress();
    }
}

internal class BuildingNumberGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Address.BuildingNumber();
    }
}

internal class CountryCodeGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Address.CountryCode();
    }
}

internal class CountryGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Address.Country();
    }
}



