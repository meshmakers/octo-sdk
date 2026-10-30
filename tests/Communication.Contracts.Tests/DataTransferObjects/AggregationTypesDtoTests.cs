using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Runtime.Contracts.Repositories.Query;

namespace Meshmakers.Octo.Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
/// <see cref="AggregationTypesDto"/> mirrors the engine's <see cref="AggregationFunction"/> (and so the System CK
/// enum <c>AggregationTypes</c>) shifted by one, because the transport enum starts with <c>None = 0</c>. A value
/// the engine gains (AB#7918: <c>Integral</c>) must appear here under the same name, or a client cannot ask for it.
/// </summary>
public class AggregationTypesDtoTests
{
    [Fact]
    public void EveryEngineAggregationFunction_HasTheSameNamedTransportValue_ShiftedByOne()
    {
        foreach (var function in Enum.GetValues<AggregationFunction>())
        {
            var name = function switch
            {
                AggregationFunction.Minimum => nameof(AggregationTypesDto.Minimum),
                AggregationFunction.Maximum => nameof(AggregationTypesDto.Maximum),
                _ => function.ToString()
            };

            Assert.True(Enum.TryParse<AggregationTypesDto>(name, out var dto), $"AggregationTypesDto has no value '{name}'.");
            Assert.Equal((int)function + 1, (int)dto);
        }
    }

    [Fact]
    public void Integral_IsEight()
    {
        // Wire value of AB#7918; the System CK key is 7, the transport enum is shifted by None = 0.
        Assert.Equal(8, (int)AggregationTypesDto.Integral);
        Assert.Equal(7, (int)AggregationFunction.Integral);
    }
}
