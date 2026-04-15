using System.Text;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration for a Base64 decoding transformation node that converts Base64 encoded strings back to their original string values.
/// </summary>
/// <remarks>
/// <para>
/// The Base64DecodeNode transforms Base64 encoded strings at specified paths back into their original string representations.
/// This is essential for deserializing encoded data, processing API responses, and retrieving original content from Base64 format.
/// </para>
/// <para>
/// Key capabilities include:
/// - Decoding Base64 strings to their original UTF-8 string format
/// - Processing multiple values through array path selection
/// - Flexible source and target path configuration
/// - Null value handling (null values remain null)
/// - Error handling for invalid Base64 strings
/// - Integration with the ETL pipeline's path configuration system
/// </para>
/// <para>
/// Common use cases:
/// - Decoding credentials or configuration data from secure storage
/// - Processing Base64 encoded API responses
/// - Extracting content from data URIs
/// - Decoding file contents received in API payloads
/// - Retrieving original values from obfuscated data
/// </para>
/// </remarks>
/// <example>
/// Configuration to decode user credentials:
/// <code>
/// {
///   "Path": "$.users[*]",
///   "SourcePath": "$.encodedPassword",
///   "TargetPath": "$.password"
/// }
/// </code>
/// This decodes all Base64 encoded passwords back to plaintext.
/// </example>
[NodeName("Base64Decode", 1)]
public record Base64DecodeNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the relative path to the Base64 encoded value within the selected objects.
    /// </summary>
    /// <remarks>
    /// This path is relative to each object selected by the Path property.
    /// The value at this location should be a valid Base64 encoded string.
    /// </remarks>
    /// <value>A JSONPath expression relative to the selected objects, e.g., "$.encodedPassword".</value>
    [PropertyGroup("Paths", 2, "jsonpath")]
    public required string SourcePath { get; init; }
}

/// <summary>
/// A transformation node that decodes Base64 encoded strings back to their original string values.
/// </summary>
/// <remarks>
/// <para>
/// The Base64DecodeNode performs Base64 decoding operations on string values within the data context.
/// It decodes Base64 strings to byte arrays and then converts them back to UTF-8 strings,
/// ensuring consistent decoding that matches the encoding process.
/// </para>
/// <para>
/// Processing workflow:
/// 1. Select source objects using the configured Path
/// 2. For each selected object, retrieve the value at SourcePath
/// 3. If the value is not null, decode it from Base64 to a byte array
/// 4. Convert the byte array to a UTF-8 string
/// 5. Store the decoded string at the TargetPath
/// 6. Continue to the next node in the pipeline
/// </para>
/// <para>
/// The node gracefully handles null values by preserving them without decoding.
/// Invalid Base64 strings will cause a FormatException, which is logged with context
/// information to aid in debugging data quality issues.
/// </para>
/// </remarks>
/// <example>
/// Example usage for decoding API keys:
/// <code>
/// // Input data:
/// {
///   "services": [
///     { "name": "API1", "encodedApiKey": "c2VjcmV0LWtleS0xMjM=" },
///     { "name": "API2", "encodedApiKey": "YW5vdGhlci1zZWNyZXQ=" }
///   ]
/// }
///
/// // Configuration:
/// {
///   "Path": "$.services[*]",
///   "SourcePath": "$.encodedApiKey",
///   "TargetPath": "$.apiKey"
/// }
///
/// // Result:
/// {
///   "services": [
///     { "name": "API1", "encodedApiKey": "c2VjcmV0LWtleS0xMjM=", "apiKey": "secret-key-123" },
///     { "name": "API2", "encodedApiKey": "YW5vdGhlci1zZWNyZXQ=", "apiKey": "another-secret" }
///   ]
/// }
/// </code>
/// </example>
/// <param name="next">The next node in the pipeline to execute after this transformation.</param>
[NodeConfiguration(typeof(Base64DecodeNodeConfiguration))]
public class Base64DecodeNode(NodeDelegate next) : IPipelineNode
{
    /// <summary>
    /// Processes the data context by decoding Base64 encoded strings back to their original values.
    /// </summary>
    /// <param name="dataContext">The data context containing the JSON data to process.</param>
    /// <param name="nodeContext">The node context containing configuration and logging capabilities.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="PipelineExecutionException">
    /// Thrown when the input data context is null.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when a value cannot be decoded because it contains invalid Base64 characters.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method implements the Base64 decoding logic by:
    /// 1. Validating the data context contains data
    /// 2. Selecting source objects using the configured Path
    /// 3. For each source object, retrieving the value at SourcePath
    /// 4. Converting non-null Base64 strings back to UTF-8 strings
    /// 5. Storing the decoded value at TargetPath
    /// 6. Proceeding to the next node in the pipeline
    /// </para>
    /// <para>
    /// The decoding process uses System.Convert.FromBase64String with UTF-8 decoding,
    /// which matches the standard encoding used by Base64EncodeNode.
    /// </para>
    /// <para>
    /// If an invalid Base64 string is encountered, the error is logged with the path
    /// information and the exception is re-thrown to maintain pipeline integrity.
    /// </para>
    /// </remarks>
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<Base64DecodeNodeConfiguration>();
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
                var sourceValue = sourceTokenValue.ToString();
                try
                {
                    var bytes = Convert.FromBase64String(sourceValue);
                    var decodedValue = Encoding.UTF8.GetString(bytes);
                    sourceToken.ReplaceNested(c.TargetPath, decodedValue);
                }
                catch (FormatException ex)
                {
                    nodeContext.Error("Failed to decode Base64 value at path '{0}.{1}': {2}",
                        c.Path, c.SourcePath, ex.Message);
                    throw;
                }
            }
            else
            {
                sourceToken.ReplaceNested(c.TargetPath, JValue.CreateNull());
            }
        }

        await next(dataContext, nodeContext).ConfigureAwait(false);
    }
}