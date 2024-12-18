using Bogus;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.SimulationNodes.Generators;

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

internal class EmailGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Person.Email;
    }
}

internal class DateOfBirthGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Person.DateOfBirth;
    }
}

internal class CompanyGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Person.Company;
    }
}

internal class GenderGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        return new Faker().Person.Gender;
    }
}

