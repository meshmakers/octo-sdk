using System.Globalization;
using System.Text;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration for a Base64 encoding transformation node that converts string values to Base64 encoded strings.
/// </summary>
/// <remarks>
/// <para>
/// The Base64EncodeNode transforms string values at specified paths into their Base64 encoded representations.
/// This is essential for data serialization, secure transmission, and storage of binary data as text.
/// </para>
/// <para>
/// Key capabilities include:
/// - Encoding string values to Base64 format using UTF-8 encoding
/// - Processing multiple values through array path selection
/// - Flexible source and target path configuration
/// - Null value handling (null values remain null)
/// - Integration with the ETL pipeline's path configuration system
/// </para>
/// <para>
/// Common use cases:
/// - Encoding credentials or sensitive data for configuration storage
/// - Preparing binary data for JSON/XML transmission
/// - Creating data URIs for embedded content
/// - Encoding file contents for API payloads
/// - Obfuscating plaintext values (note: not for security purposes)
/// </para>
/// </remarks>
/// <example>
/// Configuration to encode user credentials:
/// <code>
/// {
///   "Path": "$.users[*]",
///   "SourcePath": "$.password",
///   "TargetPath": "$.encodedPassword"
/// }
/// </code>
/// This encodes all user passwords to Base64 format.
/// </example>
[NodeName("Base64Encode", 1)]
public record Base64EncodeNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the relative path to the source value within the selected objects.
    /// </summary>
    /// <remarks>
    /// This path is relative to each object selected by the Path property.
    /// The value at this location will be encoded to Base64 format.
    /// </remarks>
    /// <value>A JSONPath expression relative to the selected objects, e.g., "$.password".</value>
    public required string SourcePath { get; init; }
}

/// <summary>
/// A transformation node that encodes string values to Base64 format.
/// </summary>
/// <remarks>
/// <para>
/// The Base64EncodeNode performs Base64 encoding operations on string values within the data context.
/// It uses UTF-8 encoding to convert strings to byte arrays before applying Base64 encoding,
/// ensuring consistent encoding across different platforms and character sets.
/// </para>
/// <para>
/// Processing workflow:
/// 1. Select source objects using the configured Path
/// 2. For each selected object, retrieve the value at SourcePath
/// 3. If the value is not null, convert it to a UTF-8 byte array
/// 4. Encode the byte array to a Base64 string
/// 5. Store the encoded string at the TargetPath
/// 6. Continue to the next node in the pipeline
/// </para>
/// <para>
/// The node gracefully handles null values by preserving them without encoding.
/// Non-string values are converted to strings before encoding using their ToString() representation.
/// </para>
/// </remarks>
/// <example>
/// Example usage for encoding API keys:
/// <code>
/// // Input data:
/// {
///   "services": [
///     { "name": "API1", "apiKey": "secret-key-123" },
///     { "name": "API2", "apiKey": "another-secret" }
///   ]
/// }
///
/// // Configuration:
/// {
///   "Path": "$.services[*]",
///   "SourcePath": "$.apiKey",
///   "TargetPath": "$.encodedApiKey"
/// }
///
/// // Result:
/// {
///   "services": [
///     { "name": "API1", "apiKey": "secret-key-123", "encodedApiKey": "c2VjcmV0LWtleS0xMjM=" },
///     { "name": "API2", "apiKey": "another-secret", "encodedApiKey": "YW5vdGhlci1zZWNyZXQ=" }
///   ]
/// }
/// </code>
/// </example>
/// <param name="next">The next node in the pipeline to execute after this transformation.</param>
[NodeConfiguration(typeof(Base64EncodeNodeConfiguration))]
public class Base64EncodeNode(NodeDelegate next) : IPipelineNode
{
    /// <summary>
    /// Processes the data context by encoding string values to Base64 format.
    /// </summary>
    /// <param name="dataContext">The data context containing the JSON data to process.</param>
    /// <param name="nodeContext">The node context containing configuration and logging capabilities.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="PipelineExecutionException">
    /// Thrown when the input data context is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method implements the Base64 encoding logic by:
    /// 1. Validating the data context contains data
    /// 2. Selecting source objects using the configured Path
    /// 3. For each source object, retrieving the value at SourcePath
    /// 4. Converting non-null values to Base64 using UTF-8 encoding
    /// 5. Storing the encoded value at TargetPath
    /// 6. Proceeding to the next node in the pipeline
    /// </para>
    /// <para>
    /// The encoding process uses System.Convert.ToBase64String with UTF-8 encoding,
    /// which is the standard encoding for web and API communications.
    /// </para>
    /// </remarks>
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<Base64EncodeNodeConfiguration>();
        if (dataContext.Current == null)
        {
            throw PipelineExecutionException.InputValueNull(nodeContext);
        }

        var sourceTokens = dataContext.Current.SelectTokens(c.Path).ToArray();

        if (!sourceTokens.Any())
        {
            nodeContext.Warning("No source data found at path '{0}'", c.Path);
            return;
        }

        foreach (var sourceToken in sourceTokens)
        {
            var sourceTokenValue = sourceToken.SelectToken(c.SourcePath);

            if (sourceTokenValue != null && sourceTokenValue.Type != JTokenType.Null)
            {
                var sourceValue = GetCultureInvariantString(sourceTokenValue);
                var bytes = Encoding.UTF8.GetBytes(sourceValue);
                var encodedValue = Convert.ToBase64String(bytes);
                sourceToken.ReplaceNested(c.TargetPath, encodedValue);
            }
            else
            {
                sourceToken.ReplaceNested(c.TargetPath, JValue.CreateNull());
            }
        }

        await next(dataContext, nodeContext).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts a JToken value to a culture-invariant string representation.
    /// </summary>
    /// <param name="token">The token to convert to string.</param>
    /// <returns>A culture-invariant string representation of the token value.</returns>
    /// <remarks>
    /// This method ensures that numeric values are formatted using the invariant culture,
    /// preventing locale-specific decimal separators (like commas) from affecting Base64 encoding consistency.
    /// </remarks>
    private static string GetCultureInvariantString(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Integer => token.Value<long>().ToString(CultureInfo.InvariantCulture),
            JTokenType.Float => token.Value<double>().ToString(CultureInfo.InvariantCulture),
            JTokenType.Boolean => token.Value<bool>().ToString(CultureInfo.InvariantCulture),
            JTokenType.Date => token.Value<DateTime>().ToString(CultureInfo.InvariantCulture),
            _ => token.ToString()
        };
    }
}