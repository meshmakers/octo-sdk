using System.Security.Cryptography;
using System.Text;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Represents a hash algorithm that can be used for calculating hash values.
/// </summary>
public enum HashAlgorithmDto
{
    /// <summary>
    /// MD5 hash algorithm (128-bit hash).
    /// </summary>
    Md5 = 0,

    /// <summary>
    /// SHA-1 hash algorithm (160-bit hash).
    /// </summary>
    Sha1 = 1,

    /// <summary>
    /// SHA-256 hash algorithm (256-bit hash).
    /// </summary>
    Sha256 = 2,

    /// <summary>
    /// SHA-384 hash algorithm (384-bit hash).
    /// </summary>
    Sha384 = 3,

    /// <summary>
    /// SHA-512 hash algorithm (512-bit hash).
    /// </summary>
    Sha512 = 4,
}

/// <summary>
/// Represents the input format for hash calculation.
/// </summary>
public enum HashInputFormatDto
{
    /// <summary>
    /// Input is treated as a UTF-8 encoded string.
    /// </summary>
    String = 0,

    /// <summary>
    /// Input is treated as a Base64 encoded string that will be decoded before hashing.
    /// </summary>
    Base64 = 1,
}

/// <summary>
/// Configuration for a hash calculation transformation node that computes cryptographic hash values.
/// </summary>
/// <remarks>
/// <para>
/// The HashNode calculates cryptographic hash values from string or Base64 encoded input data.
/// It supports multiple hash algorithms including MD5 and various SHA variants.
/// This is essential for data integrity verification, creating unique identifiers, and security operations.
/// </para>
/// <para>
/// Key capabilities include:
/// - Multiple hash algorithms: MD5, SHA1, SHA256, SHA384, SHA512
/// - String and Base64 input format support
/// - Lowercase hexadecimal output format
/// - Processing multiple values through array path selection
/// - Flexible source and target path configuration
/// - Strict null value handling (throws exception for null inputs)
/// - Integration with the ETL pipeline's path configuration system
/// </para>
/// <para>
/// Common use cases:
/// - Creating unique identifiers from data combinations
/// - Data integrity verification and checksums
/// - Password hashing and security operations
/// - Content fingerprinting and duplicate detection
/// - Cache key generation
/// - Digital signatures and verification workflows
/// </para>
/// </remarks>
/// <example>
/// Configuration to create SHA256 hashes of user emails:
/// <code>
/// {
///   "Path": "$.users[*]",
///   "SourcePath": "$.email",
///   "TargetPath": "$.emailHash",
///   "Algorithm": "SHA256",
///   "InputFormat": "String"
/// }
/// </code>
/// This creates SHA256 hashes for all user email addresses.
/// </example>
[NodeName("Hash", 1)]
public record HashNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the relative path to the source value within the selected objects.
    /// </summary>
    /// <remarks>
    /// This path is relative to each object selected by the Path property.
    /// The value at this location will be hashed according to the specified algorithm and input format.
    /// </remarks>
    /// <value>A JSONPath expression relative to the selected objects, e.g., "$.password".</value>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Specifies the hash algorithm to use for calculating the hash value.
    /// </summary>
    public required HashAlgorithmDto Algorithm { get; init; }

    /// <summary>
    /// Specifies the input format for the data to be hashed.
    /// Determines whether the input should be treated as a UTF-8 string or Base64 encoded data.
    /// </summary>
    public HashInputFormatDto InputFormat { get; init; } = HashInputFormatDto.String;
}

