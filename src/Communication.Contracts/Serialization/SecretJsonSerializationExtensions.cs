using System.Text.Json;
using Newtonsoft.Json;

namespace Meshmakers.Octo.Communication.Contracts.Serialization;

/// <summary>
///     Registers the <c>RtSecretValue</c> converters (AB#5528) on serializer configurations.
/// </summary>
public static class SecretJsonSerializationExtensions
{
    /// <summary>
    ///     Adds <see cref="RtSecretValueJsonConverter" /> unless it is already registered.
    /// </summary>
    /// <param name="options">Mutable serializer options</param>
    /// <returns>The same options</returns>
    public static JsonSerializerOptions AddOctoSecretConverters(this JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Converters.Any(c => c is RtSecretValueJsonConverter))
        {
            options.Converters.Add(new RtSecretValueJsonConverter());
        }

        return options;
    }

    /// <summary>
    ///     Adds <see cref="RtSecretValueNewtonsoftJsonConverter" /> unless it is already registered.
    /// </summary>
    /// <param name="settings">Serializer settings</param>
    /// <returns>The same settings</returns>
    public static JsonSerializerSettings AddOctoSecretConverters(this JsonSerializerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.Converters.Any(c => c is RtSecretValueNewtonsoftJsonConverter))
        {
            settings.Converters.Add(new RtSecretValueNewtonsoftJsonConverter());
        }

        return settings;
    }
}
