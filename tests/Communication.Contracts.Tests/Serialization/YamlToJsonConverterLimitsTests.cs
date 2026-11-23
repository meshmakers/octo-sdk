using System.Diagnostics;
using System.Text;
using Meshmakers.Octo.Communication.Contracts.Serialization;

namespace Communication.Contracts.Tests.Serialization;

/// <summary>
///     The converter reads caller-supplied documents, so a small document must not be able to exhaust the memory or
///     the stack of the converting process: aliases that expand exponentially, documents nested deep enough to
///     overflow the recursive YAML loader, and recursive or forward aliases are refused before anything is loaded or
///     expanded.
/// </summary>
public class YamlToJsonConverterLimitsTests
{
    private static readonly TimeSpan Generous = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     The "billion laughs" shape: each level is a sequence of ten aliases to the level below, so ten levels
    ///     expand to 10^10 scalars from a document of about a kilobyte.
    /// </summary>
    private static string BillionLaughs(int levels = 10)
    {
        var yaml = new StringBuilder("l0: &l0 lol\n");
        for (var level = 1; level <= levels; level++)
        {
            yaml.Append($"l{level}: &l{level} [");
            yaml.Append(string.Join(", ", Enumerable.Repeat($"*l{level - 1}", 10)));
            yaml.Append("]\n");
        }

        return yaml.ToString();
    }

    private static string NestedFlowSequences(int depth) => new string('[', depth) + new string(']', depth);

    private static string NestedBlockMappings(int depth)
    {
        var yaml = new StringBuilder();
        for (var level = 0; level < depth; level++)
        {
            yaml.Append(' ', level * 2).Append("a:\n");
        }

        yaml.Append(' ', depth * 2).Append("b: 1\n");
        return yaml.ToString();
    }

    private static YamlDocumentLimitExceededException AssertRefused(Func<object?> convert, YamlDocumentLimit limit)
    {
        var stopwatch = Stopwatch.StartNew();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        var ex = Assert.Throws<YamlDocumentLimitExceededException>(convert);

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal(limit, ex.Limit);
        Assert.True(stopwatch.Elapsed < Generous, $"refusal took {stopwatch.Elapsed}");
        Assert.True(allocated < 256L * 1024 * 1024, $"refusal allocated {allocated / (1024 * 1024)} MB");
        return ex;
    }

    // ===== alias expansion =======================================================

    [Fact]
    public void BillionLaughs_IsRefusedBeforeExpansion()
    {
        var ex = AssertRefused(() => YamlToJsonConverter.ToJsonNode(BillionLaughs()), YamlDocumentLimit.Nodes);

        Assert.Equal(YamlToJsonConverterOptions.DefaultMaxNodes, ex.LimitValue);
        Assert.Contains("aliases are resolved", ex.Message);
    }

    [Fact]
    public void BillionLaughs_InAFlowMapping_IsRefusedThroughAutoDetect()
    {
        // `{` opens a YAML flow mapping too: the JSON attempt fails and the YAML path must still be bounded.
        var yaml = new StringBuilder("{ l0: &l0 [ lol ]");
        for (var level = 1; level <= 10; level++)
        {
            yaml.Append($" , l{level}: &l{level} [ ");
            yaml.Append(string.Join(" , ", Enumerable.Repeat($"*l{level - 1}", 10)));
            yaml.Append(" ]");
        }

        yaml.Append(" }");

        AssertRefused(() => YamlToJsonConverter.ToJsonNodeAutoDetect(yaml.ToString()), YamlDocumentLimit.Nodes);
    }

    [Fact]
    public void AliasesWithinTheBudget_AreStillExpanded()
    {
        // Three levels of ten: 1,000 scalars, far below the default budget.
        var node = YamlToJsonConverter.ToJsonNode(BillionLaughs(levels: 3));

        Assert.Equal(10, node!["l3"]!.AsArray().Count);
        Assert.Equal("lol", node["l3"]![9]![9]![9]!.GetValue<string>());
    }

    [Fact]
    public void RecursiveAlias_IsRefused()
    {
        AssertRefused(() => YamlToJsonConverter.ToJsonNode("a: &x [ 1, *x ]\n"), YamlDocumentLimit.Alias);
    }

    [Fact]
    public void ForwardAlias_IsRefused()
    {
        AssertRefused(() => YamlToJsonConverter.ToJsonNode("a: *x\nb: &x [ 1 ]\n"), YamlDocumentLimit.Alias);
    }

