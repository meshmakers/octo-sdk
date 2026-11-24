namespace Meshmakers.Octo.Communication.Contracts.Serialization;

/// <summary>
///     Limits <see cref="YamlToJsonConverter" /> enforces before and while it converts a YAML document, so that a
///     small, caller-supplied document cannot exhaust the memory or the stack of the process that converts it.
/// </summary>
/// <remarks>
///     <para>
///         YAML aliases share one node in the document, but the conversion to JSON copies the anchored node to every
///         use site. A few lines of nested anchors and aliases (the "billion laughs" shape) therefore expand to an
///         unbounded JSON tree. <see cref="MaxNodes" /> counts the nodes as the conversion would produce them, with
///         every alias expanded, and refuses the document before anything is expanded.
///     </para>
///     <para>
///         The YAML loader builds its node tree recursively and compares mapping keys by content, so a deeply nested
///         document, or an alias chain that expands deep where it is used as a key, would overflow the stack (an
///         uncatchable crash) before the conversion could refuse it. <see cref="MaxDepth" /> is therefore checked on
///         the token stream, with every alias expanded where it stands, before the document is loaded.
///     </para>
///     <para>
///         The defaults are far above any real pipeline definition (the largest shipped pipelines have a few thousand
///         YAML nodes and nest a few dozen levels deep).
///     </para>
/// </remarks>
public sealed class YamlToJsonConverterOptions
{
    /// <summary>The default for <see cref="MaxNodes" />.</summary>
    public const long DefaultMaxNodes = 200_000;

    /// <summary>The default for <see cref="MaxDepth" />.</summary>
    public const int DefaultMaxDepth = 256;

    /// <summary>The options with the default limits.</summary>
    public static YamlToJsonConverterOptions Default { get; } = new();

    /// <summary>
    ///     Maximum number of YAML nodes (scalars, sequences, mappings) of the document after every alias is expanded
    ///     to the size of its anchored node. Must be positive.
    /// </summary>
    public long MaxNodes { get; init; } = DefaultMaxNodes;

    /// <summary>
    ///     Maximum nesting depth of sequences and mappings, with every alias expanded where it stands. Must be
    ///     positive and at most
    ///     <see cref="DefaultMaxDepth" />, so that the recursive YAML loader stays far away from the end of the stack.
    /// </summary>
    public int MaxDepth { get; init; } = DefaultMaxDepth;

    internal void Validate()
    {
        if (MaxNodes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxNodes), MaxNodes, "MaxNodes must be positive.");
        }

        if (MaxDepth is <= 0 or > DefaultMaxDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDepth), MaxDepth,
                $"MaxDepth must be between 1 and {DefaultMaxDepth}.");
        }
    }
}

/// <summary>Which limit of <see cref="YamlToJsonConverterOptions" /> a document exceeded.</summary>
public enum YamlDocumentLimit
{
    /// <summary>The document expands to more nodes than <see cref="YamlToJsonConverterOptions.MaxNodes" />.</summary>
    Nodes,

    /// <summary>The document nests deeper than <see cref="YamlToJsonConverterOptions.MaxDepth" />.</summary>
    Depth,

    /// <summary>
    ///     An alias refers to an anchor that is not complete at that point: one that contains the alias itself
    ///     (recursive) or that is defined only after it. Either would let the conversion expand without bound.
    /// </summary>
    Alias
}

/// <summary>
///     Thrown by <see cref="YamlToJsonConverter" /> when a document exceeds one of the limits of
///     <see cref="YamlToJsonConverterOptions" />. The document is refused, not converted. The message names the limit
///     and never quotes the document.
/// </summary>
/// <remarks>
///     Derives from <see cref="NotSupportedException" />, which the converter already throws for documents without a
///     JSON equivalent, so callers that handle that keep handling this one.
/// </remarks>
public sealed class YamlDocumentLimitExceededException : NotSupportedException
{
    /// <summary>A stable error code for answers and logs.</summary>
    public const string ErrorCode = "YAML_DOCUMENT_LIMIT_EXCEEDED";

    /// <summary>Creates the exception.</summary>
    /// <param name="limit">The limit the document exceeded.</param>
    /// <param name="limitValue">The value of that limit; 0 for <see cref="YamlDocumentLimit.Alias" />.</param>
    /// <param name="message">The message, naming the limit.</param>
    public YamlDocumentLimitExceededException(YamlDocumentLimit limit, long limitValue, string message)
        : base(message)
    {
        Limit = limit;
        LimitValue = limitValue;
    }

    /// <summary>The limit the document exceeded.</summary>
    public YamlDocumentLimit Limit { get; }

    /// <summary>The value of that limit; 0 for <see cref="YamlDocumentLimit.Alias" />.</summary>
    public long LimitValue { get; }
}
