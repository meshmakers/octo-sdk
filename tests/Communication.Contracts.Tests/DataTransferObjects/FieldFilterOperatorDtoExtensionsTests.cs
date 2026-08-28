using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Runtime.Contracts.Repositories.Query;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#4956: guards the transport/engine field-filter operator mapping. The two enums are not numerically
///     compatible, so a numeric cast turns BETWEEN into CONTAINS. These tests fail as soon as a member is added
///     to either enum without extending the mapping.
/// </summary>
public class FieldFilterOperatorDtoExtensionsTests
{
    /// <summary>
    ///     Engine operators that have no transport counterpart. Match is tracked by AB#1231; the string operators
    ///     were added to the engine by AB#2179 and were never exposed on the wire.
    /// </summary>
    private static readonly FieldFilterOperator[] EngineOnlyOperators =
    [
        FieldFilterOperator.Match,
        FieldFilterOperator.Contains,
        FieldFilterOperator.StartsWith,
        FieldFilterOperator.EndsWith
    ];

    public static TheoryData<FieldFilterOperatorDto> AllDtoOperators()
    {
        var data = new TheoryData<FieldFilterOperatorDto>();
        foreach (var op in Enum.GetValues<FieldFilterOperatorDto>())
        {
            data.Add(op);
        }

        return data;
    }

    public static TheoryData<FieldFilterOperator> MappableEngineOperators()
    {
        var data = new TheoryData<FieldFilterOperator>();
        foreach (var op in Enum.GetValues<FieldFilterOperator>())
        {
            if (!EngineOnlyOperators.Contains(op))
            {
                data.Add(op);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllDtoOperators))]
    public void ToFieldFilterOperator_MapsEveryDtoMemberToTheEngineMemberOfTheSameName(FieldFilterOperatorDto op)
    {
        // Act
        var engineOperator = op.ToFieldFilterOperator();

        // Assert — the mapping is by meaning, not by number, so the names must line up.
        Assert.Equal(op.ToString(), engineOperator.ToString());
    }

    [Theory]
    [MemberData(nameof(MappableEngineOperators))]
    public void ToFieldFilterOperatorDto_MapsEveryEngineMemberWithACounterpartToTheSameName(FieldFilterOperator op)
    {
        // Act
        var dtoOperator = op.ToFieldFilterOperatorDto();

        // Assert
        Assert.Equal(op.ToString(), dtoOperator.ToString());
    }

    [Theory]
    [MemberData(nameof(AllDtoOperators))]
    public void RoundTrip_ThroughTheEngineOperator_ReturnsTheOriginalDtoOperator(FieldFilterOperatorDto op)
    {
        // Act
        var roundTripped = op.ToFieldFilterOperator().ToFieldFilterOperatorDto();

        // Assert
        Assert.Equal(op, roundTripped);
    }

    [Fact]
    public void NumericCast_WouldMisinterpretTheRangeAndNullOperators()
    {
        // Arrange — this is the defect AB#4956 is about: the values 13/14/15 carry different meanings in the two
        // enums, so the cast that used to sit at the call sites silently changed the semantics of the filter.
        Assert.Equal(FieldFilterOperator.Contains, (FieldFilterOperator)FieldFilterOperatorDto.Between);
        Assert.Equal(FieldFilterOperator.StartsWith, (FieldFilterOperator)FieldFilterOperatorDto.IsNull);
        Assert.Equal(FieldFilterOperator.EndsWith, (FieldFilterOperator)FieldFilterOperatorDto.IsNotNull);

        // Act + Assert — the mapping keeps the meaning.
        Assert.Equal(FieldFilterOperator.Between, FieldFilterOperatorDto.Between.ToFieldFilterOperator());
        Assert.Equal(FieldFilterOperator.IsNull, FieldFilterOperatorDto.IsNull.ToFieldFilterOperator());
        Assert.Equal(FieldFilterOperator.IsNotNull, FieldFilterOperatorDto.IsNotNull.ToFieldFilterOperator());
    }

    [Theory]
    [InlineData(FieldFilterOperator.Match)]
    [InlineData(FieldFilterOperator.Contains)]
    [InlineData(FieldFilterOperator.StartsWith)]
    [InlineData(FieldFilterOperator.EndsWith)]
    public void ToFieldFilterOperatorDto_ThrowsForEngineOnlyOperators(FieldFilterOperator op)
    {
        // Act + Assert — better a loud failure than a wrong operator on the wire.
        Assert.Throws<ArgumentOutOfRangeException>(() => op.ToFieldFilterOperatorDto());
    }

    [Fact]
    public void ToFieldFilterOperator_ThrowsForAnUndefinedDtoValue()
    {
        // Arrange
        const FieldFilterOperatorDto undefined = (FieldFilterOperatorDto)999;

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.ToFieldFilterOperator());
    }
}
