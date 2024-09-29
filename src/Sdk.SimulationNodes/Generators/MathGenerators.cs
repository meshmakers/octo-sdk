using Bogus;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.SimulationNodes.Generators;

internal class IntRandomGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        var min = 1;
        if (configuration.TryGetValue("min", out var amplitudeToken))
        {
            min = amplitudeToken.Value<int>();
        }

        var max = 100;
        if (configuration.TryGetValue("max", out var frequencyToken))
        {
            max = frequencyToken.Value<int>();
        }

        return new Faker().Random.Int(min, max);
    }
}

internal class SinusGenerator : IValueGenerator
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        // Apply sinus simulation
        var amplitude = 1;
        if (configuration.TryGetValue("amplitude", out var amplitudeToken))
        {
            amplitude = amplitudeToken.Value<int>();
        }

        var frequency = 1;
        if (configuration.TryGetValue("frequency", out var frequencyToken))
        {
            frequency = frequencyToken.Value<int>();
        }

        double value = amplitude * Math.Sin(2 * Math.PI * frequency * (DateTime.UtcNow - StartTime).TotalSeconds);
        return value;
    }
}

internal class TriangleGenerator : IValueGenerator
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        var amplitude = 1;
        if (configuration.TryGetValue("amplitude", out var amplitudeToken))
        {
            amplitude = amplitudeToken.Value<int>();
        }

        double frequency = 1;
        if (configuration.TryGetValue("frequency", out var frequencyToken))
        {
            frequency = frequencyToken.Value<double>();
        }

        double slope = 4 * amplitude / frequency;

        double elapsed = (DateTime.UtcNow - StartTime).TotalSeconds % frequency;
        double value = elapsed < frequency / 2
            ? slope * elapsed - amplitude
            : -slope * elapsed + 3 * amplitude;
        return value;
    }
}

internal class ConstantGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, JObject configuration)
    {
        var amplitude = 1;
        if (configuration.TryGetValue("amplitude", out var amplitudeToken))
        {
            amplitude = amplitudeToken.Value<int>();
        }

        return amplitude;
    }
}