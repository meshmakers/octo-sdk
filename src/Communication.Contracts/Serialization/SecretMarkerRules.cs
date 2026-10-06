using System.Globalization;

namespace Meshmakers.Octo.Communication.Contracts.Serialization;

/// <summary>
///     The read-marker rules of a Secret value, enforced by the SDK converters themselves so they do not
///     depend on the engine version they run against (AB#5534).
/// </summary>
/// <remarks>
///     The marker is an object whose properties are a subset of the read state
///     <c>{ isSet, keyMissing, setAt }</c> (names case-insensitive): <c>isSet</c> and <c>keyMissing</c>
///     booleans, <c>setAt</c> an ISO-8601 date string or <c>null</c>. A client echoing the state object it
///     read therefore leaves the secret unchanged. The empty object is a marker too.
/// </remarks>
internal static class SecretMarkerRules
{
    public const string IsSetPropertyName = "isSet";
    public const string KeyMissingPropertyName = "keyMissing";
    public const string SetAtPropertyName = "setAt";

    public const string ExpectedShape =
        "A secret attribute value must be a string (the secret to store), null (clear) or the marker " +
        "{\"isSet\":true|false} (unchanged; optional \"keyMissing\": boolean and \"setAt\": date string or null)";

    public enum Kind
    {
        None,
        Boolean,
        Date
    }

    public static Kind Classify(string? propertyName)
    {
        if (string.Equals(propertyName, IsSetPropertyName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(propertyName, KeyMissingPropertyName, StringComparison.OrdinalIgnoreCase))
        {
            return Kind.Boolean;
        }

        return string.Equals(propertyName, SetAtPropertyName, StringComparison.OrdinalIgnoreCase)
            ? Kind.Date
            : Kind.None;
    }

    public static bool IsDateText(string? text)
    {
        return text != null &&
               DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _);
    }

    /// <summary>
    ///     Message for a wrongly typed marker property; names the token type only, never the value.
    /// </summary>
    public static string InvalidPropertyMessage(Kind kind, string tokenType)
    {
        return kind == Kind.Date
            ? $"The property '{SetAtPropertyName}' of a secret attribute marker must be a date string or null; got a token of type {tokenType}."
            : $"The properties '{IsSetPropertyName}' and '{KeyMissingPropertyName}' of a secret attribute marker must be booleans; got a token of type {tokenType}.";
    }

    public const string OtherPropertiesMessage =
        "A secret attribute object may only contain the properties 'isSet', 'keyMissing' and 'setAt'.";
}