/// <summary>
/// A transformation node that calculates cryptographic hash values from input data.
/// </summary>
/// <remarks>
/// <para>
/// The HashNode performs cryptographic hash calculations on input values within the data context.
/// It supports multiple hash algorithms and input formats, providing comprehensive hashing capabilities
/// for security, data integrity, and identification purposes in ETL pipelines.
/// </para>
/// <para>
/// Processing workflow:
/// 1. Select source objects using the configured Path
/// 2. For each selected object, retrieve the value at SourcePath
/// 3. If the value is null, throw a PipelineExecutionException
/// 4. Convert the value to the appropriate input format (string or Base64)
/// 5. Calculate the hash using the specified algorithm
/// 6. Store the lowercase hexadecimal hash at the TargetPath
/// 7. Continue to the next node in the pipeline
/// </para>
/// <para>
/// The node enforces strict null checking - null values will cause execution to fail
/// with a clear error message. This ensures data integrity and prevents silent failures
/// in hash-dependent operations.
/// </para>
/// </remarks>
/// <example>
/// Example usage for creating content hashes:
/// <code>
/// // Input data:
/// {
///   "documents": [
///     { "title": "Document 1", "content": "This is the content of document 1" },
///     { "title": "Document 2", "content": "VGhpcyBpcyBiYXNlNjQgZW5jb2RlZCBjb250ZW50" }
///   ]
/// }
///
/// // Configuration for string content:
/// {
///   "Path": "$.documents[0]",
///   "SourcePath": "$.content",
///   "TargetPath": "$.contentHash",
///   "Algorithm": "SHA256",
///   "InputFormat": "String"
/// }
///
/// // Configuration for Base64 content:
/// {
///   "Path": "$.documents[1]",
///   "SourcePath": "$.content",
///   "TargetPath": "$.contentHash",
///   "Algorithm": "SHA256",
///   "InputFormat": "Base64"
/// }
///
/// // Result:
/// {
///   "documents": [
///     {
///       "title": "Document 1",
///       "content": "This is the content of document 1",
///       "contentHash": "a1b2c3d4e5f6..."
///     },
///     {
///       "title": "Document 2",
///       "content": "VGhpcyBpcyBiYXNlNjQgZW5jb2RlZCBjb250ZW50",
///       "contentHash": "f6e5d4c3b2a1..."
///     }
///   ]
/// }
/// </code>
/// </example>
/// <param name="next">The next node in the pipeline to execute after this transformation.</param>
[NodeConfiguration(typeof(HashNodeConfiguration))]
public class HashNode(NodeDelegate next) : IPipelineNode
{
    /// <summary>
    /// Processes the data context by calculating hash values for the specified input data.
    /// </summary>
    /// <param name="dataContext">The data context containing the JSON data to process.</param>
    /// <param name="nodeContext">The node context containing configuration and logging capabilities.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="PipelineExecutionException">
    /// Thrown when the input data context is null, when source values are null,
    /// or when Base64 decoding fails for invalid Base64 input.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method implements the hash calculation logic by:
    /// 1. Validating the data context contains data
    /// 2. Selecting source objects using the configured Path
    /// 3. For each source object, retrieving the value at SourcePath
    /// 4. Validating that the source value is not null (throws exception if null)
    /// 5. Converting the input based on the specified InputFormat
    /// 6. Calculating the hash using the specified algorithm
    /// 7. Storing the lowercase hexadecimal hash at TargetPath
    /// 8. Proceeding to the next node in the pipeline
    /// </para>
    /// <para>
    /// Hash calculation uses .NET's built-in cryptographic providers for security and reliability.
    /// Output is always formatted as lowercase hexadecimal strings for consistency.
    /// </para>
    /// </remarks>
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<HashNodeConfiguration>();
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

            if (sourceTokenValue == null || sourceTokenValue.Type == JTokenType.Null)
            {
                nodeContext.Error("Null value found at path '{0}.{1}' - hash calculation requires non-null input", c.Path, c.SourcePath);
                throw new PipelineExecutionException($"[{nodeContext.NodePath}]: Null value found at path '{c.Path}.{c.SourcePath}' - hash calculation requires non-null input");
            }

            var sourceValue = sourceTokenValue.ToString();
            var hashValue = CalculateHash(sourceValue, c, nodeContext);
            sourceToken.ReplaceNested(c.TargetPath, hashValue);
        }

        await next(dataContext, nodeContext).ConfigureAwait(false);
    }

    /// <summary>
    /// Calculates the hash value for the given input using the specified configuration.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <param name="config">The configuration containing algorithm and input format details.</param>
    /// <param name="nodeContext">The node context for error reporting.</param>
    /// <returns>The calculated hash as a lowercase hexadecimal string.</returns>
    /// <exception cref="PipelineExecutionException">
    /// Thrown when Base64 decoding fails or when an unsupported algorithm is specified.
    /// </exception>
    private static string CalculateHash(string input, HashNodeConfiguration config, INodeContext nodeContext)
    {
        byte[] inputBytes;

        try
        {
            inputBytes = config.InputFormat switch
            {
                HashInputFormatDto.String => Encoding.UTF8.GetBytes(input),
                HashInputFormatDto.Base64 => Convert.FromBase64String(input),
                _ => throw new PipelineExecutionException($"[{nodeContext.NodePath}]: Unsupported input format: {config.InputFormat}")
            };
        }
        catch (FormatException ex) when (config.InputFormat == HashInputFormatDto.Base64)
        {
            nodeContext.Error("Failed to decode Base64 input '{0}': {1}", input, ex.Message);
            throw new PipelineExecutionException($"[{nodeContext.NodePath}]: Failed to decode Base64 input - {ex.Message}", ex);
        }

        byte[] hashBytes;

        using (HashAlgorithm hashAlgorithm = config.Algorithm switch
        {
            HashAlgorithmDto.Md5 => MD5.Create(),
            HashAlgorithmDto.Sha1 => SHA1.Create(),
            HashAlgorithmDto.Sha256 => SHA256.Create(),
            HashAlgorithmDto.Sha384 => SHA384.Create(),
            HashAlgorithmDto.Sha512 => SHA512.Create(),
            _ => throw new PipelineExecutionException($"[{nodeContext.NodePath}]: Unsupported hash algorithm: {config.Algorithm}")
        })
        {
            hashBytes = hashAlgorithm.ComputeHash(inputBytes);
        }

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}