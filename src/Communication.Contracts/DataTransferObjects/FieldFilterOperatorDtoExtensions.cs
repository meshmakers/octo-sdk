using System;
using Meshmakers.Octo.Runtime.Contracts.Repositories.Query;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Conversions between the transport enum <see cref="FieldFilterOperatorDto" /> and the engine enum
///     <see cref="FieldFilterOperator" />.
/// </summary>
/// <remarks>
///     AB#4956: the two enums are deliberately NOT numerically compatible. <see cref="FieldFilterOperator" /> carries
///     Contains/StartsWith/EndsWith on 13/14/15 (inserted by AB#2179), where <see cref="FieldFilterOperatorDto" />
///     carries Between/IsNull/IsNotNull. A numeric cast therefore silently turns a BETWEEN filter into a CONTAINS
///     filter. Always convert through this class - never cast.
/// </remarks>
public static class FieldFilterOperatorDtoExtensions
{
    /// <summary>
    ///     Converts a transport operator into the engine operator of the same name.
    /// </summary>
    /// <param name="op">The transport operator.</param>
    /// <returns>The matching engine operator.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The operator has no engine counterpart.</exception>
    public static FieldFilterOperator ToFieldFilterOperator(this FieldFilterOperatorDto op)
    {
        return op switch
        {
            FieldFilterOperatorDto.Equals => FieldFilterOperator.Equals,
            FieldFilterOperatorDto.NotEquals => FieldFilterOperator.NotEquals,
            FieldFilterOperatorDto.LessThan => FieldFilterOperator.LessThan,
            FieldFilterOperatorDto.LessEqualThan => FieldFilterOperator.LessEqualThan,
            FieldFilterOperatorDto.GreaterThan => FieldFilterOperator.GreaterThan,
            FieldFilterOperatorDto.GreaterEqualThan => FieldFilterOperator.GreaterEqualThan,
            FieldFilterOperatorDto.In => FieldFilterOperator.In,
            FieldFilterOperatorDto.NotIn => FieldFilterOperator.NotIn,
            FieldFilterOperatorDto.Like => FieldFilterOperator.Like,
            FieldFilterOperatorDto.MatchRegEx => FieldFilterOperator.MatchRegEx,
            FieldFilterOperatorDto.AnyEq => FieldFilterOperator.AnyEq,
            FieldFilterOperatorDto.AnyLike => FieldFilterOperator.AnyLike,
            FieldFilterOperatorDto.Between => FieldFilterOperator.Between,
            FieldFilterOperatorDto.IsNull => FieldFilterOperator.IsNull,
            FieldFilterOperatorDto.IsNotNull => FieldFilterOperator.IsNotNull,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op,
                $"Field filter operator '{op}' has no engine counterpart.")
        };
    }

    /// <summary>
    ///     Converts an engine operator into the transport operator of the same name.
    /// </summary>
    /// <param name="op">The engine operator.</param>
    /// <returns>The matching transport operator.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     The operator is engine-only and cannot be expressed on the wire:
    ///     <see cref="FieldFilterOperator.Match" /> (see AB#1231) and the string operators
    ///     <see cref="FieldFilterOperator.Contains" />, <see cref="FieldFilterOperator.StartsWith" /> and
    ///     <see cref="FieldFilterOperator.EndsWith" />.
    /// </exception>
    public static FieldFilterOperatorDto ToFieldFilterOperatorDto(this FieldFilterOperator op)
    {
        return op switch
        {
            FieldFilterOperator.Equals => FieldFilterOperatorDto.Equals,
            FieldFilterOperator.NotEquals => FieldFilterOperatorDto.NotEquals,
            FieldFilterOperator.LessThan => FieldFilterOperatorDto.LessThan,
            FieldFilterOperator.LessEqualThan => FieldFilterOperatorDto.LessEqualThan,
            FieldFilterOperator.GreaterThan => FieldFilterOperatorDto.GreaterThan,
            FieldFilterOperator.GreaterEqualThan => FieldFilterOperatorDto.GreaterEqualThan,
            FieldFilterOperator.In => FieldFilterOperatorDto.In,
            FieldFilterOperator.NotIn => FieldFilterOperatorDto.NotIn,
            FieldFilterOperator.Like => FieldFilterOperatorDto.Like,
            FieldFilterOperator.MatchRegEx => FieldFilterOperatorDto.MatchRegEx,
            FieldFilterOperator.AnyEq => FieldFilterOperatorDto.AnyEq,
            FieldFilterOperator.AnyLike => FieldFilterOperatorDto.AnyLike,
            FieldFilterOperator.Between => FieldFilterOperatorDto.Between,
            FieldFilterOperator.IsNull => FieldFilterOperatorDto.IsNull,
            FieldFilterOperator.IsNotNull => FieldFilterOperatorDto.IsNotNull,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op,
                $"Field filter operator '{op}' cannot be expressed as a transport operator.")
        };
    }

    /// <summary>
    ///     Converts a CK model operator enum (<c>System/FieldFilterOperator</c>, e.g. as carried by a persisted
    ///     query) into the engine operator of the same name.
    /// </summary>
    /// <param name="op">The CK model enum value.</param>
    /// <returns>The matching engine operator.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The value has no engine counterpart.</exception>
    /// <remarks>
    ///     Matched by name on purpose: the CK enum is a third, independent numbering that today happens to agree
    ///     with the engine up to <see cref="FieldFilterOperator.Match" /> and would drift the same way the
    ///     transport enum did (AB#4956) as soon as a member is inserted on either side.
    /// </remarks>
    public static FieldFilterOperator FromCkModelEnum(Enum op)
    {
        if (op == null)
        {
            throw new ArgumentNullException(nameof(op));
        }

        var name = op.ToString();
        return name switch
        {
            "Equals" => FieldFilterOperator.Equals,
            "NotEquals" => FieldFilterOperator.NotEquals,
            "LessThan" => FieldFilterOperator.LessThan,
            "LessEqualThan" => FieldFilterOperator.LessEqualThan,
            "GreaterThan" => FieldFilterOperator.GreaterThan,
            "GreaterEqualThan" => FieldFilterOperator.GreaterEqualThan,
            "In" => FieldFilterOperator.In,
            "NotIn" => FieldFilterOperator.NotIn,
            "Like" => FieldFilterOperator.Like,
            "MatchRegEx" => FieldFilterOperator.MatchRegEx,
            "AnyEq" => FieldFilterOperator.AnyEq,
            "AnyLike" => FieldFilterOperator.AnyLike,
            "Match" => FieldFilterOperator.Match,
            "Contains" => FieldFilterOperator.Contains,
            "StartsWith" => FieldFilterOperator.StartsWith,
            "EndsWith" => FieldFilterOperator.EndsWith,
            "Between" => FieldFilterOperator.Between,
            "IsNull" => FieldFilterOperator.IsNull,
            "IsNotNull" => FieldFilterOperator.IsNotNull,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op,
                $"Field filter operator '{name}' has no engine counterpart.")
        };
    }
}
