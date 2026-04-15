using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Represents a string manipulation operation that can be performed on string data.
/// </summary>
public enum StringOperationDto
{
    /// <summary>
    /// Trims whitespace from both ends of the string.
    /// </summary>
    Trim = 0,

    /// <summary>
    /// Trims whitespace from the beginning (left side) of the string.
    /// </summary>
    TrimStart = 1,

    /// <summary>
    /// Trims whitespace from the end (right side) of the string.
    /// </summary>
    TrimEnd = 2,

    /// <summary>
    /// Converts the string to uppercase.
    /// </summary>
    ToUpper = 3,

    /// <summary>
    /// Converts the string to lowercase.
    /// </summary>
    ToLower = 4,

    /// <summary>
    /// Extracts a substring from the beginning of the string with specified length.
    /// </summary>
    SubstringFromStart = 5,

    /// <summary>
    /// Extracts a substring from the end of the string with specified length.
    /// </summary>
    SubstringFromEnd = 6,

    /// <summary>
    /// Extracts a substring starting at a specific position with optional length.
    /// </summary>
    Substring = 7,
}

/// <summary>
/// Configuration for a string manipulation transformation node that performs various string operations.
/// </summary>
/// <remarks>
/// <para>
/// The TransformStringNode transforms string values at specified paths using various string operations
/// such as trimming, case conversion, and substring extraction. This is essential for data cleaning,
/// formatting, and text processing in ETL pipelines.
/// </para>
/// <para>
/// Key capabilities include:
/// - Trimming whitespace from strings (both sides, left, or right)
/// - Converting string case (uppercase or lowercase)
/// - Extracting substrings from the beginning, end, or specific positions
/// - Processing multiple values through array path selection
/// - Flexible source and target path configuration
/// - Null value handling (null values remain null)
/// - Integration with the ETL pipeline's path configuration system
/// </para>
/// <para>
/// Common use cases:
/// - Data cleaning and normalization
/// - Extracting prefixes, suffixes, or specific parts of strings
/// - Standardizing string formats and casing
/// - Processing user input data
/// - Preparing strings for comparison or matching operations
/// </para>
/// </remarks>
/// <example>
/// Configuration to trim and convert user names to lowercase:
/// <code>
/// {
///   "Path": "$.users[*]",
///   "SourcePath": "$.name",
///   "TargetPath": "$.cleanName",
///   "Operation": "ToLower"
/// }
/// </code>
/// This processes all user names by converting them to lowercase.
/// </example>
[NodeName("TransformString", 1)]
public record TransformStringNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the relative path to the source string value within the selected objects.
    /// </summary>
    /// <remarks>
    /// This path is relative to each object selected by the Path property.
    /// The value at this location should be a string or convertible to string.
    /// </remarks>
    /// <value>A JSONPath expression relative to the selected objects, e.g., "$.name".</value>
    [PropertyGroup("Paths", 2, "jsonpath")]
    public required string SourcePath { get; init; }

    /// <summary>
    /// Specifies the string manipulation operation to be performed on the data.
    /// </summary>
    [PropertyGroup("Options", 0)]
    public required StringOperationDto Operation { get; init; }

    /// <summary>
    /// The starting position for substring operations (Substring).
    /// This property is only used when Operation is set to Substring.
    /// Zero-based index where the substring should start.
    /// </summary>
    [PropertyGroup("Options", 1)]
    public int StartIndex { get; init; } = 0;

    /// <summary>
    /// The length of the substring to extract.
    /// Used for SubstringFromStart, SubstringFromEnd, and Substring operations.
    /// If not specified for Substring operation, extracts to the end of the string.
    /// For SubstringFromStart and SubstringFromEnd, this property is required.
    /// </summary>
    [PropertyGroup("Options", 2)]
    public int? Length { get; init; }
}