    [Fact]
    public void AliasToARedefinedAnchorThatIsStillOpen_IsRefused()
    {
        // The second `&x` names the new, still open sequence: the alias inside it is recursive although an
        // earlier node carried the same anchor.
        AssertRefused(() => YamlToJsonConverter.ToJsonNode("a: &x [ 1 ]\nb: &x [ *x ]\n"), YamlDocumentLimit.Alias);
    }

    // ===== nesting depth =========================================================

    [Fact]
    public void DeepFlowNesting_IsRefusedWithoutOverflowingTheStack()
    {
        // Deep enough to overflow the recursive YAML loader, which would crash the process uncatchably.
        AssertRefused(() => YamlToJsonConverter.ToJsonNode(NestedFlowSequences(200_000)), YamlDocumentLimit.Depth);
    }

    [Fact]
    public void DeepBlockNesting_IsRefused()
    {
        var ex = AssertRefused(() => YamlToJsonConverter.ToJsonNode(NestedBlockMappings(300)), YamlDocumentLimit.Depth);

        Assert.Equal(YamlToJsonConverterOptions.DefaultMaxDepth, ex.LimitValue);
    }

    [Fact]
    public void NestingAtTheDepthLimit_IsConverted()
    {
        var node = YamlToJsonConverter.ToJsonNode(NestedFlowSequences(YamlToJsonConverterOptions.DefaultMaxDepth));

        Assert.NotNull(node);
    }

    [Fact]
    public void NestingAtTheDepthLimit_IsConvertedOnASmallStack()
    {
        // The limit must keep the recursive loader and converter far from the end of a thread-pool sized stack.
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                YamlToJsonConverter.ToJsonNode(NestedBlockMappings(YamlToJsonConverterOptions.DefaultMaxDepth - 1));
            }
            catch (Exception e)
            {
                failure = e;
            }
        }, 256 * 1024);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }

    // ===== configured limits =====================================================

    [Fact]
    public void NodeBudget_CountsKeysAndValuesTheSameWayInScanAndConversion()
    {
        // A mapping, its key and its value: three nodes.
        Assert.NotNull(YamlToJsonConverter.ToJsonNode("a: 1\n", new YamlToJsonConverterOptions { MaxNodes = 3 }));

        AssertRefused(() => YamlToJsonConverter.ToJsonNode("a: 1\n", new YamlToJsonConverterOptions { MaxNodes = 2 }),
            YamlDocumentLimit.Nodes);
    }

    [Fact]
    public void ConfiguredDepth_IsEnforced()
    {
        var options = new YamlToJsonConverterOptions { MaxDepth = 2 };

        Assert.NotNull(YamlToJsonConverter.ToJsonNode("a:\n  b: 1\n", options));
        AssertRefused(() => YamlToJsonConverter.ToJsonNode("a:\n  b:\n    c: 1\n", options), YamlDocumentLimit.Depth);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(10, 0)]
    [InlineData(10, YamlToJsonConverterOptions.DefaultMaxDepth + 1)]
    public void InvalidOptions_AreRejected(long maxNodes, int maxDepth)
    {
        var options = new YamlToJsonConverterOptions { MaxNodes = maxNodes, MaxDepth = maxDepth };

        Assert.Throws<ArgumentOutOfRangeException>(() => YamlToJsonConverter.ToJsonNode("a: 1\n", options));
        Assert.Throws<ArgumentOutOfRangeException>(() => YamlToJsonConverter.ToJsonNodeAutoDetect("a: 1\n", options));
    }

    // ===== the refusal ===========================================================

    [Fact]
    public void Refusal_IsANotSupportedExceptionThatDoesNotQuoteTheDocument()
    {
        // Callers that already handle the converter's NotSupportedException keep handling the refusal, and the
        // message can be logged or returned: a refused definition may carry a credential.
        var yaml = "password: do-not-echo\n" + BillionLaughs();

        var ex = Assert.Throws<YamlDocumentLimitExceededException>(() => YamlToJsonConverter.ToJsonNode(yaml));

        Assert.IsAssignableFrom<NotSupportedException>(ex);
        Assert.DoesNotContain("do-not-echo", ex.Message);
        Assert.DoesNotContain("lol", ex.Message);
        Assert.Equal("YAML_DOCUMENT_LIMIT_EXCEEDED", YamlDocumentLimitExceededException.ErrorCode);
    }

    [Fact]
    public void MalformedTextAfterTheScan_StillFailsAsYaml()
    {
        // The scan stops at a syntax error without a refusal; the loader reports it as before.
        Assert.ThrowsAny<YamlDotNet.Core.YamlException>(() => YamlToJsonConverter.ToJsonNode("a: [1, 2\n"));
    }
}