/// <summary>
/// A transformation node that performs various string manipulation operations on string values.
/// </summary>
/// <remarks>
/// <para>
/// The TransformStringNode performs string operations on string values within the data context.
/// It supports trimming, case conversion, and substring extraction operations,
/// providing comprehensive string processing capabilities for ETL pipelines.
/// </para>
/// <para>
/// Processing workflow:
/// 1. Select source objects using the configured Path
/// 2. For each selected object, retrieve the value at SourcePath
/// 3. If the value is not null, convert it to string and apply the specified operation
/// 4. Store the result at the TargetPath
/// 5. Continue to the next node in the pipeline
/// </para>
/// <para>
/// The node gracefully handles null values by preserving them without processing.
/// Non-string values are converted to strings before applying operations.
/// Invalid parameters (e.g., negative indices, lengths exceeding string bounds) are handled gracefully.
/// </para>
/// </remarks>
/// <example>
/// Example usage for cleaning and formatting names:
/// <code>
/// // Input data:
/// {
///   "users": [
///     { "name": "  John Doe  ", "email": "JOHN@EXAMPLE.COM" },
///     { "name": "jane smith", "email": "jane@example.com" }
///   ]
/// }
///
/// // Configuration for trimming names:
/// {
///   "Path": "$.users[*]",
///   "SourcePath": "$.name",
///   "TargetPath": "$.cleanName",
///   "Operation": "Trim"
/// }
///
/// // Result:
/// {
///   "users": [
///     { "name": "  John Doe  ", "email": "JOHN@EXAMPLE.COM", "cleanName": "John Doe" },
///     { "name": "jane smith", "email": "jane@example.com", "cleanName": "jane smith" }
///   ]
/// }
/// </code>
/// </example>
/// <param name="next">The next node in the pipeline to execute after this transformation.</param>
[NodeConfiguration(typeof(TransformStringNodeConfiguration))]
public class TransformStringNode(NodeDelegate next) : IPipelineNode
{
    /// <summary>
    /// Processes the data context by applying string manipulation operations to string values.
    /// </summary>
    /// <param name="dataContext">The data context containing the JSON data to process.</param>
    /// <param name="nodeContext">The node context containing configuration and logging capabilities.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="PipelineExecutionException">
    /// Thrown when the input data context is null or when invalid parameters are provided for substring operations.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method implements the string manipulation logic by:
    /// 1. Validating the data context contains data
    /// 2. Selecting source objects using the configured Path
    /// 3. For each source object, retrieving the value at SourcePath
    /// 4. Converting non-null values to strings and applying the specified operation
    /// 5. Storing the processed value at TargetPath
    /// 6. Proceeding to the next node in the pipeline
    /// </para>
    /// <para>
    /// String operations handle edge cases gracefully:
    /// - Substring operations check bounds and adjust parameters as needed
    /// - Null values are preserved without processing
    /// - Empty strings are processed according to the operation
    /// </para>
    /// </remarks>
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<TransformStringNodeConfiguration>();
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
                var result = ApplyStringOperation(sourceValue, c, nodeContext);
                sourceToken.ReplaceNested(c.TargetPath, result);
            }
            else
            {
                sourceToken.ReplaceNested(c.TargetPath, JValue.CreateNull());
            }
        }

        await next(dataContext, nodeContext).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies the specified string operation to the input string.
    /// </summary>
    /// <param name="input">The input string to process.</param>
    /// <param name="config">The configuration containing operation details.</param>
    /// <param name="nodeContext">The node context for logging.</param>
    /// <returns>The processed string result.</returns>
    private static string ApplyStringOperation(string input, TransformStringNodeConfiguration config, INodeContext nodeContext)
    {
        return config.Operation switch
        {
            StringOperationDto.Trim => input.Trim(),
            StringOperationDto.TrimStart => input.TrimStart(),
            StringOperationDto.TrimEnd => input.TrimEnd(),
            StringOperationDto.ToUpper => input.ToUpper(),
            StringOperationDto.ToLower => input.ToLower(),
            StringOperationDto.SubstringFromStart => GetSubstringFromStart(input, config, nodeContext),
            StringOperationDto.SubstringFromEnd => GetSubstringFromEnd(input, config, nodeContext),
            StringOperationDto.Substring => GetSubstring(input, config, nodeContext),
            _ => throw new NotSupportedException($"String operation {config.Operation} is not supported")
        };
    }

    /// <summary>
    /// Extracts a substring from the beginning of the string.
    /// </summary>
    private static string GetSubstringFromStart(string input, TransformStringNodeConfiguration config, INodeContext nodeContext)
    {
        if (!config.Length.HasValue)
        {
            nodeContext.Error("Length property is required for SubstringFromStart operation");
            throw new PipelineExecutionException($"[{nodeContext.NodePath}]: Length property is required for SubstringFromStart operation");
        }

        var length = Math.Min(config.Length.Value, input.Length);
        if (length <= 0)
        {
            return string.Empty;
        }

        return input.Substring(0, length);
    }

    /// <summary>
    /// Extracts a substring from the end of the string.
    /// </summary>
    private static string GetSubstringFromEnd(string input, TransformStringNodeConfiguration config, INodeContext nodeContext)
    {
        if (!config.Length.HasValue)
        {
            nodeContext.Error("Length property is required for SubstringFromEnd operation");
            throw new PipelineExecutionException($"[{nodeContext.NodePath}]: Length property is required for SubstringFromEnd operation");
        }

        var length = Math.Min(config.Length.Value, input.Length);
        if (length <= 0)
        {
            return string.Empty;
        }

        var startIndex = input.Length - length;
        return input.Substring(startIndex, length);
    }

    /// <summary>
    /// Extracts a substring starting at the specified position.
    /// </summary>
    private static string GetSubstring(string input, TransformStringNodeConfiguration config, INodeContext nodeContext)
    {
        if (config.StartIndex < 0 || config.StartIndex >= input.Length)
        {
            nodeContext.Warning("StartIndex {0} is out of bounds for string of length {1}, returning empty string", config.StartIndex, input.Length);
            return string.Empty;
        }

        if (!config.Length.HasValue)
        {
            // Extract from StartIndex to end of string
            return input.Substring(config.StartIndex);
        }

        var availableLength = input.Length - config.StartIndex;
        var length = Math.Min(config.Length.Value, availableLength);

        if (length <= 0)
        {
            return string.Empty;
        }

        return input.Substring(config.StartIndex, length);
    }
}