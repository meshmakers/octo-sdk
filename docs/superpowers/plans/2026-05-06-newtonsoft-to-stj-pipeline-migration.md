# Newtonsoft → STJ Pipeline Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Newtonsoft.Json with System.Text.Json in the OctoMesh pipeline framework (`octo-sdk` + `octo-mesh-adapter`), introducing a layered zero-copy `IDataContext` and a custom JSONPath evaluator. Pipeline behavior must remain identical for every existing pipeline.

**Architecture:** Public `IDataContext` becomes path-only (no JSON types in signatures). Internally: read base = `JsonElement` view over a shared UTF-8 buffer (zero-copy); write overlay = sparse `JsonNode` lifts with copy-on-write semantics; iteration child contexts share the parent's read base via JsonElement aliases (no per-iteration clone). JSONPath is a custom ~700 LOC evaluator over `JsonElement`, supporting only the dialect features actually used by in-tree code and production pipelines.

**Tech Stack:** C# 13 / .NET 10 + netstandard2.0 multi-target, `System.Text.Json` 10.x, xUnit v3, FakeItEasy, Bogus. No new third-party dependencies. Newtonsoft.Json kept only in the parity test project.

**Spec:** [`2026-05-06-newtonsoft-to-stj-pipeline-migration-design.md`](../specs/2026-05-06-newtonsoft-to-stj-pipeline-migration-design.md)

---

## Plan revision history

- **2026-05-06 (initial)** — created from spec.
- **2026-05-06 (rev1)** — added Phase 2A (orchestration scaffolding), Phase 2B (DataPipelineException port), Tasks 6.6–6.12 (configuration/serialization/debugger/JTokenExtensions deletion), Phase 6A (Adapters and Services), Phase 6B (trigger contexts). Reason: discovered during execution that the migration scope extends beyond `Nodes/` — orchestrator, trigger contexts, configuration serializers, debugger, `Adapters/`, `Services/` all consume the old `IDataContext` API. They were temporarily excluded via `<Compile Remove>` blocks in `Sdk.Common.csproj` and `Sdk.Common.Tests.csproj` to unblock Phase 2 development; the new tasks restore them progressively.
- **2026-05-07 (rev2)** — added Task 9.5 (multi-match fix for anomaly nodes), Phase 11 (restore excluded unit tests), and a deferred-perf note for the CreateSubContext β/γ optimization. Reason: discovered during Phase 9 execution that `StatisticalAnomalyNode`/`MachineLearningAnomalyNode` lost recursive-descent semantics (correctness), ~22 mesh-adapter unit tests + several octo-sdk node test files were excluded due to legacy mock patterns (test-coverage gap), and the CreateSubContext optimization (β/γ) was deferred to post-benchmark assessment.

### Cross-cutting notes for all rev1 tasks

- **`<Compile Remove>` exclusion blocks:** while migration is in progress, these blocks live in:
  - `octo-sdk/src/Sdk.Common/Sdk.Common.csproj` (production source exclusions)
  - `octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj` (mirror test-side exclusions)
  Each rev1 task must (a) restore the file from the production exclusion list, (b) migrate it, (c) restore mirroring test exclusions when corresponding tests are re-enabled. The acceptance criterion for the merge is that **both** `<Compile Remove>` blocks are empty.
- **Migration transformation rules:** apply the rules from Task 6.1 unless a task explicitly calls for a different shape.
- **netstandard2.0 caveat:** `Sdk.Common` multi-targets `net10.0;netstandard2.0`. The shorthand `foreach (var (k, v) in jsonObject)` deconstruction does NOT work on netstandard2.0 — `KeyValuePair<,>` deconstruction is not in scope. Use `foreach (var kvp in jsonObject)` and reference `kvp.Key` / `kvp.Value` explicitly. This applies to every iteration over `JsonObject`, `IDictionary<,>`, etc. in code that ends up in `Sdk.Common`.

---

## File Structure

### New files

```
octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/
  PathSegment.cs                  # AST records (Root, Property, Index, Wildcard, RecursiveDescent, Filter)
  JsonPathParser.cs               # tokenizer + recursive-descent parser → AST
  JsonPathEvaluator.cs            # walks JsonElement, yields matches with canonical paths
  JsonPathException.cs            # parse/eval errors
  JsonPathNotSupportedException.cs # unsupported feature errors
  CanonicalPath.cs                # path normalization (used by overlay key matching)

octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext/
  IDataContext.cs                 # path-only public API (replaces existing IDataContext)
  DataContext.cs                  # layered impl
  DataKind.cs                     # enum
  DataOverlay.cs                  # subtree-lift overlay with copy-on-write
  DataNodeAlias.cs                # internal struct: (path, JsonElement) for zero-copy iteration aliases
  PipelineJsonOptions.cs          # canonical JsonSerializerOptions

octo-sdk/tests/Sdk.Common.PipelineParityTests/
  Sdk.Common.PipelineParityTests.csproj
  ParityCorpus.cs                 # corpus loader (JSON inputs)
  PathExpressions.cs              # corpus of JSONPath strings (in-tree + production)
  ReadParityTests.cs              # Newtonsoft.SelectToken vs new evaluator
  WriteParityTests.cs             # Newtonsoft SetByPath vs new IDataContext.Set
  IterationParityTests.cs         # Newtonsoft ForEach vs new IterateArrayAsync
  TestData/                       # synthetic + sanitized real inputs
```

### Modified files (high-level — full list in tasks)

```
octo-sdk/src/Sdk.Common/Sdk.Common.csproj                # remove Newtonsoft package
octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/**         # ~70 GREEN nodes (mechanical), 3 YELLOW, 3 iteration nodes
octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Buffering/LiteDbBsonConverter.cs   # full rewrite (JsonNode ↔ BSON)
octo-sdk/src/Sdk.Common/EtlDataPipeline/DataPipelineException.cs                 # rename JTokenType refs
octo-sdk/src/Sdk.SimulationNodes/**                                              # follow-on node migrations
octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/**                                   # all nodes; UpdateRecordArrayItemNode RED refactor
octo-sdk/tests/Sdk.Common.Tests/**                                               # tests rewritten against new API
deployment/energy-community-deployment/data/_calculation/energy-create-all-billing-documents.yaml  # refactor double-dot
```

### Deleted files

```
octo-sdk/src/Sdk.Common/JTokenExtensions.cs              # absorbed into DataOverlay + JsonPathEvaluator
octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext.cs   # replaced by DataContext/DataContext.cs (note path change)
```

---

## Phase 0 — Setup and Baseline Benchmarks

### Task 0.1: Confirm working tree is clean and on a feature branch

**Files:**
- Verify: working tree clean

- [ ] **Step 1: Confirm clean state across all sub-repos**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk status --short
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter status --short
```

Expected: empty output for both.

- [ ] **Step 2: Create feature branches**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk checkout -b dev/newtonsoft-to-stj-pipeline
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter checkout -b dev/newtonsoft-to-stj-pipeline
```

### Task 0.2: Capture baseline ForEachNode memory benchmark

**Files:**
- Create: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/Benchmarks/ForEachMemoryBenchmark.cs`

- [ ] **Step 1: Write benchmark using BenchmarkDotNet (or xUnit timing measurement if BenchmarkDotNet is unavailable)**

```csharp
using System.Diagnostics;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline.Benchmarks;

public class ForEachMemoryBenchmark
{
    [Fact]
    public void ForEach_BaselineMemoryFootprint()
    {
        // Build a 5MB document with a 1000-element iteration array.
        var fullDoc = new JObject();
        var arr = new JArray();
        for (var i = 0; i < 1000; i++)
        {
            arr.Add(new JObject
            {
                ["id"] = i,
                ["payload"] = new string('x', 5000)
            });
        }
        fullDoc["items"] = arr;
        fullDoc["bigBlob"] = new string('y', 1_000_000);

        // Measure allocations during a simulated ForEach: clone the full doc 1000 times.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var beforeBytes = GC.GetTotalAllocatedBytes(precise: true);
        var sw = Stopwatch.StartNew();

        long checksum = 0;
        for (var i = 0; i < 1000; i++)
        {
            var clone = (JObject)fullDoc.DeepClone();
            checksum += clone["items"]!.Count();
        }

        sw.Stop();
        var deltaBytes = GC.GetTotalAllocatedBytes(precise: true) - beforeBytes;

        Assert.Equal(1_000_000L, checksum);
        // Print baseline; we expect deltaBytes >> 1 GB.
        Console.WriteLine($"Baseline: {deltaBytes / (1024 * 1024)} MB allocated, {sw.ElapsedMilliseconds} ms");
    }
}
```

- [ ] **Step 2: Run benchmark and record result**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~ForEachMemoryBenchmark" -c DebugL --logger "console;verbosity=normal"
```

Record the printed `Baseline: X MB allocated, Y ms` in a file:

```bash
echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) baseline-newtonsoft: <paste output>" >> \
  /Users/reimar/dev/meshmakers/branches/main/octo-sdk/docs/superpowers/plans/baseline-perf.txt
```

- [ ] **Step 3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add tests/Sdk.Common.Tests/EtlDataPipeline/Benchmarks/ForEachMemoryBenchmark.cs docs/superpowers/plans/baseline-perf.txt
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "test: capture baseline memory benchmark for ForEachNode-style cloning"
```

---

## Phase 1 — Custom JSONPath Evaluator

The evaluator is built TDD-first. We write tests against the dialect surface described in the spec §6.2, then implement.

### Task 1.1: Create JSONPath project structure

**Files:**
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/PathSegment.cs`

- [ ] **Step 1: Define AST records**

```csharp
namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

public abstract record PathSegment;

public sealed record RootSegment : PathSegment;
public sealed record PropertySegment(string Name) : PathSegment;
public sealed record IndexSegment(int Index) : PathSegment;
public sealed record WildcardSegment : PathSegment;
public sealed record RecursiveDescentSegment : PathSegment;

/// <summary>
/// Equality filter on a relative property path: [?(@.Foo == 'literal')].
/// Only string-literal equality is supported per spec §6.2.
/// </summary>
public sealed record FilterSegment(IReadOnlyList<string> RelativeProperty, string Literal) : PathSegment;

public sealed record JsonPathExpression(IReadOnlyList<PathSegment> Segments);
```

- [ ] **Step 2: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/PathSegment.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): add PathSegment AST records"
```

### Task 1.2: Add exception types

**Files:**
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathException.cs`
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathNotSupportedException.cs`

- [ ] **Step 1: Define exceptions**

```csharp
// JsonPathException.cs
namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

public class JsonPathException : Exception
{
    public string Path { get; }
    public int Position { get; }

    public JsonPathException(string message, string path, int position)
        : base($"{message} (path: '{path}', position {position})")
    {
        Path = path;
        Position = position;
    }
}
```

```csharp
// JsonPathNotSupportedException.cs
namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

public sealed class JsonPathNotSupportedException : JsonPathException
{
    public string Feature { get; }

    public JsonPathNotSupportedException(string feature, string path, int position)
        : base($"JSONPath feature '{feature}' is not supported by the OctoMesh evaluator", path, position)
    {
        Feature = feature;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathException.cs src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathNotSupportedException.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): add exception types"
```

### Task 1.3: TDD — Parser handles root and plain dotted paths

**Files:**
- Create: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathParserTests.cs`
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathParser.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline.JsonPath;

public class JsonPathParserTests
{
    [Fact]
    public void Parse_Root_ProducesRootSegmentOnly()
    {
        var expr = JsonPathParser.Parse("$");
        Assert.Single(expr.Segments);
        Assert.IsType<RootSegment>(expr.Segments[0]);
    }

    [Fact]
    public void Parse_DottedPath_ProducesPropertySegments()
    {
        var expr = JsonPathParser.Parse("$.foo.bar");
        Assert.Equal(3, expr.Segments.Count);
        Assert.IsType<RootSegment>(expr.Segments[0]);
        Assert.Equal(new PropertySegment("foo"), expr.Segments[1]);
        Assert.Equal(new PropertySegment("bar"), expr.Segments[2]);
    }

    [Fact]
    public void Parse_PathWithUnderscore_HandlesIdentifierChars()
    {
        var expr = JsonPathParser.Parse("$._items.full_doc");
        Assert.Equal(new PropertySegment("_items"), expr.Segments[1]);
        Assert.Equal(new PropertySegment("full_doc"), expr.Segments[2]);
    }

    [Fact]
    public void Parse_EmptyString_Throws()
    {
        Assert.Throws<JsonPathException>(() => JsonPathParser.Parse(""));
    }

    [Fact]
    public void Parse_BarePathWithoutRoot_Throws()
    {
        var ex = Assert.Throws<JsonPathException>(() => JsonPathParser.Parse("foo.bar"));
        Assert.Contains("must start with '$'", ex.Message);
    }
}
```

- [ ] **Step 2: Run tests — they fail (parser doesn't exist)**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~JsonPathParserTests" -c DebugL
```

Expected: FAIL — `The type or namespace name 'JsonPathParser' does not exist`.

- [ ] **Step 3: Implement minimal parser**

```csharp
// JsonPathParser.cs
namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

public static class JsonPathParser
{
    public static JsonPathExpression Parse(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new JsonPathException("Path is empty", path, 0);
        }

        var segments = new List<PathSegment>();
        var pos = 0;

        if (path[pos] != '$')
        {
            throw new JsonPathException("Path must start with '$'", path, pos);
        }
        segments.Add(new RootSegment());
        pos++;

        while (pos < path.Length)
        {
            if (path[pos] == '.')
            {
                pos++;
                if (pos >= path.Length)
                {
                    throw new JsonPathException("Trailing '.' with no property", path, pos);
                }

                var start = pos;
                while (pos < path.Length && IsIdentifierChar(path[pos]))
                {
                    pos++;
                }
                if (pos == start)
                {
                    throw new JsonPathException("Expected property name after '.'", path, pos);
                }
                segments.Add(new PropertySegment(path.Substring(start, pos - start)));
            }
            else
            {
                throw new JsonPathException($"Unexpected character '{path[pos]}'", path, pos);
            }
        }

        return new JsonPathExpression(segments);
    }

    private static bool IsIdentifierChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_';
}
```

- [ ] **Step 4: Run tests — they pass**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~JsonPathParserTests" -c DebugL
```

Expected: PASS, 5 tests.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathParser.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathParserTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): parse root and plain dotted paths"
```

### Task 1.4: TDD — Parser handles array indices and wildcards

- [ ] **Step 1: Add failing tests to JsonPathParserTests.cs**

```csharp
[Fact]
public void Parse_ArrayIndex_ProducesIndexSegment()
{
    var expr = JsonPathParser.Parse("$.arr[0]");
    Assert.Equal(3, expr.Segments.Count);
    Assert.Equal(new IndexSegment(0), expr.Segments[2]);
}

[Fact]
public void Parse_Wildcard_ProducesWildcardSegment()
{
    var expr = JsonPathParser.Parse("$.arr[*]");
    Assert.IsType<WildcardSegment>(expr.Segments[2]);
}

[Fact]
public void Parse_WildcardWithDescent_HandlesChain()
{
    var expr = JsonPathParser.Parse("$.items[*].name");
    Assert.Equal(4, expr.Segments.Count);
    Assert.IsType<WildcardSegment>(expr.Segments[2]);
    Assert.Equal(new PropertySegment("name"), expr.Segments[3]);
}

[Fact]
public void Parse_NegativeIndex_Throws()
{
    Assert.Throws<JsonPathNotSupportedException>(() => JsonPathParser.Parse("$.arr[-1]"));
}

[Fact]
public void Parse_ArraySlice_ThrowsNotSupported()
{
    var ex = Assert.Throws<JsonPathNotSupportedException>(() => JsonPathParser.Parse("$.arr[1:3]"));
    Assert.Contains("slice", ex.Feature, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run — fail**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~JsonPathParserTests" -c DebugL
```

Expected: FAIL on the new tests.

- [ ] **Step 3: Extend parser to handle `[N]`, `[*]`, and reject `[-N]` / `[a:b]`**

Modify `JsonPathParser.Parse` — replace the `else` branch that throws on unexpected characters with:

```csharp
else if (path[pos] == '[')
{
    pos++; // consume '['
    if (pos >= path.Length)
    {
        throw new JsonPathException("Unterminated '['", path, pos);
    }

    if (path[pos] == '*')
    {
        pos++;
        ExpectClosingBracket(path, ref pos);
        segments.Add(new WildcardSegment());
    }
    else if (path[pos] == '-')
    {
        throw new JsonPathNotSupportedException("negative array index", path, pos);
    }
    else if (char.IsDigit(path[pos]))
    {
        var numStart = pos;
        while (pos < path.Length && char.IsDigit(path[pos])) pos++;

        // Detect slice: digits followed by ':'
        if (pos < path.Length && path[pos] == ':')
        {
            throw new JsonPathNotSupportedException("array slice", path, pos);
        }

        var index = int.Parse(path.Substring(numStart, pos - numStart));
        ExpectClosingBracket(path, ref pos);
        segments.Add(new IndexSegment(index));
    }
    else if (path[pos] == '?' || path[pos] == '\'' || path[pos] == '"')
    {
        // Filter or bracket-property: defer to later tasks.
        throw new JsonPathNotSupportedException(path[pos] == '?' ? "filter" : "bracket-property", path, pos);
    }
    else
    {
        throw new JsonPathException($"Unexpected character '{path[pos]}' inside '['", path, pos);
    }
}
else
{
    throw new JsonPathException($"Unexpected character '{path[pos]}'", path, pos);
}
```

Add helper:

```csharp
private static void ExpectClosingBracket(string path, ref int pos)
{
    if (pos >= path.Length || path[pos] != ']')
    {
        throw new JsonPathException("Expected ']'", path, pos);
    }
    pos++;
}
```

- [ ] **Step 4: Run — pass**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~JsonPathParserTests" -c DebugL
```

Expected: PASS, all tests.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathParser.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathParserTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): parse array indices and wildcards"
```

### Task 1.5: TDD — Parser handles recursive descent

- [ ] **Step 1: Add failing tests**

```csharp
[Fact]
public void Parse_RecursiveDescent_ProducesRecursiveSegment()
{
    var expr = JsonPathParser.Parse("$..foo");
    Assert.Equal(3, expr.Segments.Count);
    Assert.IsType<RootSegment>(expr.Segments[0]);
    Assert.IsType<RecursiveDescentSegment>(expr.Segments[1]);
    Assert.Equal(new PropertySegment("foo"), expr.Segments[2]);
}

[Fact]
public void Parse_RecursiveDescentWithWildcard_Parses()
{
    var expr = JsonPathParser.Parse("$..[*]");
    Assert.Equal(3, expr.Segments.Count);
    Assert.IsType<RecursiveDescentSegment>(expr.Segments[1]);
    Assert.IsType<WildcardSegment>(expr.Segments[2]);
}
```

- [ ] **Step 2: Run — fail**

- [ ] **Step 3: Extend parser** — in the `path[pos] == '.'` branch, before consuming as property, check for second `.`:

```csharp
if (path[pos] == '.')
{
    pos++;
    if (pos < path.Length && path[pos] == '.')
    {
        pos++; // consume second '.'
        segments.Add(new RecursiveDescentSegment());
        continue; // next segment will follow
    }
    // ... existing identifier parsing
}
```

Place the `continue` in the loop, after which the next iteration reads either a property (e.g. `..foo` → next is identifier — handle by NOT requiring leading `.` after recursive descent) or an `[` bracket selector.

Note: After a `RecursiveDescentSegment`, the next segment may be a property (e.g., `..foo`) without a leading dot. Adjust the loop so that after appending a `RecursiveDescentSegment`, an immediate identifier or `[` is recognized.

```csharp
// Inside the main while loop, after appending RecursiveDescentSegment:
if (pos < path.Length && IsIdentifierChar(path[pos]))
{
    var start = pos;
    while (pos < path.Length && IsIdentifierChar(path[pos])) pos++;
    segments.Add(new PropertySegment(path.Substring(start, pos - start)));
}
// brackets handled by next loop iteration as normal
```

- [ ] **Step 4: Run — pass**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathParser.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathParserTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): parse recursive descent"
```

### Task 1.6: TDD — Parser handles equality filters

- [ ] **Step 1: Add failing tests**

```csharp
[Fact]
public void Parse_EqualityFilter_BasicForm()
{
    var expr = JsonPathParser.Parse("$.items[?(@.Id == 'abc')]");
    var filter = Assert.IsType<FilterSegment>(expr.Segments[2]);
    Assert.Equal(new[] { "Id" }, filter.RelativeProperty);
    Assert.Equal("abc", filter.Literal);
}

[Fact]
public void Parse_EqualityFilter_NestedRelativePath()
{
    var expr = JsonPathParser.Parse("$.items[?(@.attrs.code == 'X1')]");
    var filter = Assert.IsType<FilterSegment>(expr.Segments[2]);
    Assert.Equal(new[] { "attrs", "code" }, filter.RelativeProperty);
}

[Fact]
public void Parse_EqualityFilter_RecursiveDescentInside()
{
    // From production: $..[?(@.Id=='Machine_xxx')].Value
    var expr = JsonPathParser.Parse("$..[?(@.Id == 'Machine_1')].Value");
    Assert.IsType<RootSegment>(expr.Segments[0]);
    Assert.IsType<RecursiveDescentSegment>(expr.Segments[1]);
    Assert.IsType<FilterSegment>(expr.Segments[2]);
    Assert.Equal(new PropertySegment("Value"), expr.Segments[3]);
}

[Fact]
public void Parse_FilterWithDoubleEqualsAndNoSpaces_Parses()
{
    var expr = JsonPathParser.Parse("$.items[?(@.Id=='abc')]");
    var filter = Assert.IsType<FilterSegment>(expr.Segments[2]);
    Assert.Equal("abc", filter.Literal);
}

[Fact]
public void Parse_FilterWithUnsupportedOperator_Throws()
{
    Assert.Throws<JsonPathNotSupportedException>(() => JsonPathParser.Parse("$.items[?(@.x > 5)]"));
}

[Fact]
public void Parse_FilterWithLogicalOperator_Throws()
{
    Assert.Throws<JsonPathNotSupportedException>(() => JsonPathParser.Parse("$.items[?(@.x == 'a' && @.y == 'b')]"));
}

[Fact]
public void Parse_FilterWithRegex_Throws()
{
    Assert.Throws<JsonPathNotSupportedException>(() => JsonPathParser.Parse("$.items[?(@.x =~ /abc/)]"));
}
```

- [ ] **Step 2: Run — fail**

- [ ] **Step 3: Extend parser to handle `[?(@.path == 'literal')]`**

Replace the `path[pos] == '?'` branch in the `[` handling:

```csharp
else if (path[pos] == '?')
{
    pos++; // consume '?'
    if (pos >= path.Length || path[pos] != '(')
    {
        throw new JsonPathException("Expected '(' after '?'", path, pos);
    }
    pos++; // consume '('

    // Consume '@' followed by relative property path
    if (pos >= path.Length || path[pos] != '@')
    {
        throw new JsonPathException("Filter must start with '@'", path, pos);
    }
    pos++;

    var props = new List<string>();
    while (pos < path.Length && path[pos] == '.')
    {
        pos++;
        var s = pos;
        while (pos < path.Length && IsIdentifierChar(path[pos])) pos++;
        if (pos == s)
        {
            throw new JsonPathException("Expected property in filter", path, pos);
        }
        props.Add(path.Substring(s, pos - s));
    }

    SkipWhitespace(path, ref pos);

    // Detect operator. Only '==' supported.
    if (pos + 1 >= path.Length)
    {
        throw new JsonPathException("Unterminated filter", path, pos);
    }
    if (path[pos] == '!' && path[pos + 1] == '=')
    {
        throw new JsonPathNotSupportedException("filter operator '!='", path, pos);
    }
    if (path[pos] == '<' || path[pos] == '>')
    {
        throw new JsonPathNotSupportedException($"filter operator '{path[pos]}'", path, pos);
    }
    if (path[pos] == '=' && (pos + 1 >= path.Length || path[pos + 1] != '='))
    {
        throw new JsonPathNotSupportedException("single '='", path, pos);
    }
    if (path[pos] != '=' || path[pos + 1] != '=')
    {
        throw new JsonPathException("Expected '==' in filter", path, pos);
    }
    pos += 2;
    SkipWhitespace(path, ref pos);

    // Consume single-quoted string literal
    if (pos >= path.Length || path[pos] != '\'')
    {
        throw new JsonPathNotSupportedException("non-string-literal filter rhs", path, pos);
    }
    pos++;
    var litStart = pos;
    while (pos < path.Length && path[pos] != '\'') pos++;
    if (pos >= path.Length)
    {
        throw new JsonPathException("Unterminated string literal", path, pos);
    }
    var literal = path.Substring(litStart, pos - litStart);
    pos++; // consume closing quote

    SkipWhitespace(path, ref pos);

    // Reject logical operators
    if (pos < path.Length && (path[pos] == '&' || path[pos] == '|'))
    {
        throw new JsonPathNotSupportedException("filter logical operator", path, pos);
    }
    // Reject regex
    if (pos < path.Length && path[pos] == '=' && pos + 1 < path.Length && path[pos + 1] == '~')
    {
        throw new JsonPathNotSupportedException("filter regex '=~'", path, pos);
    }

    if (pos >= path.Length || path[pos] != ')')
    {
        throw new JsonPathException("Expected ')' to close filter", path, pos);
    }
    pos++; // consume ')'
    ExpectClosingBracket(path, ref pos);
    segments.Add(new FilterSegment(props, literal));
}
```

Add the helper:

```csharp
private static void SkipWhitespace(string path, ref int pos)
{
    while (pos < path.Length && (path[pos] == ' ' || path[pos] == '\t')) pos++;
}
```

Also: the regex test must run BEFORE the operator-rejection logic (for `=~`). Adjust order if needed — see test for the `=~` case; it's parsed at the operator position, not after the rhs. So in the operator-detection block, also check for `=~`:

```csharp
if (path[pos] == '=' && pos + 1 < path.Length && path[pos + 1] == '~')
{
    throw new JsonPathNotSupportedException("filter regex '=~'", path, pos);
}
```

(Place this before the `==` check.)

- [ ] **Step 4: Run — pass**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathParser.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathParserTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): parse equality filters"
```

### Task 1.7: TDD — Evaluator: root and plain navigation

**Files:**
- Create: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathEvaluatorTests.cs`
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathEvaluator.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using System.Text.Json;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline.JsonPath;

public class JsonPathEvaluatorTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Evaluate_Root_ReturnsTheRoot()
    {
        var doc = Parse("{\"a\":1}");
        var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$")).ToList();
        Assert.Single(matches);
        Assert.Equal(JsonValueKind.Object, matches[0].Element.ValueKind);
    }

    [Fact]
    public void Evaluate_Property_ReturnsScalar()
    {
        var doc = Parse("{\"a\": {\"b\": 42}}");
        var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.a.b")).ToList();
        Assert.Single(matches);
        Assert.Equal(42, matches[0].Element.GetInt32());
    }

    [Fact]
    public void Evaluate_MissingProperty_YieldsNoResults()
    {
        var doc = Parse("{\"a\": 1}");
        var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.missing")).ToList();
        Assert.Empty(matches);
    }

    [Fact]
    public void Evaluate_PropertyOnNonObject_YieldsNoResults()
    {
        var doc = Parse("[1, 2, 3]");
        var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.foo")).ToList();
        Assert.Empty(matches);
    }
}
```

- [ ] **Step 2: Run — fail**

- [ ] **Step 3: Implement minimal evaluator**

```csharp
// JsonPathEvaluator.cs
using System.Text.Json;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

public readonly record struct PathMatch(JsonElement Element, string CanonicalPath);

public static class JsonPathEvaluator
{
    public static IEnumerable<PathMatch> Evaluate(JsonElement root, JsonPathExpression expression)
    {
        IEnumerable<PathMatch> current = new[] { new PathMatch(root, "$") };

        foreach (var segment in expression.Segments)
        {
            current = segment switch
            {
                RootSegment => current, // already at root
                PropertySegment p => SelectProperty(current, p.Name),
                _ => throw new NotImplementedException($"Segment {segment.GetType().Name} not yet supported")
            };
        }

        return current;
    }

    private static IEnumerable<PathMatch> SelectProperty(IEnumerable<PathMatch> input, string name)
    {
        foreach (var match in input)
        {
            if (match.Element.ValueKind != JsonValueKind.Object) continue;
            if (match.Element.TryGetProperty(name, out var child))
            {
                yield return new PathMatch(child, match.CanonicalPath + "." + name);
            }
        }
    }
}
```

- [ ] **Step 4: Run — pass**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathEvaluator.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathEvaluatorTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): evaluator handles root and property navigation"
```

### Task 1.8: TDD — Evaluator: array index and wildcard

- [ ] **Step 1: Add failing tests**

```csharp
[Fact]
public void Evaluate_ArrayIndex_ReturnsElement()
{
    var doc = Parse("{\"arr\": [10, 20, 30]}");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.arr[1]")).ToList();
    Assert.Single(matches);
    Assert.Equal(20, matches[0].Element.GetInt32());
    Assert.Equal("$.arr[1]", matches[0].CanonicalPath);
}

[Fact]
public void Evaluate_ArrayIndexOutOfBounds_YieldsNoResults()
{
    var doc = Parse("{\"arr\": [10, 20]}");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.arr[5]")).ToList();
    Assert.Empty(matches);
}

[Fact]
public void Evaluate_Wildcard_ReturnsAllElements()
{
    var doc = Parse("{\"arr\": [10, 20, 30]}");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.arr[*]")).ToList();
    Assert.Equal(3, matches.Count);
    Assert.Equal(new[] { "$.arr[0]", "$.arr[1]", "$.arr[2]" }, matches.Select(m => m.CanonicalPath));
}

[Fact]
public void Evaluate_WildcardWithDescent_NavigatesAfter()
{
    var doc = Parse("{\"arr\": [{\"name\": \"a\"}, {\"name\": \"b\"}]}");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.arr[*].name")).ToList();
    Assert.Equal(2, matches.Count);
    Assert.Equal("a", matches[0].Element.GetString());
    Assert.Equal("b", matches[1].Element.GetString());
}

[Fact]
public void Evaluate_WildcardOnObject_ReturnsAllValues()
{
    // STJ: wildcard on object yields all property values, by Newtonsoft convention
    var doc = Parse("{\"obj\": {\"a\": 1, \"b\": 2}}");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.obj[*]")).ToList();
    Assert.Equal(2, matches.Count);
    Assert.Contains(matches, m => m.Element.GetInt32() == 1);
    Assert.Contains(matches, m => m.Element.GetInt32() == 2);
}
```

- [ ] **Step 2: Run — fail**

- [ ] **Step 3: Extend evaluator**

In `Evaluate`, add cases:

```csharp
IndexSegment i => SelectIndex(current, i.Index),
WildcardSegment => SelectWildcard(current),
```

And the helpers:

```csharp
private static IEnumerable<PathMatch> SelectIndex(IEnumerable<PathMatch> input, int index)
{
    foreach (var match in input)
    {
        if (match.Element.ValueKind != JsonValueKind.Array) continue;
        if (index < match.Element.GetArrayLength())
        {
            yield return new PathMatch(match.Element[index], match.CanonicalPath + "[" + index + "]");
        }
    }
}

private static IEnumerable<PathMatch> SelectWildcard(IEnumerable<PathMatch> input)
{
    foreach (var match in input)
    {
        if (match.Element.ValueKind == JsonValueKind.Array)
        {
            var idx = 0;
            foreach (var item in match.Element.EnumerateArray())
            {
                yield return new PathMatch(item, match.CanonicalPath + "[" + idx + "]");
                idx++;
            }
        }
        else if (match.Element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in match.Element.EnumerateObject())
            {
                yield return new PathMatch(prop.Value, match.CanonicalPath + "." + prop.Name);
            }
        }
    }
}
```

- [ ] **Step 4: Run — pass**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathEvaluator.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathEvaluatorTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): evaluator handles array index and wildcards"
```

### Task 1.9: TDD — Evaluator: recursive descent

- [ ] **Step 1: Add failing tests**

```csharp
[Fact]
public void Evaluate_RecursiveDescent_FindsAllMatchingProperties()
{
    var doc = Parse(@"{
        ""a"": { ""target"": 1, ""b"": { ""target"": 2 } },
        ""c"": [ { ""target"": 3 } ]
    }");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$..target")).ToList();
    var values = matches.Select(m => m.Element.GetInt32()).OrderBy(x => x).ToList();
    Assert.Equal(new[] { 1, 2, 3 }, values);
}

[Fact]
public void Evaluate_RecursiveDescentWildcard_VisitsAllNodes()
{
    var doc = Parse("{\"a\": [1, 2]}");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$..[*]")).ToList();
    // Visits: $.a (array), $.a[0]=1, $.a[1]=2 — exact set depends on impl,
    // but all leaf values must appear at least once
    Assert.Contains(matches, m => m.Element.ValueKind == JsonValueKind.Number && m.Element.GetInt32() == 1);
    Assert.Contains(matches, m => m.Element.ValueKind == JsonValueKind.Number && m.Element.GetInt32() == 2);
}
```

- [ ] **Step 2: Run — fail**

- [ ] **Step 3: Extend evaluator** — add a `RecursiveDescentSegment` case that yields the input plus every descendant:

```csharp
RecursiveDescentSegment => SelectRecursive(current),
```

```csharp
private static IEnumerable<PathMatch> SelectRecursive(IEnumerable<PathMatch> input)
{
    foreach (var match in input)
    {
        foreach (var d in DescendAll(match)) yield return d;
    }
}

private static IEnumerable<PathMatch> DescendAll(PathMatch match)
{
    yield return match;
    switch (match.Element.ValueKind)
    {
        case JsonValueKind.Object:
            foreach (var prop in match.Element.EnumerateObject())
            {
                var childPath = match.CanonicalPath + "." + prop.Name;
                foreach (var d in DescendAll(new PathMatch(prop.Value, childPath))) yield return d;
            }
            break;
        case JsonValueKind.Array:
            var idx = 0;
            foreach (var item in match.Element.EnumerateArray())
            {
                var childPath = match.CanonicalPath + "[" + idx + "]";
                foreach (var d in DescendAll(new PathMatch(item, childPath))) yield return d;
                idx++;
            }
            break;
    }
}
```

- [ ] **Step 4: Run — pass**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathEvaluator.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathEvaluatorTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): evaluator handles recursive descent"
```

### Task 1.10: TDD — Evaluator: equality filters

- [ ] **Step 1: Add failing tests**

```csharp
[Fact]
public void Evaluate_FilterEquality_SelectsMatching()
{
    var doc = Parse(@"{
        ""items"": [
            { ""Id"": ""abc"", ""Value"": 1 },
            { ""Id"": ""xyz"", ""Value"": 2 }
        ]
    }");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.items[?(@.Id == 'abc')]")).ToList();
    Assert.Single(matches);
    Assert.Equal(1, matches[0].Element.GetProperty("Value").GetInt32());
}

[Fact]
public void Evaluate_FilterEquality_NestedPath()
{
    var doc = Parse(@"{
        ""items"": [
            { ""attrs"": { ""code"": ""A"" } },
            { ""attrs"": { ""code"": ""B"" } }
        ]
    }");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$.items[?(@.attrs.code == 'B')]")).ToList();
    Assert.Single(matches);
}

[Fact]
public void Evaluate_FilterAfterRecursiveDescent_FindsMatches()
{
    // Production pattern: $..[?(@.Id=='X')].Value
    var doc = Parse(@"{
        ""bucket"": [
            { ""Id"": ""m1"", ""Value"": 100 },
            { ""children"": [ { ""Id"": ""m2"", ""Value"": 200 } ] }
        ]
    }");
    var matches = JsonPathEvaluator.Evaluate(doc, JsonPathParser.Parse("$..[?(@.Id == 'm2')].Value")).ToList();
    Assert.Single(matches);
    Assert.Equal(200, matches[0].Element.GetInt32());
}
```

- [ ] **Step 2: Run — fail**

- [ ] **Step 3: Extend evaluator**

```csharp
FilterSegment f => SelectFilter(current, f),
```

```csharp
private static IEnumerable<PathMatch> SelectFilter(IEnumerable<PathMatch> input, FilterSegment filter)
{
    foreach (var match in input)
    {
        if (match.Element.ValueKind == JsonValueKind.Array)
        {
            var idx = 0;
            foreach (var item in match.Element.EnumerateArray())
            {
                if (FilterMatches(item, filter))
                {
                    yield return new PathMatch(item, match.CanonicalPath + "[" + idx + "]");
                }
                idx++;
            }
        }
        else if (match.Element.ValueKind == JsonValueKind.Object)
        {
            // Filter on an object: yield the object itself if it matches.
            // (Newtonsoft is consistent with this for `$..[?(...)]` — recursive descent
            // visits each node and the filter selects matches.)
            if (FilterMatches(match.Element, filter))
            {
                yield return match;
            }
        }
    }
}

private static bool FilterMatches(JsonElement candidate, FilterSegment filter)
{
    var node = candidate;
    foreach (var prop in filter.RelativeProperty)
    {
        if (node.ValueKind != JsonValueKind.Object) return false;
        if (!node.TryGetProperty(prop, out node)) return false;
    }
    return node.ValueKind == JsonValueKind.String && node.GetString() == filter.Literal;
}
```

- [ ] **Step 4: Run — pass**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonPathEvaluator.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonPathEvaluatorTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): evaluator handles equality filters"
```

### Task 1.11: Add CanonicalPath helpers

**Files:**
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/CanonicalPath.cs`
- Create: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/CanonicalPathTests.cs`

- [ ] **Step 1: Tests**

```csharp
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline.JsonPath;

public class CanonicalPathTests
{
    [Fact]
    public void IsAncestor_RootIsAncestorOfAll()
    {
        Assert.True(CanonicalPath.IsAncestor("$", "$.a.b"));
        Assert.True(CanonicalPath.IsAncestor("$", "$"));
    }

    [Fact]
    public void IsAncestor_PropertyPathSegmentBoundary()
    {
        Assert.True(CanonicalPath.IsAncestor("$.a", "$.a.b"));
        Assert.True(CanonicalPath.IsAncestor("$.a", "$.a[0]"));
        Assert.False(CanonicalPath.IsAncestor("$.a", "$.ab"));
    }

    [Fact]
    public void GetSegments_SplitsCleanly()
    {
        var segments = CanonicalPath.GetSegments("$.a[0].b");
        Assert.Equal(new[] { ".a", "[0]", ".b" }, segments);
    }

    [Fact]
    public void GetParent_ReturnsImmediateParent()
    {
        Assert.Equal("$.a", CanonicalPath.GetParent("$.a.b"));
        Assert.Equal("$.a", CanonicalPath.GetParent("$.a[0]"));
        Assert.Equal("$", CanonicalPath.GetParent("$.a"));
        Assert.Null(CanonicalPath.GetParent("$"));
    }
}
```

- [ ] **Step 2: Implement**

```csharp
namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

internal static class CanonicalPath
{
    public static bool IsAncestor(string ancestor, string descendant)
    {
        if (ancestor == "$") return true;
        if (!descendant.StartsWith(ancestor, StringComparison.Ordinal)) return false;
        if (descendant.Length == ancestor.Length) return true;
        var next = descendant[ancestor.Length];
        return next == '.' || next == '[';
    }

    public static IReadOnlyList<string> GetSegments(string path)
    {
        if (path == "$") return Array.Empty<string>();
        var segments = new List<string>();
        var i = 1; // skip '$'
        while (i < path.Length)
        {
            var start = i;
            if (path[i] == '.')
            {
                i++;
                while (i < path.Length && path[i] != '.' && path[i] != '[') i++;
                segments.Add(path.Substring(start, i - start));
            }
            else if (path[i] == '[')
            {
                while (i < path.Length && path[i] != ']') i++;
                if (i < path.Length) i++; // consume ']'
                segments.Add(path.Substring(start, i - start));
            }
            else
            {
                throw new ArgumentException($"Malformed canonical path: '{path}'");
            }
        }
        return segments;
    }

    public static string? GetParent(string path)
    {
        if (path == "$") return null;
        var segments = GetSegments(path);
        if (segments.Count == 0) return null;
        var parent = "$" + string.Concat(segments.Take(segments.Count - 1));
        return parent;
    }
}
```

- [ ] **Step 3: Run, pass, commit**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~CanonicalPathTests" -c DebugL
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/CanonicalPath.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/CanonicalPathTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(jsonpath): add CanonicalPath helpers"
```

---

## Phase 2 — Layered IDataContext Foundation

### Task 2.1: Define new public API surface (interface, enum, options)

**Files:**
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext/DataKind.cs`
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext/PipelineJsonOptions.cs`
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext/IDataContext.cs` (will REPLACE the existing one in Task 2.X — for now, place under DataContext/ subfolder)

- [ ] **Step 1: DataKind enum**

```csharp
namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

public enum DataKind
{
    Undefined,
    Null,
    Object,
    Array,
    String,
    Number,
    Boolean
}
```

- [ ] **Step 2: PipelineJsonOptions**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

public static class PipelineJsonOptions
{
    public static readonly JsonSerializerOptions Default = CreateDefault();

    public static JsonSerializerOptions CreateDefault() => new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter() }
    };
}
```

- [ ] **Step 3: New IDataContext (place at `EtlDataPipeline/IDataContext.cs`, deleting the existing file)**

First delete the existing file:

```bash
rm /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/EtlDataPipeline/IDataContext.cs
```

Then create the new one at the same path:

```csharp
// EtlDataPipeline/IDataContext.cs
using System.Text.Json;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

public interface IDataContext
{
    IDataContext? Parent { get; }

    bool Exists(string path);
    DataKind GetKind(string path);
    int Length(string path);
    IEnumerable<string> Keys(string path);

    T? Get<T>(string path, JsonSerializerOptions? options = null);
    IEnumerable<T?>? GetArray<T>(string path);

    void Set<T>(string path, T? value);

    void Set<T>(string path,
        T? value,
        DocumentModes documentMode,
        ValueKinds valueKind,
        TargetValueWriteModes writeMode,
        JsonSerializerOptions? options = null);

    void Clear(string path);

    Task IterateArrayAsync(string path, Func<IDataContext, Task> body);
    Task IterateObjectAsync(string path, Func<string, IDataContext, Task> body);
    Task IterateMatchesAsync(string jsonPath, Func<IDataContext, Task> body);

    void CopyTo(string sourcePath, string targetPath);

    void WriteJsonTo(string path, Stream destination);
    void SetFromJson(string path, ReadOnlyMemory<byte> utf8Json);
}
```

- [ ] **Step 4: Verify it compiles in isolation**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/Sdk.Common.csproj -c DebugL
```

Expected: many compilation errors in `DataContext.cs`, the `Nodes/` folder, `JTokenExtensions.cs`, etc. — that's intended. The contract is in place; concrete impls follow.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(pipeline): introduce path-only IDataContext, DataKind, PipelineJsonOptions

WIP: callers in Nodes/ and DataContext.cs will not compile until subsequent
tasks land the new DataContext implementation and migrate node code."
```

### Task 2.2: TDD — DataOverlay (subtree-rooted copy-on-write)

**Files:**
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext/DataOverlay.cs`
- Create: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/DataContext/DataOverlayTests.cs`

The overlay is the heart of the layered model. It must satisfy the read–merge invariant in spec §5.1.

- [ ] **Step 1: Write parity-invariant tests**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline.DataContext;

public class DataOverlayTests
{
    private static JsonElement Base(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Read_NoOverlay_ReturnsBaseValue()
    {
        var overlay = new DataOverlay(Base("{\"a\": 1}"));
        Assert.True(overlay.TryRead("$.a", out var node));
        Assert.Equal(1, node!.GetValue<int>());
    }

    [Fact]
    public void Write_ThenRead_ReturnsWrittenValue()
    {
        var overlay = new DataOverlay(Base("{\"a\": 1}"));
        overlay.Write("$.a", JsonValue.Create(42));
        Assert.True(overlay.TryRead("$.a", out var node));
        Assert.Equal(42, node!.GetValue<int>());
    }

    [Fact]
    public void Write_DeepDescendant_AncestorReadObservesIt()
    {
        // Spec §5.1 invariant.
        var overlay = new DataOverlay(Base("{\"a\": {\"b\": 1, \"c\": 2}}"));
        overlay.Write("$.a.b", JsonValue.Create(99));

        Assert.True(overlay.TryRead("$.a.b", out var b));
        Assert.Equal(99, b!.GetValue<int>());

        Assert.True(overlay.TryRead("$.a", out var a));
        var aObj = a!.AsObject();
        Assert.Equal(99, aObj["b"]!.GetValue<int>());
        Assert.Equal(2, aObj["c"]!.GetValue<int>());
    }

    [Fact]
    public void Write_ToRoot_RootReadReflectsIt()
    {
        var overlay = new DataOverlay(Base("{\"a\": 1}"));
        overlay.Write("$", JsonNode.Parse("{\"x\": 99}"));
        Assert.True(overlay.TryRead("$", out var node));
        Assert.Equal(99, node!.AsObject()["x"]!.GetValue<int>());
    }

    [Fact]
    public void Write_DisjointSubtrees_BothObservableFromRoot()
    {
        var overlay = new DataOverlay(Base("{\"a\": {\"b\": 1}, \"c\": {\"d\": 2}}"));
        overlay.Write("$.a.b", JsonValue.Create(11));
        overlay.Write("$.c.d", JsonValue.Create(22));

        Assert.True(overlay.TryRead("$", out var root));
        var rootObj = root!.AsObject();
        Assert.Equal(11, rootObj["a"]!.AsObject()["b"]!.GetValue<int>());
        Assert.Equal(22, rootObj["c"]!.AsObject()["d"]!.GetValue<int>());
    }

    [Fact]
    public void Read_UnrelatedPath_StillHitsBase()
    {
        var overlay = new DataOverlay(Base("{\"a\": {\"b\": 1}, \"c\": 2}"));
        overlay.Write("$.a.b", JsonValue.Create(99));

        Assert.True(overlay.TryRead("$.c", out var c));
        Assert.Equal(2, c!.GetValue<int>());
    }

    [Fact]
    public void Write_OverlappingThenRead_LatestWins()
    {
        var overlay = new DataOverlay(Base("{\"a\": {\"b\": 1}}"));
        overlay.Write("$.a.b", JsonValue.Create(11));
        overlay.Write("$.a.b", JsonValue.Create(22));

        Assert.True(overlay.TryRead("$.a.b", out var b));
        Assert.Equal(22, b!.GetValue<int>());
    }

    [Fact]
    public void Write_AddsNewProperty_VisibleFromAncestor()
    {
        var overlay = new DataOverlay(Base("{\"a\": {\"b\": 1}}"));
        overlay.Write("$.a.newProp", JsonValue.Create("hello"));

        Assert.True(overlay.TryRead("$.a", out var a));
        var aObj = a!.AsObject();
        Assert.Equal(1, aObj["b"]!.GetValue<int>());
        Assert.Equal("hello", aObj["newProp"]!.GetValue<string>());
    }
}
```

- [ ] **Step 2: Run — fail**

- [ ] **Step 3: Implement DataOverlay (lift-on-first-write strategy)**

The simplest correct strategy:

- The overlay tracks at most one **lifted root** (`JsonNode? _lifted`).
- The first write at *any* path materializes the entire base into `_lifted` as a `JsonNode` tree.
- All subsequent reads/writes go through `_lifted`.
- If `_lifted` is null, reads fall back to the base `JsonElement`.

This trades zero-copy on first write for correctness simplicity. The zero-copy savings come from iteration child contexts NOT inheriting their parent's lifted state — they get fresh empty overlays, so as long as they don't trigger their own first write, they read the parent's lifted state via context fallback (next phase).

For child contexts that DO write (`$.MergePath`, etc.), the per-child overlay materializes only the child's local data, which is small.

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

internal sealed class DataOverlay
{
    private readonly JsonElement _base;
    private JsonNode? _lifted; // when non-null, all reads/writes route here

    public DataOverlay(JsonElement baseElement)
    {
        _base = baseElement;
    }

    public bool HasWrites => _lifted is not null;

    public bool TryRead(string canonicalPath, out JsonNode? value)
    {
        if (_lifted is not null)
        {
            value = NavigateLifted(_lifted, canonicalPath, allowMissing: true);
            return value is not null || PathExistsLifted(_lifted, canonicalPath);
        }

        // No writes yet — walk the base.
        var matches = JsonPathEvaluator.Evaluate(_base, JsonPathParser.Parse(canonicalPath)).ToList();
        if (matches.Count == 0)
        {
            value = null;
            return false;
        }
        // Materialize the JsonElement match as a JsonNode (copy at read time).
        value = JsonNode.Parse(matches[0].Element.GetRawText());
        return true;
    }

    public void Write(string canonicalPath, JsonNode? value)
    {
        EnsureLifted();
        if (canonicalPath == "$")
        {
            _lifted = value;
            return;
        }

        var segments = CanonicalPath.GetSegments(canonicalPath);
        SetByPath(_lifted!, segments, value);
    }

    public void Clear(string canonicalPath)
    {
        if (canonicalPath == "$")
        {
            _lifted = null;
            return;
        }
        if (_lifted is null) return;
        var segments = CanonicalPath.GetSegments(canonicalPath);
        ClearByPath(_lifted, segments);
    }

    private void EnsureLifted()
    {
        if (_lifted is null)
        {
            _lifted = JsonNode.Parse(_base.GetRawText());
        }
    }

    // --- Navigation helpers below are TDD-developed in subsequent tasks ---
    private static JsonNode? NavigateLifted(JsonNode root, string canonicalPath, bool allowMissing)
    {
        if (canonicalPath == "$") return root;
        var segments = CanonicalPath.GetSegments(canonicalPath);
        JsonNode? current = root;
        foreach (var seg in segments)
        {
            current = StepInto(current, seg);
            if (current is null) return allowMissing ? null : throw new InvalidOperationException($"Path not found: {canonicalPath}");
        }
        return current;
    }

    private static bool PathExistsLifted(JsonNode root, string canonicalPath) =>
        NavigateLifted(root, canonicalPath, allowMissing: true) is not null;

    private static JsonNode? StepInto(JsonNode? current, string segment)
    {
        if (current is null) return null;
        if (segment.StartsWith("."))
        {
            var name = segment.Substring(1);
            if (current is JsonObject obj && obj.TryGetPropertyValue(name, out var child)) return child;
            return null;
        }
        if (segment.StartsWith("[") && segment.EndsWith("]"))
        {
            var idxStr = segment.Substring(1, segment.Length - 2);
            if (int.TryParse(idxStr, out var idx) && current is JsonArray arr && idx >= 0 && idx < arr.Count)
            {
                return arr[idx];
            }
            return null;
        }
        throw new ArgumentException($"Unknown segment: {segment}");
    }

    private static void SetByPath(JsonNode root, IReadOnlyList<string> segments, JsonNode? value)
    {
        JsonNode current = root;
        for (var i = 0; i < segments.Count - 1; i++)
        {
            current = NavigateOrCreate(current, segments[i]);
        }
        SetSegment(current, segments[segments.Count - 1], value);
    }

    private static JsonNode NavigateOrCreate(JsonNode current, string segment)
    {
        if (segment.StartsWith("."))
        {
            var name = segment.Substring(1);
            if (current is JsonObject obj)
            {
                if (!obj.TryGetPropertyValue(name, out var child) || child is null)
                {
                    child = new JsonObject();
                    obj[name] = child;
                }
                return child;
            }
            throw new InvalidOperationException($"Cannot navigate '{segment}' on non-object");
        }
        if (segment.StartsWith("["))
        {
            var idx = int.Parse(segment.Substring(1, segment.Length - 2));
            if (current is JsonArray arr)
            {
                while (arr.Count <= idx) arr.Add(null);
                if (arr[idx] is null) arr[idx] = new JsonObject();
                return arr[idx]!;
            }
            throw new InvalidOperationException($"Cannot navigate '{segment}' on non-array");
        }
        throw new ArgumentException($"Unknown segment: {segment}");
    }

    private static void SetSegment(JsonNode current, string segment, JsonNode? value)
    {
        if (segment.StartsWith("."))
        {
            var name = segment.Substring(1);
            if (current is JsonObject obj) { obj[name] = value; return; }
            throw new InvalidOperationException($"Cannot set '{segment}' on non-object");
        }
        if (segment.StartsWith("["))
        {
            var idx = int.Parse(segment.Substring(1, segment.Length - 2));
            if (current is JsonArray arr)
            {
                while (arr.Count <= idx) arr.Add(null);
                arr[idx] = value;
                return;
            }
            throw new InvalidOperationException($"Cannot set '{segment}' on non-array");
        }
        throw new ArgumentException($"Unknown segment: {segment}");
    }

    private static void ClearByPath(JsonNode root, IReadOnlyList<string> segments)
    {
        JsonNode? current = root;
        for (var i = 0; i < segments.Count - 1; i++)
        {
            current = StepInto(current, segments[i]);
            if (current is null) return;
        }
        var last = segments[segments.Count - 1];
        if (last.StartsWith(".") && current is JsonObject obj) obj.Remove(last.Substring(1));
        else if (last.StartsWith("[") && current is JsonArray arr)
        {
            var idx = int.Parse(last.Substring(1, last.Length - 2));
            if (idx >= 0 && idx < arr.Count) arr.RemoveAt(idx);
        }
    }
}
```

- [ ] **Step 4: Run — pass**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~DataOverlayTests" -c DebugL
```

Expected: PASS, 8 tests.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/DataContext/DataOverlay.cs tests/Sdk.Common.Tests/EtlDataPipeline/DataContext/DataOverlayTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(datacontext): subtree-rooted overlay with lift-on-first-write"
```

### Task 2.3: TDD — DataContext (single-context, no parent chain yet)

**Files:**
- Create: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext.cs` (replacing the deleted file at this exact path)
- Create: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/DataContext/DataContextTests.cs`

Note: `EtlDataPipeline/DataContext.cs` was deleted in Task 2.1. We restore it here as the new layered impl.

- [ ] **Step 1: Write tests for the public API methods individually**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline.DataContext;

public class DataContextTests
{
    private static JsonElement Doc(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Get_ReadsFromBase()
    {
        var ctx = new DataContextImpl(Doc("{\"a\": 42}"));
        Assert.Equal(42, ctx.Get<int>("$.a"));
    }

    [Fact]
    public void Set_OverlayWins()
    {
        var ctx = new DataContextImpl(Doc("{\"a\": 1}"));
        ctx.Set("$.a", 99);
        Assert.Equal(99, ctx.Get<int>("$.a"));
    }

    [Fact]
    public void Exists_ReturnsTrueForBaseAndOverlay()
    {
        var ctx = new DataContextImpl(Doc("{\"a\": 1}"));
        Assert.True(ctx.Exists("$.a"));
        Assert.False(ctx.Exists("$.missing"));
        ctx.Set("$.added", "x");
        Assert.True(ctx.Exists("$.added"));
    }

    [Fact]
    public void GetKind_ReturnsCorrectKindFromBase()
    {
        var ctx = new DataContextImpl(Doc("{\"o\": {}, \"a\": [], \"s\": \"hi\", \"n\": 1, \"b\": true, \"x\": null}"));
        Assert.Equal(DataKind.Object, ctx.GetKind("$.o"));
        Assert.Equal(DataKind.Array, ctx.GetKind("$.a"));
        Assert.Equal(DataKind.String, ctx.GetKind("$.s"));
        Assert.Equal(DataKind.Number, ctx.GetKind("$.n"));
        Assert.Equal(DataKind.Boolean, ctx.GetKind("$.b"));
        Assert.Equal(DataKind.Null, ctx.GetKind("$.x"));
        Assert.Equal(DataKind.Undefined, ctx.GetKind("$.missing"));
    }

    [Fact]
    public void Length_OnArrayAndString_AndObject()
    {
        var ctx = new DataContextImpl(Doc("{\"a\": [1,2,3], \"s\": \"hello\", \"o\": {\"a\":1,\"b\":2}}"));
        Assert.Equal(3, ctx.Length("$.a"));
        Assert.Equal(5, ctx.Length("$.s"));
        Assert.Equal(2, ctx.Length("$.o"));
    }

    [Fact]
    public void Keys_ReturnsObjectKeys()
    {
        var ctx = new DataContextImpl(Doc("{\"a\": 1, \"b\": 2}"));
        Assert.Equal(new[] { "a", "b" }, ctx.Keys("$").OrderBy(x => x));
    }

    [Fact]
    public async Task IterateArrayAsync_ProvidesChildContextPerItem()
    {
        var ctx = new DataContextImpl(Doc("{\"items\": [10, 20, 30]}"));
        var collected = new List<int>();
        await ctx.IterateArrayAsync("$.items", child =>
        {
            collected.Add(child.Get<int>("$"));
            return Task.CompletedTask;
        });
        Assert.Equal(new[] { 10, 20, 30 }, collected);
    }

    [Fact]
    public async Task IterateMatchesAsync_UsesEvaluator()
    {
        var ctx = new DataContextImpl(Doc(@"{""items"":[{""Id"":""a"",""V"":1},{""Id"":""b"",""V"":2}]}"));
        var collected = new List<int>();
        await ctx.IterateMatchesAsync("$.items[?(@.Id == 'b')]", child =>
        {
            collected.Add(child.Get<int>("$.V"));
            return Task.CompletedTask;
        });
        Assert.Equal(new[] { 2 }, collected);
    }

    [Fact]
    public void GetArray_ReturnsTypedSequence()
    {
        var ctx = new DataContextImpl(Doc("{\"nums\": [1, 2, 3]}"));
        Assert.Equal(new[] { 1, 2, 3 }, ctx.GetArray<int>("$.nums"));
    }
}
```

- [ ] **Step 2: Run — fail (DataContextImpl doesn't exist)**

- [ ] **Step 3: Implement**

```csharp
// EtlDataPipeline/DataContext.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

public sealed class DataContextImpl : IDataContext
{
    private readonly DataOverlay _overlay;
    private readonly JsonElement _base;
    private readonly JsonDocument? _ownedDocument;

    public DataContextImpl(JsonElement baseElement)
    {
        _base = baseElement;
        _overlay = new DataOverlay(baseElement);
    }

    public DataContextImpl(JsonDocument document) : this(document.RootElement)
    {
        _ownedDocument = document;
    }

    public DataContextImpl() : this(JsonDocument.Parse("{}")) { }

    public IDataContext? Parent => null;

    public bool Exists(string path)
    {
        if (_overlay.TryRead(path, out var node)) return node is not null;
        var matches = JsonPathEvaluator.Evaluate(_base, JsonPathParser.Parse(path));
        return matches.Any();
    }

    public DataKind GetKind(string path)
    {
        if (_overlay.HasWrites)
        {
            if (!_overlay.TryRead(path, out var node) || node is null) return DataKind.Undefined;
            return node switch
            {
                JsonObject => DataKind.Object,
                JsonArray => DataKind.Array,
                JsonValue v => v.GetValueKind() switch
                {
                    JsonValueKind.String => DataKind.String,
                    JsonValueKind.Number => DataKind.Number,
                    JsonValueKind.True => DataKind.Boolean,
                    JsonValueKind.False => DataKind.Boolean,
                    JsonValueKind.Null => DataKind.Null,
                    _ => DataKind.Undefined
                },
                _ => DataKind.Undefined
            };
        }

        var match = JsonPathEvaluator.Evaluate(_base, JsonPathParser.Parse(path)).FirstOrDefault();
        if (match.CanonicalPath is null) return DataKind.Undefined;
        return match.Element.ValueKind switch
        {
            JsonValueKind.Object => DataKind.Object,
            JsonValueKind.Array => DataKind.Array,
            JsonValueKind.String => DataKind.String,
            JsonValueKind.Number => DataKind.Number,
            JsonValueKind.True => DataKind.Boolean,
            JsonValueKind.False => DataKind.Boolean,
            JsonValueKind.Null => DataKind.Null,
            _ => DataKind.Undefined
        };
    }

    public int Length(string path)
    {
        return GetKind(path) switch
        {
            DataKind.Array => GetAsNode(path)?.AsArray().Count ?? 0,
            DataKind.Object => GetAsNode(path)?.AsObject().Count ?? 0,
            DataKind.String => GetAsNode(path)?.GetValue<string>().Length ?? 0,
            _ => throw new InvalidOperationException($"Length not defined for kind at '{path}'")
        };
    }

    public IEnumerable<string> Keys(string path)
    {
        var node = GetAsNode(path);
        if (node is JsonObject obj) return obj.Select(p => p.Key);
        return Array.Empty<string>();
    }

    public T? Get<T>(string path, JsonSerializerOptions? options = null)
    {
        options ??= PipelineJsonOptions.Default;
        var node = GetAsNode(path);
        if (node is null) return default;
        return node.Deserialize<T>(options);
    }

    public IEnumerable<T?>? GetArray<T>(string path)
    {
        var node = GetAsNode(path);
        if (node is null) return null;
        if (node is JsonArray arr) return arr.Select(item => item is null ? default : item.Deserialize<T>(PipelineJsonOptions.Default));
        if (node is JsonValue val) return new[] { val.Deserialize<T>(PipelineJsonOptions.Default) };
        return null;
    }

    public void Set<T>(string path, T? value) =>
        Set(path, value, DocumentModes.Extend, ValueKinds.Simple, TargetValueWriteModes.Overwrite);

    public void Set<T>(string path,
        T? value,
        DocumentModes documentMode,
        ValueKinds valueKind,
        TargetValueWriteModes writeMode,
        JsonSerializerOptions? options = null)
    {
        options ??= PipelineJsonOptions.Default;

        if (documentMode == DocumentModes.Replace)
        {
            _overlay.Write("$", new JsonObject());
        }

        var node = value is null
            ? null
            : (value is JsonNode jn ? jn.DeepClone() : JsonSerializer.SerializeToNode(value, options));

        if (path == "$" || string.IsNullOrEmpty(path))
        {
            _overlay.Write("$", node);
            return;
        }

        switch (writeMode)
        {
            case TargetValueWriteModes.Overwrite:
                _overlay.Write(path, valueKind == ValueKinds.Array ? new JsonArray(node) : node);
                break;
            case TargetValueWriteModes.Append:
                AppendOrPrepend(path, node, prepend: false);
                break;
            case TargetValueWriteModes.Prepend:
                AppendOrPrepend(path, node, prepend: true);
                break;
            case TargetValueWriteModes.Merge:
                MergeAt(path, node);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(writeMode));
        }
    }

    private void AppendOrPrepend(string path, JsonNode? node, bool prepend)
    {
        var existing = GetAsNode(path);
        if (existing is JsonArray arr)
        {
            var clone = arr.DeepClone()!.AsArray();
            if (node is JsonArray nodeArr)
            {
                foreach (var item in nodeArr.ToList())
                {
                    item.Parent?.AsObject(); // ensure detachable
                    if (prepend) clone.Insert(0, item.DeepClone()); else clone.Add(item.DeepClone());
                }
            }
            else
            {
                if (prepend) clone.Insert(0, node); else clone.Add(node);
            }
            _overlay.Write(path, clone);
        }
        else if (existing is null)
        {
            var newArr = new JsonArray();
            if (node is not null) newArr.Add(node);
            _overlay.Write(path, newArr);
        }
        else
        {
            throw DataPipelineException.ValueIsArrayMustBeScalarForWriteMode(path,
                prepend ? TargetValueWriteModes.Prepend : TargetValueWriteModes.Append);
        }
    }

    private void MergeAt(string path, JsonNode? node)
    {
        var existing = GetAsNode(path);
        if (existing is JsonObject existingObj && node is JsonObject newObj)
        {
            var merged = (JsonObject)existingObj.DeepClone();
            foreach (var (k, v) in newObj)
            {
                merged[k] = v?.DeepClone();
            }
            _overlay.Write(path, merged);
            return;
        }
        throw DataPipelineException.SourceValueIsObjectMustBeObjectForWriteMode(path, TargetValueWriteModes.Merge);
    }

    public void Clear(string path) => _overlay.Clear(path);

    public Task IterateArrayAsync(string path, Func<IDataContext, Task> body)
    {
        // Single-context implementation for now; child-context support comes in Task 2.4.
        var node = GetAsNode(path);
        if (node is not JsonArray arr) return Task.CompletedTask;
        return RunSequential(arr, body);
    }

    private static async Task RunSequential(JsonArray arr, Func<IDataContext, Task> body)
    {
        foreach (var item in arr)
        {
            using var doc = JsonDocument.Parse(item?.ToJsonString() ?? "null");
            var child = new DataContextImpl(doc.RootElement);
            await body(child);
        }
    }

    public async Task IterateObjectAsync(string path, Func<string, IDataContext, Task> body)
    {
        var node = GetAsNode(path);
        if (node is not JsonObject obj) return;
        foreach (var (key, value) in obj)
        {
            using var doc = JsonDocument.Parse(value?.ToJsonString() ?? "null");
            var child = new DataContextImpl(doc.RootElement);
            await body(key, child);
        }
    }

    public async Task IterateMatchesAsync(string jsonPath, Func<IDataContext, Task> body)
    {
        var expr = JsonPathParser.Parse(jsonPath);
        // For simplicity, walk the materialized current state.
        using var snapshot = JsonDocument.Parse(GetAsNode("$")?.ToJsonString() ?? _base.GetRawText());
        foreach (var match in JsonPathEvaluator.Evaluate(snapshot.RootElement, expr))
        {
            using var doc = JsonDocument.Parse(match.Element.GetRawText());
            var child = new DataContextImpl(doc.RootElement);
            await body(child);
        }
    }

    public void CopyTo(string sourcePath, string targetPath)
    {
        var src = GetAsNode(sourcePath);
        if (src is not null) _overlay.Write(targetPath, src.DeepClone());
    }

    public void WriteJsonTo(string path, Stream destination)
    {
        var node = GetAsNode(path);
        if (node is null) return;
        using var writer = new Utf8JsonWriter(destination);
        node.WriteTo(writer);
    }

    public void SetFromJson(string path, ReadOnlyMemory<byte> utf8Json)
    {
        var node = JsonNode.Parse(utf8Json.Span);
        _overlay.Write(path, node);
    }

    private JsonNode? GetAsNode(string path)
    {
        return _overlay.TryRead(path, out var node) ? node : null;
    }
}
```

- [ ] **Step 4: Run, pass**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~DataContextTests" -c DebugL
```

Expected: 9 tests pass. NOTE: this will also surface compilation errors in unrelated nodes and tests that reference the old API. Those are expected and addressed in later phases.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/DataContext.cs tests/Sdk.Common.Tests/EtlDataPipeline/DataContext/DataContextTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(datacontext): layered DataContextImpl with path-only API"
```

### Task 2.4: TDD — Child contexts with parent fallback (zero-copy iteration)

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext.cs`
- Add tests: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/DataContext/ChildContextTests.cs`

- [ ] **Step 1: Write tests for parent-fallback semantics**

```csharp
using System.Text.Json;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline.DataContext;

public class ChildContextTests
{
    private static JsonElement Doc(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Child_Reads_FallBackToParent()
    {
        var parent = new DataContextImpl(Doc("{\"shared\": 42, \"more\": [1,2]}"));
        var child = parent.CreateIterationChild(new[] { ("$.key", JsonDocument.Parse("99").RootElement) });
        Assert.Equal(42, child.Get<int>("$.shared"));
        Assert.Equal(99, child.Get<int>("$.key"));
    }

    [Fact]
    public void Child_Writes_DoNotEscapeToParent()
    {
        var parent = new DataContextImpl(Doc("{\"x\": 1}"));
        var child = parent.CreateIterationChild(System.Array.Empty<(string, JsonElement)>());
        child.Set("$.x", 99);
        Assert.Equal(99, child.Get<int>("$.x"));
        Assert.Equal(1, parent.Get<int>("$.x")); // parent unchanged
    }

    [Fact]
    public void Child_Aliases_AreReadable()
    {
        var parent = new DataContextImpl(Doc("{}"));
        var item = JsonDocument.Parse("{\"id\":\"a\"}").RootElement;
        var full = JsonDocument.Parse("{\"big\":\"data\"}").RootElement;
        var child = parent.CreateIterationChild(new[] { ("$.key", item), ("$.full", full) });
        Assert.Equal("a", child.Get<string>("$.key.id"));
        Assert.Equal("data", child.Get<string>("$.full.big"));
    }
}
```

- [ ] **Step 2: Run — fail (CreateIterationChild doesn't exist)**

- [ ] **Step 3: Add child-context support to DataContextImpl**

Add an internal child class and a factory method:

```csharp
// Inside DataContextImpl:

internal IDataContext CreateIterationChild(IReadOnlyList<(string AliasPath, JsonElement Value)> aliases)
{
    return new DataContextChild(this, aliases);
}

private sealed class DataContextChild : IDataContext
{
    private readonly DataContextImpl _parent;
    private readonly Dictionary<string, JsonElement> _aliases;
    private readonly DataOverlay _overlay;

    public DataContextChild(DataContextImpl parent, IReadOnlyList<(string AliasPath, JsonElement Value)> aliases)
    {
        _parent = parent;
        _aliases = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (k, v) in aliases) _aliases[k] = v;

        // Child's read base is empty by default; aliases provide the synthetic top-level entries.
        // The overlay starts empty too. Reads fall back through aliases → parent.
        _overlay = new DataOverlay(JsonDocument.Parse("{}").RootElement);
    }

    public IDataContext? Parent => _parent;

    public bool Exists(string path)
    {
        if (_overlay.TryRead(path, out var node) && node is not null) return true;
        if (TryReadAlias(path, out _)) return true;
        return _parent.Exists(path);
    }

    public DataKind GetKind(string path)
    {
        if (_overlay.HasWrites && _overlay.TryRead(path, out var n) && n is not null) return KindOfNode(n);
        if (TryReadAlias(path, out var aliasElem)) return KindOf(aliasElem);
        return _parent.GetKind(path);
    }

    public int Length(string path) => /* mirror DataContextImpl.Length, using GetAsNode */ throw new NotImplementedException();
    public IEnumerable<string> Keys(string path) => /* mirror */ throw new NotImplementedException();
    public T? Get<T>(string path, JsonSerializerOptions? options = null) =>
        GetAsNode(path) is { } n ? n.Deserialize<T>(options ?? PipelineJsonOptions.Default) : default;
    public IEnumerable<T?>? GetArray<T>(string path) => /* mirror */ throw new NotImplementedException();
    public void Set<T>(string path, T? value) => Set(path, value, DocumentModes.Extend, ValueKinds.Simple, TargetValueWriteModes.Overwrite);
    public void Set<T>(string path, T? value, DocumentModes documentMode, ValueKinds valueKind, TargetValueWriteModes writeMode, JsonSerializerOptions? options = null)
    {
        // Reuse parent's Set logic via a small inline implementation that writes to _this_ overlay.
        options ??= PipelineJsonOptions.Default;
        var node = value is null ? null : (value is JsonNode jn ? jn.DeepClone() : JsonSerializer.SerializeToNode(value, options));
        if (path == "$" || string.IsNullOrEmpty(path)) { _overlay.Write("$", node); return; }
        if (writeMode == TargetValueWriteModes.Overwrite) _overlay.Write(path, valueKind == ValueKinds.Array ? new JsonArray(node) : node);
        else throw new NotImplementedException("Append/Prepend/Merge in child context: implement using GetAsNode + DataOverlay.Write");
    }

    public void Clear(string path) => _overlay.Clear(path);

    public Task IterateArrayAsync(string path, Func<IDataContext, Task> body) => /* mirror */ throw new NotImplementedException();
    public Task IterateObjectAsync(string path, Func<string, IDataContext, Task> body) => /* mirror */ throw new NotImplementedException();
    public Task IterateMatchesAsync(string jsonPath, Func<IDataContext, Task> body) => /* mirror */ throw new NotImplementedException();
    public void CopyTo(string sourcePath, string targetPath) => /* mirror */ throw new NotImplementedException();
    public void WriteJsonTo(string path, Stream destination) => /* mirror */ throw new NotImplementedException();
    public void SetFromJson(string path, ReadOnlyMemory<byte> utf8Json) => _overlay.Write(path, JsonNode.Parse(utf8Json.Span));

    private JsonNode? GetAsNode(string path)
    {
        if (_overlay.TryRead(path, out var n) && n is not null) return n;
        if (TryReadAlias(path, out var aliasElem)) return JsonNode.Parse(aliasElem.GetRawText());
        // Fall back to parent: read parent's node at path
        return ((DataContextImpl)_parent).TryGetNodeForChildFallback(path);
    }

    private bool TryReadAlias(string path, out JsonElement element)
    {
        // Find longest-matching alias prefix, then descend into element.
        foreach (var (aliasPath, value) in _aliases)
        {
            if (CanonicalPath.IsAncestor(aliasPath, path))
            {
                if (path == aliasPath) { element = value; return true; }
                // Walk descendant
                var rel = path.Substring(aliasPath.Length);
                var segments = CanonicalPath.GetSegments("$" + rel);
                JsonElement cur = value;
                foreach (var seg in segments)
                {
                    if (seg.StartsWith("."))
                    {
                        if (cur.ValueKind != JsonValueKind.Object || !cur.TryGetProperty(seg.Substring(1), out cur)) { element = default; return false; }
                    }
                    else
                    {
                        var idx = int.Parse(seg.Substring(1, seg.Length - 2));
                        if (cur.ValueKind != JsonValueKind.Array || idx >= cur.GetArrayLength()) { element = default; return false; }
                        cur = cur[idx];
                    }
                }
                element = cur;
                return true;
            }
        }
        element = default;
        return false;
    }

    private static DataKind KindOf(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => DataKind.Object, JsonValueKind.Array => DataKind.Array,
        JsonValueKind.String => DataKind.String, JsonValueKind.Number => DataKind.Number,
        JsonValueKind.True or JsonValueKind.False => DataKind.Boolean,
        JsonValueKind.Null => DataKind.Null,
        _ => DataKind.Undefined
    };

    private static DataKind KindOfNode(JsonNode n) => n switch
    {
        JsonObject => DataKind.Object,
        JsonArray => DataKind.Array,
        JsonValue v => v.GetValueKind() switch
        {
            JsonValueKind.String => DataKind.String, JsonValueKind.Number => DataKind.Number,
            JsonValueKind.True or JsonValueKind.False => DataKind.Boolean,
            JsonValueKind.Null => DataKind.Null,
            _ => DataKind.Undefined
        },
        _ => DataKind.Undefined
    };
}

// On DataContextImpl, expose internal helper for child fallback:
internal JsonNode? TryGetNodeForChildFallback(string path)
{
    return _overlay.TryRead(path, out var n) ? n : null;
}
```

NOTE: the `throw new NotImplementedException()` placeholders are intentional for this task — the failing tests in Step 1 only exercise reads, writes, and aliases. Implement the rest in subsequent tasks once driven by failing tests.

- [ ] **Step 4: Run — pass the 3 child tests**

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/DataContext.cs tests/Sdk.Common.Tests/EtlDataPipeline/DataContext/ChildContextTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(datacontext): child contexts with alias-based zero-copy reads and parent fallback"
```

### Task 2.5: Fill in remaining child-context methods (TDD-driven)

For each of: `Length`, `Keys`, `GetArray`, `IterateArrayAsync`, `IterateObjectAsync`, `IterateMatchesAsync`, `CopyTo`, `WriteJsonTo`, full `Set` writeModes, complete the implementation as:

- [ ] Add a focused test for that method on `DataContextChild`
- [ ] Make it pass
- [ ] Commit individually

These are mostly mechanical: mirror the corresponding `DataContextImpl` method, but route reads through `GetAsNode` (which already does overlay → alias → parent fallback) and writes through the child's own overlay.

After this task, all `IDataContext` methods are implemented for both root and child contexts. **Run all DataContext tests and the full Sdk.Common.Tests test suite — note: many tests in `tests/Sdk.Common.Tests/` will still fail because they reference old node code. That's expected and addressed in Phase 4–6.**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~DataContext|FullyQualifiedName~JsonPath" -c DebugL
```

Expected: PASS for the DataContext + JsonPath tests; ignore the rest for now.

---

## Phase 2A — Orchestration scaffolding

**Goal:** restore the pipeline orchestration glue against the new path-only `IDataContext`. Phase 4 (iteration node rewrites) needs `IPipelineNode`, `NodeDelegate`, `INodeContext`/`NodeContext`, `IEtlContext`/`DefaultEtlContext`/`EtlContextAccessor`, `IEtlDataOrchestrator`/`EtlDataOrchestrator` to be compilable and correct. None of these files have node-specific business logic — most of the work is removing `Newtonsoft.Json.Linq` references, swapping `JToken` for `JsonNode`, and replacing `dataContext.Current` accesses with `Get<T>("$")` / path operations.

These tasks share the following pattern. Skip the steps that don't apply to the file in question.

**Common task template (rev1 orchestration tasks):**

- [ ] **Step 1: Restore from `<Compile Remove>` exclusion** — open `/Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/Sdk.Common.csproj` and delete the `<Compile Remove="..." />` line for the file being migrated.
- [ ] **Step 2: Read the existing (now-restored) file** to enumerate dependencies (Newtonsoft types, IDataContext usages, etc.).
- [ ] **Step 3: Apply the migration transformation** — use the rules from Task 6.1. Replace `JToken` with `JsonNode`, `JObject` with `JsonObject`, `JArray` with `JsonArray`, `JTokenType` with `DataKind`. Replace `dataContext.Current` reads with `dataContext.Get<JsonNode>("$")`; writes with `dataContext.Set("$", node)`. For deconstructed iterations (`foreach (var (k, v) in obj)`) use `foreach (var kvp in obj)` to keep netstandard2.0 happy.
- [ ] **Step 4: `dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/Sdk.Common.csproj -c DebugL`** until clean.
- [ ] **Step 5: If a corresponding test file exists in `tests/Sdk.Common.Tests/`** (e.g., `EtlContextAccessorTests.cs`), restore it from the test-side `<Compile Remove>` exclusion in `Sdk.Common.Tests.csproj`, migrate the test to the new API (typically just `JToken`→`JsonNode` and `dataContext.GetSimpleValueByPath` → `dataContext.Get<T>`), and run it.
- [ ] **Step 6: Commit** the production file, test file (if any), and csproj updates together.

The order below is dependency-driven: types referenced by other types come first.

### Task 2A.1: Migrate `IGlobalConfiguration`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/IGlobalConfiguration.cs`

`IGlobalConfiguration` is a pure interface with no Newtonsoft dependencies (verified — uses string, bool, generics only). It is **not** in the `<Compile Remove>` block — it lives under `Nodes/` (top-level), but the broader `EtlDataPipeline/Nodes/**/*.cs` exclusion captures it. So this task simply confirms the file remains correct after the new `IDataContext` lands.

- [ ] **Step 1: Read the file** at `/Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/IGlobalConfiguration.cs`. It should compile without changes once it's no longer excluded.
- [ ] **Step 2: Update the `<Compile Remove="EtlDataPipeline/Nodes/**/*.cs" />` line in `Sdk.Common.csproj`** to a more specific exclusion that EXCLUDES this single file from the wildcard:

```xml
<!-- Replace the broad exclusion with this set, restoring IGlobalConfiguration and its concrete impl + the lookup services that have no Newtonsoft deps -->
<Compile Remove="EtlDataPipeline/Nodes/**/*.cs" />
<Compile Include="EtlDataPipeline/Nodes/IGlobalConfiguration.cs" />
<Compile Include="EtlDataPipeline/Nodes/GlobalConfiguration.cs" />
<Compile Include="EtlDataPipeline/Nodes/INodeLookupService.cs" />
<Compile Include="EtlDataPipeline/Nodes/INodeQualifiedNameLookupService.cs" />
<Compile Include="EtlDataPipeline/Nodes/NodeLookupService.cs" />
<Compile Include="EtlDataPipeline/Nodes/NodeQualifiedNameLookupService.cs" />
<Compile Include="EtlDataPipeline/Nodes/NodeLookup.cs" />
```

NOTE: only re-include the file if its content compiles cleanly with the new `IDataContext`. Read each before re-including; any file that uses `JToken` etc. stays excluded and is migrated in a later task. `GlobalConfiguration.cs` likely uses Newtonsoft for `GetValue<T>` deserialization — if so, migrate it as part of Step 3.

- [ ] **Step 3: If `GlobalConfiguration.cs` uses `Newtonsoft.Json`** for raw JSON deserialization in `GetValue<T>`/`GetRawJson`, replace with `System.Text.Json.JsonSerializer.Deserialize<T>(json, PipelineJsonOptions.Default)`.
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/Nodes/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): restore IGlobalConfiguration and lookup services after IDataContext migration"
```

### Task 2A.2: Migrate `INodeContext` and `NodeContext`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/INodeContext.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/NodeContext.cs`

`INodeContext.CreateSubContext(JToken? input, ...)` is the legacy entry-point. The new shape removes `JToken` from the public surface. Replace both `CreateSubContext` overloads to take `JsonElement` (zero-copy alias) or `JsonNode?` (write-able).

- [ ] **Step 1: Restore from `<Compile Remove>`** — both files are captured by `EtlDataPipeline/Nodes/**/*.cs`. Add explicit `<Compile Include>` lines as in Task 2A.1.
- [ ] **Step 2: Read both files.**
- [ ] **Step 3: Replace `JToken? input` parameters** with `JsonElement? aliasElement` (the iteration alias) in both `CreateSubContext` overloads. Update implementation to call `((DataContextImpl)dataContext).CreateIterationChild(...)` with the alias.
- [ ] **Step 4: Remove `using Newtonsoft.Json.Linq;`** from both files. There should be no other Newtonsoft references — `NodeContext.cs` is mostly logging + path tracking.
- [ ] **Step 5: `dotnet build` clean.**
- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/Nodes/INodeContext.cs src/Sdk.Common/EtlDataPipeline/Nodes/NodeContext.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate INodeContext / NodeContext to JsonElement aliases"
```

### Task 2A.3: Migrate `NodeDelegate`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/NodeDelegate.cs`

`NodeDelegate` is a one-liner: `delegate Task(IDataContext dataContext, INodeContext nodeContext)`. The signature does NOT change — it already takes `IDataContext`. The file's `<Compile Remove>` exclusion exists only because earlier `IDataContext` was the legacy form; once `INodeContext` migrates (Task 2A.2), `NodeDelegate` compiles cleanly.

- [ ] **Step 1: Remove the `<Compile Remove="EtlDataPipeline/NodeDelegate.cs" />` line** from `Sdk.Common.csproj`.
- [ ] **Step 2: Read the file** and confirm it has no Newtonsoft `using` statements. It uses only `IDataContext` and `INodeContext` — both already migrated.
- [ ] **Step 3: `dotnet build` clean.**
- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): restore NodeDelegate (no migration needed)"
```

### Task 2A.4: Migrate `IPipelineNode`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/IPipelineNode.cs`

Same shape as `NodeDelegate` — a single-method interface taking `IDataContext`/`INodeContext`. No code changes; just remove the exclusion.

- [ ] **Step 1: Remove the `<Compile Remove="EtlDataPipeline/IPipelineNode.cs" />` line.**
- [ ] **Step 2: Read the file** to confirm no Newtonsoft references. (Verified: only `using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;`.)
- [ ] **Step 3: `dotnet build` clean.**
- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): restore IPipelineNode (no migration needed)"
```

### Task 2A.5: Migrate `IEtlContext`, `DefaultEtlContext`, `EtlContextAccessor`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/IEtlContext.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DefaultEtlContext.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/EtlContextAccessor.cs`

`IEtlContext` is the per-execution context (tenant, execution id, properties dict). Inspection shows `IEtlContext.cs` has no Newtonsoft references — only `OctoObjectId`, `RtEntityId`, etc. `DefaultEtlContext` and `EtlContextAccessor` are likely similar. Some properties may need migration if `Properties: IDictionary<string, object?>` is read with Newtonsoft elsewhere (it isn't — it's `object?`).

- [ ] **Step 1: Remove the three `<Compile Remove>` lines** for these files.
- [ ] **Step 2: Read each file**; remove any `using Newtonsoft.Json.*;` lines if present.
- [ ] **Step 3: If `DefaultEtlContext` exposes any `JToken`-typed property** (unlikely — `IEtlContext` doesn't define one), change it to `JsonNode?`.
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: If `tests/Sdk.Common.Tests/EtlDataPipeline/EtlContextAccessorTests.cs` exists**, restore from the test-side exclusion, migrate, and run.
- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/IEtlContext.cs src/Sdk.Common/EtlDataPipeline/DefaultEtlContext.cs src/Sdk.Common/EtlDataPipeline/EtlContextAccessor.cs tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj tests/Sdk.Common.Tests/EtlDataPipeline/EtlContextAccessorTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): restore IEtlContext / DefaultEtlContext / EtlContextAccessor"
```

### Task 2A.6: Migrate `IEtlDataOrchestrator` and `EtlDataOrchestrator`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/IEtlDataOrchestrator.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/EtlDataOrchestrator.cs`

The orchestrator is the top-level driver: it constructs `IDataContext`, walks `IPipelineNode` chains, and reports results. Likely takes a `JToken?` initial value in the legacy API — replace with a more flexible shape:

- New entry signature: `Task<JsonNode?> RunAsync(IPipelineNode root, IEtlContext etlContext, JsonElement? initialValue = null, CancellationToken ct = default)`.
- Inside, construct `DataContextImpl(initialValue ?? JsonDocument.Parse("{}").RootElement)`.

- [ ] **Step 1: Remove the two `<Compile Remove>` lines.**
- [ ] **Step 2: Read both files.**
- [ ] **Step 3: Migrate**:
  - `JToken?` initial value → `JsonElement?` parameter.
  - Internal `JToken` plumbing for capture/return → `JsonNode?` constructed from `dataContext.Get<JsonNode>("$")`.
  - Any `dataContext.Current` accesses → `dataContext.Get<JsonNode>("$")` / `dataContext.Set("$", ...)`.
  - Any deconstruction loops over the properties dictionary → use `foreach (var kvp in dict)` (netstandard2.0 caveat).
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: If `tests/Sdk.Common.Tests/EtlDataPipeline/EtlDataOrchestratorTests.cs` exists**, restore + migrate + run.
- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/IEtlDataOrchestrator.cs src/Sdk.Common/EtlDataPipeline/EtlDataOrchestrator.cs tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj tests/Sdk.Common.Tests/EtlDataPipeline/EtlDataOrchestratorTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate EtlDataOrchestrator to JsonElement / JsonNode"
```

### Task 2A.7: Migrate `NodeBase` and `ChildNodeBase`

**Files (search if not at the assumed path):**
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/NodeBase.cs` (or wherever `NodeBase` lives)
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/ChildNodeBase.cs` (or equivalent)

`NodeBase` is the abstract base every concrete node inherits from; `ChildNodeBase` adds child-iteration helpers (`ProcessChildTransformationsAsSequenceAsync`). The concrete nodes won't compile until these bases are migrated.

- [ ] **Step 1: Locate the files**

```bash
find /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common -name 'NodeBase.cs' -o -name 'ChildNodeBase.cs'
```

If the file lives outside the `Nodes/` directory, adjust paths in subsequent steps.
- [ ] **Step 2: Restore from `<Compile Remove>`** (both are under the `EtlDataPipeline/Nodes/**/*.cs` glob — add explicit `<Compile Include>` entries).
- [ ] **Step 3: Read both.**
- [ ] **Step 4: Apply migration transformation**:
  - `JToken` parameters/return types → `JsonNode?`.
  - Internal `dataContext.Current` accesses → `dataContext.Get<JsonNode>("$")` / `dataContext.Set("$", ...)`.
  - `IDataContext.CreateSubContext(JToken? input, ...)` calls → `((DataContextImpl)ctx).CreateIterationChild(aliases)` with `JsonElement` aliases.
  - `ProcessChildTransformationsAsSequenceAsync` likely walks a `Transformations` collection from a `IChildNodeConfiguration` and chains `NodeDelegate`s — pure plumbing, no JSON-type changes needed.
- [ ] **Step 5: `dotnet build` clean.**
- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/Nodes/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate NodeBase and ChildNodeBase to JsonNode"
```

After this task, all orchestration scaffolding compiles. Phase 4 (iteration node rewrites) can proceed.

---

## Phase 2B — Port `DataPipelineException`

### Task 2B.1: Restore and migrate `DataPipelineException`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataPipelineException.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext.cs` (replace `InvalidOperationException` placeholders introduced in Task 2.3 with typed factories)

`DataPipelineException` is the typed exception family used by every pipeline node. Preserve the typed shape (each factory returns a specific exception subtype semantically) but rewrite factory methods that take `JToken` to take string paths and `JsonNode?` / `JsonElement` instead.

- [ ] **Step 1: Read the existing (excluded) file**

```bash
cat /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/EtlDataPipeline/DataPipelineException.cs
```

Enumerate every `internal static Exception X(...)` factory. The `JToken` consumers as of plan rev1 are:
- `SourceMustBeAnObject(JToken currentNode)` — uses `currentNode.GetType().Name` to embed the kind name in the message.

All other factories already take string paths or scalar args; they need only a `using` cleanup.

- [ ] **Step 2: Remove the `<Compile Remove="EtlDataPipeline/DataPipelineException.cs" />` line** from `Sdk.Common.csproj`.

- [ ] **Step 3: Migrate the file**:

```csharp
// Top of file: replace
//   using Newtonsoft.Json.Linq;
// with no Newtonsoft usings (System.Text.Json types are referenced inline by full name where needed,
// or add `using System.Text.Json.Nodes;` if you want JsonNode in factory signatures).

internal static Exception SourceMustBeAnObject(string sourcePath, DataKind currentKind)
{
    return new DataPipelineException($"Source at path '{sourcePath}' must be an object. Current kind is '{currentKind}'.");
}
```

Update all callers of `SourceMustBeAnObject` accordingly (likely `MergeAt` in `DataContext.cs`, or one of the YELLOW node implementations).

- [ ] **Step 4: Update `DataContext.cs` to use typed factories**

In `DataContextImpl.AppendOrPrepend` (the rev0 plan body shows `throw DataPipelineException.ValueIsArrayMustBeScalarForWriteMode(...)`); the implementation that landed in Task 2.3 currently throws `InvalidOperationException` placeholders. Replace each:

```csharp
// AppendOrPrepend — replace the InvalidOperationException placeholder:
throw DataPipelineException.ValueIsArrayMustBeScalarForWriteMode(path,
    prepend ? TargetValueWriteModes.Prepend : TargetValueWriteModes.Append);

// MergeAt — replace the InvalidOperationException placeholder:
throw DataPipelineException.SourceValueIsObjectMustBeObjectForWriteMode(path, TargetValueWriteModes.Merge);
```

Audit the rest of `DataContext.cs` for any `InvalidOperationException`s introduced in Task 2.3 placeholders and replace with the appropriate typed factory.

- [ ] **Step 5: Re-run all DataContext / DataOverlay / JsonPath tests** to confirm no regressions

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~DataContext|FullyQualifiedName~DataOverlay|FullyQualifiedName~JsonPath" -c DebugL
```

Expected: all pass. Tests that previously asserted `Throws<InvalidOperationException>` on writeMode mismatches must be updated to assert `Throws<DataPipelineException>`.

- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/DataPipelineException.cs src/Sdk.Common/EtlDataPipeline/DataContext.cs tests/Sdk.Common.Tests/EtlDataPipeline/DataContext/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): port DataPipelineException to STJ; replace InvalidOperationException placeholders"
```

---

## Phase 3 — Parity Test Harness

### Task 3.1: Scaffold `Sdk.Common.PipelineParityTests` project

**Files:**
- Create: `octo-sdk/tests/Sdk.Common.PipelineParityTests/Sdk.Common.PipelineParityTests.csproj`

- [ ] **Step 1: Create the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>Sdk.Common.PipelineParityTests</RootNamespace>
    <Configurations>Debug;Release;DebugL</Configurations>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Sdk.Common\Sdk.Common.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add to solution**

```bash
dotnet sln /Users/reimar/dev/meshmakers/branches/main/octo-sdk/Octo.Sdk.sln add /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.PipelineParityTests/Sdk.Common.PipelineParityTests.csproj
```

- [ ] **Step 3: Verify it builds (expect compile errors only when test files exist)**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.PipelineParityTests/Sdk.Common.PipelineParityTests.csproj -c DebugL
```

Expected: build success (no source files yet).

- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add tests/Sdk.Common.PipelineParityTests/Sdk.Common.PipelineParityTests.csproj Octo.Sdk.sln
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "test: scaffold Sdk.Common.PipelineParityTests project"
```

### Task 3.2: Build the JSONPath corpus

**Files:**
- Create: `octo-sdk/tests/Sdk.Common.PipelineParityTests/PathExpressions.cs`

- [ ] **Step 1: Run extraction script (one-shot, manual)**

```bash
# Extract all JSONPath strings from production pipelines and in-tree code into a sorted unique list.
{
  rg -h --no-line-number -o "\"[^\"]*\\\$[^\"]*\"" /Users/reimar/dev/meshmakers/branches/main/deployment/maco-deployment /Users/reimar/dev/meshmakers/branches/main/deployment/energy-community-deployment 2>/dev/null
  rg -h --no-line-number -o "[^\"']*\\\$\\.[A-Za-z_][^\"' ]+" /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/src 2>/dev/null
} | sort -u > /tmp/path-corpus.txt
wc -l /tmp/path-corpus.txt
```

- [ ] **Step 2: Hand-curate the corpus to ~50 representative paths** spanning every dialect feature (root, dotted, index, wildcard, nested wildcard, recursive descent, equality filter, recursive descent + filter). Save curated list as a static C# array:

```csharp
namespace Sdk.Common.PipelineParityTests;

public static class PathExpressions
{
    public static readonly string[] All =
    {
        // Root and dotted
        "$",
        "$.foo",
        "$.foo.bar.baz",
        "$._items",
        "$.full_doc.nested",

        // Array index
        "$.arr[0]",
        "$.documents[1]",
        "$.key.fileSystemItem[0].Items[0].RtId",

        // Wildcards
        "$.items[*]",
        "$.orders[*].items[*]",
        "$.lineItems[*].total",
        "$.result[*]._operatingFacilities[*]._operatingFacilityUpdate",

        // Recursive descent
        "$..target",
        "$..[*]",
        "$.._entityUpdates[*]",
        "$.._associationUpdates[*]",

        // Equality filters
        "$.items[?(@.Id == 'abc')]",
        "$.items[?(@.attrs.code == 'X1')]",
        "$..[?(@.Id == 'machine_1')].Value",
        "$..[?(@.Id == 'machine_1')].Status",

        // Empty / edge
        "$.missing.path",
        "$.arr[5]",
    };
}
```

- [ ] **Step 3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add tests/Sdk.Common.PipelineParityTests/PathExpressions.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "test(parity): add curated JSONPath corpus from in-tree and production"
```

### Task 3.3: Build the corpus loader and synthetic input documents

**Files:**
- Create: `octo-sdk/tests/Sdk.Common.PipelineParityTests/ParityCorpus.cs`
- Create: `octo-sdk/tests/Sdk.Common.PipelineParityTests/TestData/cust-orders.json`
- Create: `octo-sdk/tests/Sdk.Common.PipelineParityTests/TestData/eda-messages.json`
- Create: `octo-sdk/tests/Sdk.Common.PipelineParityTests/TestData/machine-statuses.json`

- [ ] **Step 1: Synthesize three input documents** that mirror common shapes from production:
  1. `cust-orders.json` — `{ orders: [{ id, items: [...] }, ...] }` for wildcard chains.
  2. `eda-messages.json` — `{ EdaMessages: [{ MeteringPoints: [{ Id, Value, Status }] }] }` for nested wildcards + filters.
  3. `machine-statuses.json` — `{ Machines: [{ Id, Value, Status }] }` for equality filters.

Each ~5–10 KB, hand-written to cover edge cases.

- [ ] **Step 2: Mark them as embedded resources in the csproj**

Add to `Sdk.Common.PipelineParityTests.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="TestData\*.json" />
</ItemGroup>
```

- [ ] **Step 3: ParityCorpus loader**

```csharp
using System.Reflection;

namespace Sdk.Common.PipelineParityTests;

public static class ParityCorpus
{
    public static IEnumerable<(string Name, string Json)> Inputs()
    {
        var asm = typeof(ParityCorpus).Assembly;
        foreach (var name in asm.GetManifestResourceNames().Where(n => n.EndsWith(".json")))
        {
            using var s = asm.GetManifestResourceStream(name)!;
            using var r = new StreamReader(s);
            yield return (name, r.ReadToEnd());
        }
    }
}
```

- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add tests/Sdk.Common.PipelineParityTests/ParityCorpus.cs tests/Sdk.Common.PipelineParityTests/TestData tests/Sdk.Common.PipelineParityTests/Sdk.Common.PipelineParityTests.csproj
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "test(parity): add corpus loader and synthetic input documents"
```

### Task 3.4: Read-parity test (Newtonsoft SelectTokens vs new evaluator)

**Files:**
- Create: `octo-sdk/tests/Sdk.Common.PipelineParityTests/ReadParityTests.cs`

- [ ] **Step 1: Write the parity test**

```csharp
using System.Text.Json;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Sdk.Common.PipelineParityTests;

public class ReadParityTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var (name, json) in ParityCorpus.Inputs())
        {
            foreach (var path in PathExpressions.All)
            {
                yield return new object[] { name, json, path };
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void NewtonsoftAndStj_AgreeOnPath(string corpusName, string json, string path)
    {
        // Newtonsoft expected results.
        var jt = JToken.Parse(json);
        var expected = jt.SelectTokens(path).Select(t => t.ToString(Newtonsoft.Json.Formatting.None)).ToList();

        // STJ + new evaluator results — skip if path uses unsupported feature.
        List<string> actual;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var expr = JsonPathParser.Parse(path);
            actual = JsonPathEvaluator.Evaluate(doc.RootElement, expr).Select(m => m.Element.GetRawText()).ToList();
        }
        catch (JsonPathNotSupportedException)
        {
            return; // dialect-out-of-scope — by spec §6.4
        }

        Assert.Equal(expected.Count, actual.Count);

        // Compare structurally: parse both sides as JsonDocument and compare raw text after normalization.
        for (var i = 0; i < expected.Count; i++)
        {
            var expectedNormalized = NormalizeJson(expected[i]);
            var actualNormalized = NormalizeJson(actual[i]);
            Assert.Equal(expectedNormalized, actualNormalized);
        }
    }

    private static string NormalizeJson(string s)
    {
        // Round-trip parse to canonicalize whitespace, key ordering left to lexical equivalence.
        try
        {
            using var d = JsonDocument.Parse(s);
            return JsonSerializer.Serialize(d.RootElement);
        }
        catch
        {
            return s; // raw scalar
        }
    }
}
```

- [ ] **Step 2: Run**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.PipelineParityTests/Sdk.Common.PipelineParityTests.csproj -c DebugL --logger "console;verbosity=normal"
```

Expected: most tests pass. Any failures indicate a real evaluator divergence — investigate, fix, re-run. Common categories of divergence:

- Object-wildcard ordering: Newtonsoft returns properties in an order that may differ from STJ's `EnumerateObject`. If the count agrees but ordering differs, sort both sides before comparing in the test (acceptable for set-equality semantics).
- Number representation: Newtonsoft's `5` vs STJ's `5` should both serialize identically; if they don't, normalize both as strings and compare.
- Recursive-descent ordering: spec doesn't require a specific order; if the count agrees, treat as set-equal.

If a divergence requires evaluator changes, fix in the evaluator and add a focused unit test in `JsonPathEvaluatorTests` for the specific case.

- [ ] **Step 3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add tests/Sdk.Common.PipelineParityTests/ReadParityTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "test(parity): read-parity tests across corpus × path expressions"
```

### Task 3.5: Write-parity test

**Files:**
- Create: `octo-sdk/tests/Sdk.Common.PipelineParityTests/WriteParityTests.cs`

- [ ] **Step 1: Test SetValueByPath behavioral parity**

```csharp
using System.Text.Json;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Sdk.Common.PipelineParityTests;

public class WriteParityTests
{
    [Fact]
    public void SimpleOverwrite_Parity()
    {
        var json = "{\"a\": 1}";
        var jt = JObject.Parse(json);
        jt["a"] = 99;
        var expected = JsonSerializer.Serialize(JsonDocument.Parse(jt.ToString(Newtonsoft.Json.Formatting.None)).RootElement);

        var ctx = new DataContextImpl(JsonDocument.Parse(json).RootElement);
        ctx.Set("$.a", 99);
        using var ms = new MemoryStream();
        ctx.WriteJsonTo("$", ms);
        var actual = JsonSerializer.Serialize(JsonDocument.Parse(ms.ToArray()).RootElement);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetMissingPath_AutoCreatesIntermediateObjects()
    {
        // Newtonsoft's ReplaceNested behavior — must match.
        var ctx = new DataContextImpl(JsonDocument.Parse("{}").RootElement);
        ctx.Set("$.a.b.c", 42);
        Assert.Equal(42, ctx.Get<int>("$.a.b.c"));
        Assert.Equal(DataKind.Object, ctx.GetKind("$.a"));
        Assert.Equal(DataKind.Object, ctx.GetKind("$.a.b"));
    }

    [Fact]
    public void Append_ToExistingArray_AddsAtEnd()
    {
        var ctx = new DataContextImpl(JsonDocument.Parse("{\"arr\": [1,2]}").RootElement);
        ctx.Set("$.arr", 3, DocumentModes.Extend, ValueKinds.Simple, TargetValueWriteModes.Append);
        var arr = ctx.GetArray<int>("$.arr")!.ToArray();
        Assert.Equal(new[] { 1, 2, 3 }, arr);
    }
}
```

- [ ] **Step 2: Run, fix, commit**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.PipelineParityTests/Sdk.Common.PipelineParityTests.csproj \
  --filter "FullyQualifiedName~WriteParityTests" -c DebugL
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add tests/Sdk.Common.PipelineParityTests/WriteParityTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "test(parity): write-parity tests for SetByPath semantics"
```

---

## Phase 4 — Iteration Node Rewrites

These three nodes are where the zero-copy optimization actually pays off. Each becomes ~5 lines of `Iterate*Async` calls.

### Task 4.1: Rewrite `ForEachNode` against new API

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Control/ForEachNode.cs`

- [ ] **Step 1: Replace the entire body of `ProcessObjectAsync` with the path-only version**

```csharp
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

[NodeName("ForEach", 1)]
public record ForEachNodeConfiguration : SourceTargetPathNodeConfiguration, IChildNodeConfiguration
{
    public required ICollection<NodeConfiguration>? Transformations { get; set; }

    [PropertyGroup("Paths", 2, "jsonpath")]
    public string FullDocumentPath { get; set; } = "$.full";

    [PropertyGroup("Paths", 0, "jsonpath")]
    public required string IterationPath { get; set; }

    [PropertyGroup("Paths", 3, "jsonpath")]
    public string KeyPath { get; set; } = "$.key";

    [PropertyGroup("Paths", 4, "jsonpath")]
    public string MergePath { get; set; } = "$.key";

    [PropertyGroup("Execution", 10)]
    public int MaxDegreeOfParallelism { get; set; }
}

[NodeConfiguration(typeof(ForEachNodeConfiguration))]
public class ForEachNode(NodeDelegate next) : ChildNodeBase
{
    public override async Task ProcessObjectAsync(IDataContext dataContext, INodeContext rootNodeContext)
    {
        var c = rootNodeContext.GetNodeConfiguration<ForEachNodeConfiguration>();

        if (!dataContext.Exists(c.Path))
        {
            throw PipelineExecutionException.PathNotFound(rootNodeContext.NodePath, c.Path);
        }
        if (dataContext.GetKind(c.IterationPath) != DataKind.Array)
        {
            throw PipelineExecutionException.PathMustBeArray(rootNodeContext.NodePath, nameof(c.IterationPath), c.IterationPath);
        }

        var collected = new System.Collections.Concurrent.ConcurrentBag<System.Text.Json.Nodes.JsonNode?>();
        var index = 0u;

        await dataContext.IterateArrayAsync(c.IterationPath, async itemCtx =>
        {
            // The iterator gives us a child context whose root is the current item.
            // To match the old behavior, we wrap it so the child sees:
            //   $.full = subInputObject (parent's c.Path subtree, via alias)
            //   $.key  = currentItem    (already the child's root from IterateArrayAsync)
            //
            // Implementation: we re-create a richer child here using the parent's CreateIterationChild API.
            // But for the simple case where MergePath == KeyPath (the default), we can run the body directly
            // and read the merged value from the child's root.

            var itemNodeContext = rootNodeContext.RegisterChildNode(index, c, itemCtx);
            var arrayNext = new NodeDelegate((ds, nc) =>
            {
                itemNodeContext.Unregister(ds);
                var mergeItem = ds.Get<System.Text.Json.Nodes.JsonNode>(c.MergePath);
                if (mergeItem is not null) collected.Add(mergeItem.DeepClone());
                return Task.CompletedTask;
            });

            await ProcessChildTransformationsAsSequenceAsync(itemCtx, itemNodeContext, arrayNext, c);
            index++;
        });

        var resultArray = new System.Text.Json.Nodes.JsonArray();
        foreach (var item in collected) resultArray.Add(item?.DeepClone());
        dataContext.Set(c.TargetPath, resultArray, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode);

        await next(dataContext, rootNodeContext);
    }
}
```

NOTE: this implementation does not yet support the `$.full` alias for accessing the full document under `c.FullDocumentPath`. That's a small refinement: extend `IterateArrayAsync` to accept an optional `aliases` parameter, or introduce `IterateArrayWithAliasesAsync`. For the first cut, pipelines that depend on `$.full.X` reads from inside the iteration body will fail; we add that support in Task 4.2 if any in-tree tests fail.

- [ ] **Step 2: Run all node tests and observe failures**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~ForEach" -c DebugL
```

If any tests need `$.full.X` access, proceed to Task 4.2. Otherwise commit.

- [ ] **Step 3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/Nodes/Control/ForEachNode.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(pipeline): rewrite ForEachNode against path-only IDataContext"
```

### Task 4.2: Add `IterateArrayWithAliasesAsync` if needed

If Task 4.1 surfaced needs for `$.full.X` reads inside iteration:

- [ ] **Step 1: Add overload to `IDataContext`**

```csharp
Task IterateArrayAsync(string path, IReadOnlyList<(string Alias, string SourcePath)> aliases, Func<IDataContext, Task> body);
```

- [ ] **Step 2: Implement on `DataContextImpl`** by resolving each `SourcePath` to a `JsonElement` once (zero-copy view) and passing the aliases to `CreateIterationChild`.

- [ ] **Step 3: Update `ForEachNode` to call this overload** with `[("$" + FullDocumentPath.Substring(1), c.Path)]`.

- [ ] **Step 4: Test and commit.**

### Task 4.3: Rewrite `ObjectIteratorNode`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Control/ObjectIteratorNode.cs`

- [ ] **Step 1: Replace the body of `ProcessToken`**

```csharp
protected static async Task ProcessToken(IDataContext dataContext, INodeContext rootNodeContext,
    NodeDelegate nextDelegate, TTokenConfigurationNode iteratorConfigurationNode)
{
    if (dataContext.GetKind("$") == DataKind.Array)
    {
        var collected = new System.Collections.Concurrent.ConcurrentBag<System.Text.Json.Nodes.JsonNode?>();
        var index = 0u;
        await dataContext.IterateArrayAsync("$", async itemCtx =>
        {
            var itemNodeContext = rootNodeContext.RegisterChildNode(index, iteratorConfigurationNode, itemCtx);
            var arrayNext = new NodeDelegate((ds, nc) =>
            {
                itemNodeContext.Unregister(ds);
                var node = ds.Get<System.Text.Json.Nodes.JsonNode>("$");
                if (node is not null) collected.Add(node.DeepClone());
                return Task.CompletedTask;
            });
            await ProcessChildTransformationsAsSequenceAsync(itemCtx, itemNodeContext, arrayNext, iteratorConfigurationNode);
            index++;
        });

        var arr = new System.Text.Json.Nodes.JsonArray();
        foreach (var item in collected) arr.Add(item?.DeepClone());
        dataContext.Set("$", arr);
        await nextDelegate(dataContext, rootNodeContext);
    }
    else
    {
        var singleNext = new NodeDelegate((ds, nc) =>
        {
            nc.Unregister(ds);
            var node = ds.Get<System.Text.Json.Nodes.JsonNode>("$");
            dataContext.Set("$", node);
            return Task.CompletedTask;
        });
        await ProcessChildTransformationsAsSequenceAsync(dataContext, rootNodeContext, singleNext, iteratorConfigurationNode);
        await nextDelegate(dataContext, rootNodeContext);
    }
}
```

- [ ] **Step 2: Run focused tests, fix, commit**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~ObjectIterator" -c DebugL
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/Nodes/Control/ObjectIteratorNode.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(pipeline): rewrite ObjectIteratorNode against path-only IDataContext"
```

### Task 4.4: Rewrite `SelectByPathNode`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Control/SelectByPathNode.cs`

- [ ] **Step 1: Replace `ProcessObjectAsync`**

```csharp
public override async Task ProcessObjectAsync(IDataContext dataContext, INodeContext rootNodeContext)
{
    var c = rootNodeContext.GetNodeConfiguration<SelectByPathNodeConfiguration>();
    if (dataContext.GetKind("$") == DataKind.Undefined) { await next(dataContext, rootNodeContext); return; }

    var updates = new System.Collections.Concurrent.ConcurrentBag<UpdateItem>();
    var tasks = new List<Task>();

    foreach (var sel in c.SelectPath)
    {
        var path = sel.Path;
        if (!dataContext.Exists(path))
        {
            rootNodeContext.Debug($"No token found for path: {path}. Skipping execution.");
            continue;
        }

        async Task Run()
        {
            await dataContext.IterateMatchesAsync(path, async pathCtx =>
            {
                var pathNodeContext = rootNodeContext.RegisterChildNode(0u, sel, pathCtx); // index unused for matches
                var tokenNext = new NodeDelegate((ds, nc) =>
                {
                    pathNodeContext.Unregister(ds);
                    var value = ds.Get<System.Text.Json.Nodes.JsonNode>("$");
                    updates.Add(new UpdateItem
                    {
                        TargetPath = sel.TargetPath,
                        DocumentMode = sel.DocumentMode,
                        TargetValueKind = sel.TargetValueKind,
                        TargetValueWriteMode = sel.TargetValueTargetValueWriteMode,
                        Value = value?.DeepClone()
                    });
                    return Task.CompletedTask;
                });
                await ProcessChildTransformationsAsSequenceAsync(pathCtx, pathNodeContext, tokenNext, sel);
            });
        }

        tasks.Add(Run());
    }
    await Task.WhenAll(tasks);

    foreach (var u in updates)
    {
        dataContext.Set(u.TargetPath, u.Value, u.DocumentMode, u.TargetValueKind, u.TargetValueWriteMode);
    }

    await next(dataContext, rootNodeContext);
}

private record UpdateItem
{
    public required string TargetPath { get; init; }
    public required DocumentModes DocumentMode { get; init; }
    public required ValueKinds TargetValueKind { get; init; }
    public required TargetValueWriteModes TargetValueWriteMode { get; init; }
    public System.Text.Json.Nodes.JsonNode? Value { get; init; }
}
```

- [ ] **Step 2: Test and commit**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~SelectByPath" -c DebugL
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/Nodes/Control/SelectByPathNode.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "feat(pipeline): rewrite SelectByPathNode against path-only IDataContext"
```

---

## Phase 5 — RED + YELLOW Node Refactors

### Task 5.1: Refactor `UpdateRecordArrayItemNode` (RED)

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/UpdateRecordArrayItemNode.cs`

The node's old logic mutates a `JArray` view in place. Replace with read-modify-write reconstruction.

- [ ] **Step 1: Read the existing node body to understand current attribute matching/update semantics**

```bash
cat /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/UpdateRecordArrayItemNode.cs
```

- [ ] **Step 2: Replace the implementation**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.MeshAdapter.Sdk.Nodes.Transform;

[NodeConfiguration(typeof(UpdateRecordArrayItemNodeConfiguration))]
public class UpdateRecordArrayItemNode(NodeDelegate next) : NodeBase
{
    public override async Task ProcessAsync(IDataContext dataContext, INodeContext rootNodeContext)
    {
        var c = rootNodeContext.GetNodeConfiguration<UpdateRecordArrayItemNodeConfiguration>();

        if (dataContext.GetKind(c.Path) != DataKind.Array)
        {
            await next(dataContext, rootNodeContext);
            return;
        }

        var matchValue = dataContext.Get<string>(c.MatchValuePath);
        if (matchValue is null)
        {
            await next(dataContext, rootNodeContext);
            return;
        }

        var sourceArr = dataContext.Get<JsonArray>(c.Path);
        if (sourceArr is null) { await next(dataContext, rootNodeContext); return; }

        var updated = new JsonArray();
        foreach (var item in sourceArr)
        {
            if (item is JsonObject record)
            {
                var attrs = (record["Attributes"] ?? record["attributes"]) as JsonObject;
                var attrValue = attrs?[c.MatchAttributeName]?.GetValue<string>();
                if (string.Equals(attrValue, matchValue, StringComparison.OrdinalIgnoreCase))
                {
                    var clone = (JsonObject)record.DeepClone();
                    var cloneAttrs = (clone["Attributes"] ?? clone["attributes"]) as JsonObject;
                    if (cloneAttrs is not null)
                    {
                        foreach (var update in c.AttributeUpdates)
                        {
                            var newValue = dataContext.Get<JsonNode>(update.ValuePath);
                            cloneAttrs[update.AttributeName] = newValue?.DeepClone();
                        }
                    }
                    updated.Add(clone);
                    continue;
                }
            }
            updated.Add(item?.DeepClone());
        }

        dataContext.Set(c.TargetPath, updated, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode);
        await next(dataContext, rootNodeContext);
    }
}
```

- [ ] **Step 3: Migrate any related tests** in `octo-mesh-adapter/tests/` and run

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj \
  --filter "FullyQualifiedName~UpdateRecordArrayItem" -c DebugL
```

- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter add src/MeshAdapter.Sdk/Nodes/Transform/UpdateRecordArrayItemNode.cs tests/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter commit -m "refactor(meshadapter): rebuild UpdateRecordArrayItemNode without in-place mutation"
```

### Task 5.2: Refactor `ProjectNode` (YELLOW)

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/ProjectNode.cs`

- [ ] **Step 1: Replace the in-place mutation logic with overlay-based projection**

The node has two modes (Inclusion + Clear). Replace the old DeepClone+mutate with: read source, build projected JsonObject from configured fields, set it back via `dataContext.Set`. Specific code requires reading the existing implementation; the engineer should follow the pattern in §7.2 of the spec — produce projected JsonObject by enumerating config fields and `Get<JsonNode>` from source.

- [ ] **Step 2: Test, commit**

### Task 5.3: Refactor `MapNode` and `DistinctNode` (YELLOW)

Similar shape to 5.2. Replace local `JObject`/`JArray` constructions with `JsonObject`/`JsonArray`. Local mutations stay (they're internal, not on shared state). Commit each separately.

---

## Phase 6 — GREEN Node Mechanical Migration

For each GREEN node, the migration is a fixed transformation. Apply it to all files listed below.

### Task 6.1: Establish the transformation pattern

**Transformation rules** (apply mechanically per file):

1. `using Newtonsoft.Json;` → remove.
2. `using Newtonsoft.Json.Linq;` → remove.
3. `JToken token = dataContext.GetComplexObjectByPath<JToken>(path);` → `JsonNode? node = dataContext.Get<JsonNode>(path);`.
4. `dataContext.SetValueByPath(path, ...)` → `dataContext.Set(path, ...)` (signature is unchanged for the four-arg form).
5. `dataContext.GetSimpleValueByPath<T>(path)` → `dataContext.Get<T>(path)`.
6. `dataContext.GetSimpleArrayValueByPath<T>(path)` → `dataContext.GetArray<T>(path)`.
7. `dataContext.IsPathSimpleArrayValue(path)` → `dataContext.GetKind(path) == DataKind.Array`.
8. `dataContext.SelectByPath(path).Select(t => t.ToObject<T>())` → use `IterateMatchesAsync` if iteration is needed, or `Get<T>(path)` if a single match is expected.
9. `dataContext.Current` reads → `dataContext.Get<JsonNode>("$")` or kind/iteration helpers.
10. `dataContext.Current = newValue` → `dataContext.Set("$", newValue)`.
11. `JObject.FromObject(x)` → `JsonSerializer.SerializeToNode(x, PipelineJsonOptions.Default)`.
12. `JArray.FromObject(x)` → same as above (returns `JsonNode`; cast to `JsonArray` if necessary).
13. `token.ToObject<T>()` → `node.Deserialize<T>(PipelineJsonOptions.Default)`.
14. `JTokenType.X` checks → `GetKind` + `DataKind.X`.
15. `JsonSerializer` parameter (Newtonsoft) → `JsonSerializerOptions? options = null`, with default `PipelineJsonOptions.Default`.

### Task 6.2: Apply to GREEN nodes in `octo-sdk` (Trigger nodes)

**Files:**

- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Triggers/FromExecutePipelineCommandNode.cs`
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Triggers/FromPollingNode.cs`
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Triggers/FromPipelineDataEventNode.cs`

- [ ] **Step 1: Apply the transformation pattern to each file**
- [ ] **Step 2: `dotnet build` and resolve any leftover compilation errors**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/Sdk.Common.csproj -c DebugL
```

- [ ] **Step 3: Run `Sdk.Common.Tests` filtered to Trigger node tests; fix as needed**
- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/Nodes/Triggers/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate Trigger nodes to path-only IDataContext"
```

### Task 6.3: Apply to GREEN nodes (Loads)

**Files:**

- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Loads/ToWebhookNode.cs`
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Loads/ToPipelineDataEventNode.cs`
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Loads/SetPipelineExecutionResultNode.cs`

- [ ] Apply pattern, build, test, commit. Same shape as Task 6.2.

### Task 6.4: Apply to GREEN nodes (Extracts, Transforms)

**Files:** all `.cs` files under `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Extracts/` and `Nodes/Transforms/` not already covered (excluding the YELLOW ones from Phase 5).

- [ ] Apply pattern, build, test, commit per logical group (~6-10 files per commit).

### Task 6.5: Apply to `Sdk.SimulationNodes`

**Files:** all `.cs` files under `octo-sdk/src/Sdk.SimulationNodes/`.

- [ ] Apply pattern, build, test, commit.

### Task 6.6: Migrate configuration serializers

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/Serializer/JsonPipelineConfigurationSerializer.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/Serializer/YamlPipelineConfigurationSerializer.cs`

These serializers read/write `IPipelineConfiguration` (the YAML/JSON pipeline definition) using Newtonsoft `JsonSerializer` + `JToken`. Migrate to `System.Text.Json.JsonSerializer` + `JsonNode`. Yaml side likely uses YamlDotNet to deserialize into a Dictionary, then a JToken bridge — replace the JToken bridge with JsonNode (or write straight to a `JsonDocument` via `JsonSerializerOptions`).

- [ ] **Step 1: Restore both `<Compile Remove>` lines** for these files in `Sdk.Common.csproj`.
- [ ] **Step 2: Read each file** to understand the actual entry points (`Serialize`, `Deserialize<T>`) and what types they accept/return. Note which Newtonsoft `JsonConverter`s they wire up — those become STJ `JsonConverter`s in Task 6.7.
- [ ] **Step 3: Apply the Task 6.1 transformation rules**, with these specifics:
  - `JsonSerializer.Create(settings)` (Newtonsoft) → `new JsonSerializerOptions { ... }` (STJ); merge with `PipelineJsonOptions.Default`.
  - `JsonConvert.DeserializeObject<T>(json, settings)` → `System.Text.Json.JsonSerializer.Deserialize<T>(json, options)`.
  - `JToken.Parse(yaml)` bridge → use `JsonNode.Parse(...)` after YAML→JSON conversion via YamlDotNet's `Serializer` to emit JSON.
  - Wire up the `NodeConfigurationTypeConverter` as a `JsonConverter` (STJ) — but this depends on Task 6.7. If 6.7 hasn't landed yet, accept a build break here and complete both tasks together.
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: If `tests/Sdk.Common.Tests/EtlDataPipeline/Configuration/**/*Tests.cs` exists**, restore from `Sdk.Common.Tests.csproj` `<Compile Remove>` and migrate the tests. The serializer tests are valuable — they exercise polymorphic node config round-trips.
- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/Configuration/Serializer/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate JsonPipelineConfigurationSerializer / YamlPipelineConfigurationSerializer to STJ"
```

### Task 6.7: Migrate node configuration discriminator/converter/appender

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/Serializer/NodeConfigurationTypeAppender.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/Serializer/NodeConfigurationTypeConverter.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/Serializer/NodeConfigurationTypeDiscriminator.cs`

These three together implement polymorphic deserialization of `NodeConfiguration` based on a discriminator (likely `nodeName` or `$type`). In Newtonsoft this is a `JsonConverter` + custom `SerializationBinder`. In STJ, two paths exist:
1. **`[JsonDerivedType]` attributes** on the `NodeConfiguration` base class — but the derived type set isn't fixed at compile time (nodes are discovered via reflection from loaded assemblies), so this is unworkable.
2. **Custom `JsonConverter<NodeConfiguration>`** that reads the discriminator property, looks up the concrete type via the existing `NodeConfigurationTypeDiscriminator` registry, then deserializes again into that type.

Use option 2.

- [ ] **Step 1: Restore the three `<Compile Remove>` lines.**
- [ ] **Step 2: Read each file** — understand the discriminator scheme (which property holds the type name? how is it normalized?).
- [ ] **Step 3: Migrate**:
  - `NodeConfigurationTypeDiscriminator.cs`: pure registry — likely just remove `using Newtonsoft.Json.*;` and minor signature tweaks.
  - `NodeConfigurationTypeAppender.cs`: writes the discriminator property during serialization. In STJ, this is part of the `JsonConverter.Write(...)` method — fold into the converter.
  - `NodeConfigurationTypeConverter.cs`: implement `JsonConverter<NodeConfiguration>` with `Read` (look up discriminator, reparse subtree as concrete type) and `Write` (serialize concrete type, then add discriminator property).

Reference shape for `Read`:

```csharp
public override NodeConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    using var doc = JsonDocument.ParseValue(ref reader);
    var root = doc.RootElement;
    if (!root.TryGetProperty(_discriminatorPropertyName, out var disc))
    {
        throw DataPipelineException.NoDiscriminatorFound();
    }
    var concreteType = _discriminator.Resolve(disc.GetString()!);
    return (NodeConfiguration?)JsonSerializer.Deserialize(root.GetRawText(), concreteType, options);
}
```

- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: Run polymorphic round-trip tests** if any exist (likely under `tests/Sdk.Common.Tests/EtlDataPipeline/Configuration/`).
- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/Configuration/Serializer/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate NodeConfiguration polymorphic converter to STJ"
```

### Task 6.8: Migrate `NodeSchemaRegistry` and `PipelineSchemaGenerator`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/NodeSchemaRegistry.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/PipelineSchemaGenerator.cs`

These produce `pipeline-schema.json` (a JSON Schema) used for autocompletion in the Refinery Studio UI. The current implementation uses `NJsonSchema` (which itself pulls Newtonsoft transitively).

- [ ] **Step 1: Restore both `<Compile Remove>` lines.**
- [ ] **Step 2: Read both files.**
- [ ] **Step 3: Investigate the `NJsonSchema` dependency**:

```bash
rg "NJsonSchema" /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common
```

Decide one of:
  - **(a)** Keep `NJsonSchema` as a transitive Newtonsoft consumer. The `Sdk.Common.csproj` already references `NJsonSchema`. Document this as accepted: the migration removes Newtonsoft from the *pipeline runtime*, but `Sdk.Common` ends up keeping a transitive dep solely for schema generation, which runs at build time only.
  - **(b)** Switch to a STJ-native schema lib (e.g., `JsonSchema.Net`). This is a bigger change and only worth it if the user explicitly asks. Default: accept (a).
- [ ] **Step 4: Migrate the two files** to STJ wherever they directly use `JToken`/`JObject` for schema construction. Where `NJsonSchema` API itself takes Newtonsoft types, leave them — that's the transitive dep we accepted.
- [ ] **Step 5: Verify the generated schema is identical**:

```bash
# Build before:
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Plug.Simulation/Sdk.Plug.Simulation.csproj -c DebugL
cp /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Plug.Simulation/bin/DebugL/net10.0/pipeline-schema.json /tmp/pipeline-schema.before.json
# After this task's changes, rebuild and diff:
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Plug.Simulation/Sdk.Plug.Simulation.csproj -c DebugL
diff /tmp/pipeline-schema.before.json /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Plug.Simulation/bin/DebugL/net10.0/pipeline-schema.json
```

Investigate any non-trivial differences. Property-ordering churn is acceptable; semantic-shape changes are not.

- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/Configuration/NodeSchemaRegistry.cs src/Sdk.Common/EtlDataPipeline/Configuration/PipelineSchemaGenerator.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate NodeSchemaRegistry and PipelineSchemaGenerator to STJ"
```

### Task 6.9: Port `PipelineSerializationException`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/Serializer/PipelineSerializationException.cs`

Small file: typed exception thrown by serializers. Likely takes a path or property name in its factory methods, plus an inner exception. No JSON-type changes needed beyond removing Newtonsoft `using` statements if any.

- [ ] **Step 1: Restore the `<Compile Remove>` line.**
- [ ] **Step 2: Read the file.**
- [ ] **Step 3: Remove any `using Newtonsoft.*;` statements**. If a factory takes `JToken` as a parameter, change it to `JsonNode?`/`string path`.
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/Configuration/Serializer/PipelineSerializationException.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): port PipelineSerializationException to STJ"
```

### Task 6.10: Restore DI registration

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/DependencyInjection/DataPipelineBuilder.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/DependencyInjection/IDataPipelineBuilder.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Configuration/DependencyInjection/ServiceCollectionExtensions.cs`

These wire up the pipeline framework into `IServiceCollection`. They reference orchestrator/serializer/schema types. Once the underlying types are migrated (Tasks 2A.5, 2A.6, 6.6–6.9), these compile with only `using` cleanup.

- [ ] **Step 1: Remove the `<Compile Remove="EtlDataPipeline/Configuration/DependencyInjection/**/*.cs" />` line.**
- [ ] **Step 2: Read each file.**
- [ ] **Step 3: Remove `using Newtonsoft.Json.*;` lines if present.** Adjust any explicit type references that no longer exist.
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/Configuration/DependencyInjection/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): restore DataPipelineBuilder and DI extensions"
```

### Task 6.11: Migrate the pipeline debugger

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Debugger/**/*.cs` (full directory)

The debugger (`IPipelineDebugger`, `DefaultPipelineDebugger`, `DebugInformationRoot`, `DebugEventArgs`, `DebugPipelineLogger`, `IPipelineDebugSerializer`, `PipelineDebugSerializer`, `PipelineDebuggerException`) takes per-node snapshots of `IDataContext` for replay/inspection in tooling. The snapshot is currently a `JToken` (deep-cloned). Migrate snapshots to `JsonNode?` (deep-cloned via `node.DeepClone()`) and serialize via STJ.

- [ ] **Step 1: Remove the `<Compile Remove="EtlDataPipeline/Debugger/**/*.cs" />` line.**
- [ ] **Step 2: Read each file in the directory** to understand the data shapes (`DebugInformationRoot` likely contains a tree of node snapshots).
- [ ] **Step 3: Apply the Task 6.1 transformation rules**:
  - Snapshot type `JToken` → `JsonNode?`. Take snapshots via `dataContext.Get<JsonNode>("$")?.DeepClone()`.
  - `PipelineDebugSerializer` (writes the snapshot tree to disk) → migrate `JsonConvert.SerializeObject(...)` to `System.Text.Json.JsonSerializer.Serialize(...)` with `PipelineJsonOptions.Default`.
  - `DebugPipelineLogger` likely wraps `IPipelineLogger` to also fan out to a debugger — no JSON-type changes.
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/Debugger/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate debugger snapshots and serializer to STJ"
```

### Task 6.12: Delete `JTokenExtensions.cs`

**Files:**
- Delete: `octo-sdk/src/Sdk.Common/JTokenExtensions.cs`

This is the helper that absorbed bits of JToken navigation into convenient extension methods. Its functionality is now covered by `DataOverlay`, `DataContextImpl`, and `JsonPathEvaluator`.

- [ ] **Step 1: Confirm no remaining consumers**

```bash
rg "JTokenExtensions|JTokenEx\." /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src
```

Expected: empty after Phases 4–6 complete. Any remaining hit means the consumer wasn't migrated yet — fix the consumer first, then proceed.

- [ ] **Step 2: Delete the file**

```bash
rm /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/JTokenExtensions.cs
```

- [ ] **Step 3: Remove the `<Compile Remove="JTokenExtensions.cs" />` line** from `Sdk.Common.csproj`.

- [ ] **Step 4: `dotnet build` clean.**

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "chore: delete JTokenExtensions (functionality absorbed into DataOverlay)"
```

---

## Phase 6A — Adapters and Services

**Goal:** migrate the directories `octo-sdk/src/Sdk.Common/Adapters/` and `octo-sdk/src/Sdk.Common/Services/` against the new path-only `IDataContext`. These files glue the pipeline framework to the adapter hosting infrastructure (startup, hub callbacks, lifetime, polling, registry, context creation) and consume `IDataContext` / `IEtlDataOrchestrator` indirectly.

### Task 6A.1: Enumerate the file set

- [ ] **Step 1: List all files in both directories**

```bash
find /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/Adapters -name '*.cs' -type f
find /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/Services -name '*.cs' -type f
```

Expected files (verified at plan-rev1 time):

`Adapters/`:
- `IAdapterService.cs`, `AdapterExecutionService.cs`, `AdapterHealthFileService.cs`, `AdapterShutdown.cs`, `AdapterStartup.cs`, `AdapterOptions.cs`, `HostedAdapterExecutionService.cs`, `IAdapterHubCallbackService.cs`, `AdapterException.cs`, `AdapterPipelineDebugger.cs`, `AdapterHubCallbackService.cs`, `AdapterLifetimeManagment.cs`, `AdapterBuilder.cs`

`Services/`:
- `PipelineRegistryService.cs`, `PollingService.cs`, `IPipelineRegistryService.cs`, `PollingItem.cs`, `IPollingService.cs`, `PipelineRegistration.cs`, `PipelineExecution.cs`, `PipelineTriggerExecutionException.cs`, `PollingHandle.cs`, `PipelineNodeExecutionException.cs`, `ExecutePipelineOptions.cs`, `IContextCreatorService.cs`, `PipelineExecutionException.cs`, `IPipelineExecutionReporter.cs`, `AdapterPipelineExecutionReporter.cs`, `DefaultContextCreatorService.cs`

- [ ] **Step 2: Read each file briefly** to classify:
  - **GREEN-style** (just remove `using Newtonsoft.*;` and run): probably the exception types, options, interfaces with no JToken signatures.
  - **YELLOW-style** (small JToken→JsonNode swap on the public surface): `IContextCreatorService`/`DefaultContextCreatorService` (creates an `IDataContext` from an input value, likely currently `JToken? input`), `AdapterPipelineExecutionReporter` (might serialize execution snapshots), `AdapterPipelineDebugger` (wraps the pipeline debugger).
  - **RED-style** (genuine logic-coupling that needs careful rework): `AdapterBuilder` (pipeline schema generation entry point — also see Task 6.8), `AdapterExecutionService` / `HostedAdapterExecutionService` (the adapter loop that drives pipelines).

- [ ] **Step 3: Write the classification into the plan** as you discover it. Add or amend tasks below as needed before starting Task 6A.2.

### Task 6A.2: Migrate Adapters/ — GREEN files

**Files:** the GREEN-classified files from Task 6A.1's enumeration.

- [ ] **Step 1: Remove the `<Compile Remove="Adapters/**/*.cs" />` line** from `Sdk.Common.csproj`. (We'll re-add narrower exclusions for any RED files that aren't ready yet.)
- [ ] **Step 2: Apply Task 6.1 transformation rules** to each GREEN file in turn.
- [ ] **Step 3: `dotnet build` clean** after each subset (e.g., commit per logical group).
- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/Adapters/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(adapter): migrate adapter exception, options, and interface types to STJ"
```

### Task 6A.3: Migrate Adapters/ — YELLOW and RED files

For each YELLOW/RED file from Task 6A.1's classification:

- [ ] **Step 1: Read the file.**
- [ ] **Step 2: Apply targeted refactor**:
  - For `AdapterPipelineDebugger`: thin wrapper over `IPipelineDebugger` — straightforward.
  - For `AdapterBuilder`: if it invokes `PipelineSchemaGenerator.Generate(...)`, ensure it passes through STJ-friendly types after Task 6.8.
  - For `HostedAdapterExecutionService` / `AdapterExecutionService`: the adapter loop calls `IEtlDataOrchestrator.RunAsync(...)` — update to pass `JsonElement?` initial values (or `null`).
- [ ] **Step 3: `dotnet build` clean.**
- [ ] **Step 4: Commit per logical unit (one file or pair per commit).**

### Task 6A.4: Migrate Services/ — GREEN files

**Files:** the GREEN-classified files from Services/ (likely options DTOs, exception types, interfaces).

- [ ] **Step 1: Remove the `<Compile Remove="Services/**/*.cs" />` line** from `Sdk.Common.csproj`.
- [ ] **Step 2: Apply Task 6.1 transformation rules** to GREEN files.
- [ ] **Step 3: `dotnet build` clean.**
- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/Services/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(services): migrate service interfaces and exception types to STJ"
```

### Task 6A.5: Migrate Services/ — YELLOW and RED files

Focus files (verify in Task 6A.1):
- `IContextCreatorService.cs` / `DefaultContextCreatorService.cs`: factory of `(IEtlContext, IDataContext)`. Likely takes an initial value as `JToken?`; change to `JsonElement?`.
- `PipelineRegistryService.cs`: tracks pipeline registrations. Likely uses a Newtonsoft serializer for caching configs.
- `AdapterPipelineExecutionReporter.cs`: reports execution results back to the controller. May serialize `IDataContext` snapshots — adopt `dataContext.Get<JsonNode>("$")` + `JsonSerializer.Serialize`.
- `PipelineExecution.cs` / `PipelineRegistration.cs`: data classes — likely just `using` cleanup.

- [ ] **Step 1: Read each.**
- [ ] **Step 2: Apply targeted refactor.**
- [ ] **Step 3: `dotnet build` clean.**
- [ ] **Step 4: Restore mirror test exclusions** in `Sdk.Common.Tests.csproj` (`<Compile Remove="Adapters/**/*.cs" />` and `<Compile Remove="Services/**/*.cs" />`). Migrate any tests that reference the migrated APIs.
- [ ] **Step 5: Commit per logical unit.**

### Task 6A.6: Migrate `octo-sdk` test fixtures and supporting files

**Files:**
- `tests/Sdk.Common.Tests/Fixtures/**/*.cs`
- `tests/Sdk.Common.Tests/TestData/**/*.cs`

After Phase 6A's production code migrates, the corresponding test fixtures (`<Compile Remove="Fixtures/**/*.cs" />` and `<Compile Remove="TestData/**/*.cs" />` in `Sdk.Common.Tests.csproj`) become re-includable.

- [ ] **Step 1: Restore both `<Compile Remove>` lines.**
- [ ] **Step 2: Migrate fixtures**: any `JToken`-typed sample data → `JsonNode?` or `JsonElement`; any custom `IDataContext` test doubles must implement the new path-only contract.
- [ ] **Step 3: Run all `Sdk.Common.Tests`** to verify the suite is green.
- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add tests/Sdk.Common.Tests/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "test: restore Sdk.Common.Tests fixtures and test data after Phase 6A migration"
```

After Phase 6A, `Sdk.Common.csproj`'s `<Compile Remove>` block contains only the trigger-context entries (Phase 6B).

---

## Phase 6B — Trigger contexts

**Goal:** restore the trigger-side context types (`ITriggerContext`, `ITriggerPipelineNode`, `TriggerContext`, `AdapterTriggerContext`). These are used by trigger nodes (`From*Node` in `Nodes/Triggers/` and `octo-mesh-adapter`'s `Nodes/Trigger/`) to start pipeline executions.

### Task 6B.1: Migrate `ITriggerContext`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/ITriggerContext.cs`

Inspection shows `ITriggerContext` has no Newtonsoft references — it uses `OctoObjectId`, `INodeContext`, `IGlobalConfiguration`, `ExecutePipelineOptions`. Once `INodeContext` is migrated (Task 2A.2) and `ExecutePipelineOptions` is restored (Task 6A.4), this file compiles unchanged.

- [ ] **Step 1: Remove the `<Compile Remove="EtlDataPipeline/ITriggerContext.cs" />` line.**
- [ ] **Step 2: Read the file**, confirm no Newtonsoft `using` lines.
- [ ] **Step 3: `dotnet build` clean.**
- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): restore ITriggerContext"
```

### Task 6B.2: Migrate `ITriggerPipelineNode`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/ITriggerPipelineNode.cs`

Likely defines `Task ProcessAsync(ITriggerContext ctx)` — a sibling of `IPipelineNode` for triggers.

- [ ] **Step 1: Remove the `<Compile Remove>` line.**
- [ ] **Step 2: Read the file.**
- [ ] **Step 3: Remove any `using Newtonsoft.*;` if present.** Likely no other changes.
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/ITriggerPipelineNode.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): restore ITriggerPipelineNode"
```

### Task 6B.3: Migrate `TriggerContext`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/TriggerContext.cs`

The default `ITriggerContext` implementation. Its `ExecuteAsync(ExecutePipelineOptions, object?)` accepts an arbitrary input — the legacy form likely converts that input to `JToken` before passing to the orchestrator.

- [ ] **Step 1: Remove the `<Compile Remove>` line.**
- [ ] **Step 2: Read the file.**
- [ ] **Step 3: Migrate**:
  - Replace `JToken.FromObject(input)` with `JsonSerializer.SerializeToElement(input, PipelineJsonOptions.Default)` — yields `JsonElement` that the orchestrator can consume directly.
  - Replace any internal `JToken`-typed plumbing with `JsonNode?` / `JsonElement`.
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: If `tests/Sdk.Common.Tests/EtlDataPipeline/AdapterTriggerContextTests.cs` covers this**, restore + run.
- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/TriggerContext.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate TriggerContext to STJ"
```

### Task 6B.4: Migrate `AdapterTriggerContext`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/AdapterTriggerContext.cs`

A specialization of `TriggerContext` that adds adapter-specific plumbing (hub callbacks, lifetime). Same migration shape as 6B.3.

- [ ] **Step 1: Remove the `<Compile Remove>` line.**
- [ ] **Step 2: Read the file** to understand the specialization.
- [ ] **Step 3: Apply migration** — same rules. Watch for any `JsonConvert.SerializeObject` calls when reporting trigger results to the hub.
- [ ] **Step 4: `dotnet build` clean.**
- [ ] **Step 5: Restore `tests/Sdk.Common.Tests/EtlDataPipeline/AdapterTriggerContextTests.cs`** from the test-side `<Compile Remove>` and run.
- [ ] **Step 6: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj src/Sdk.Common/EtlDataPipeline/AdapterTriggerContext.cs tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj tests/Sdk.Common.Tests/EtlDataPipeline/AdapterTriggerContextTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(pipeline): migrate AdapterTriggerContext to STJ"
```

After Phase 6B, the `<Compile Remove>` block in `Sdk.Common.csproj` is **empty**. This is the explicit acceptance criterion.

---

## Phase 7 — LiteDB BSON Converter Rewrite

### Task 7.1: Rewrite `LiteDbBsonConverter` for `JsonNode`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Buffering/LiteDbBsonConverter.cs`
- Add tests: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/Nodes/Buffering/LiteDbBsonConverterTests.cs`

- [ ] **Step 1: Write round-trip tests**

```csharp
using System.Text.Json.Nodes;
using LiteDB;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Buffering;

public class LiteDbBsonConverterTests
{
    [Theory]
    [InlineData("{\"a\": 1, \"b\": \"x\", \"c\": true, \"d\": null}")]
    [InlineData("[1, \"two\", false, null, {\"x\":1}]")]
    [InlineData("\"scalar\"")]
    [InlineData("42")]
    [InlineData("true")]
    public void RoundTrip_PreservesValue(string json)
    {
        var node = JsonNode.Parse(json);
        var bson = LiteDbBsonConverter.ToBson(node);
        var roundTripped = LiteDbBsonConverter.FromBson(bson);
        Assert.Equal(node?.ToJsonString(), roundTripped?.ToJsonString());
    }
}
```

- [ ] **Step 2: Implement** — see existing converter for a structural template; map `JsonObject` → `BsonDocument`, `JsonArray` → `BsonArray`, `JsonValue` → primitive `BsonValue` based on `GetValueKind()`. Reverse for `FromBson`. The existing converter has a working JToken-based design; the engineer mirrors it for JsonNode.

- [ ] **Step 3: Update `BufferNode` and `BufferRetrievalNode`** to call the new converter and use `Get<JsonNode>` / `Set` on the data context.

- [ ] **Step 4: Test all buffering node tests, commit**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~Buffering" -c DebugL
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/Nodes/Buffering tests/Sdk.Common.Tests/EtlDataPipeline/Nodes/Buffering/
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "refactor(buffering): rewrite LiteDbBsonConverter for JsonNode"
```

---

## Phase 8 — Sdk.Common.csproj Cleanup

### Task 8.1: Remove `Newtonsoft.Json` package reference

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/Sdk.Common.csproj`

- [ ] **Step 1: Remove the line `<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />`**

- [ ] **Step 2: Verify build is clean**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/Octo.Sdk.sln -c DebugL 2>&1 | tee /tmp/build.log
grep -c "error" /tmp/build.log
```

Expected: zero errors. If non-zero, fix lingering Newtonsoft references and rebuild.

- [ ] **Step 3: Confirm no `Newtonsoft` strings remain in non-test source**

```bash
rg "Newtonsoft" /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src
```

Expected: **only matches in `Communication.Contracts/` (out-of-scope DTOs)**. Pipeline code is clean.

- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "chore: drop Newtonsoft.Json from Sdk.Common"
```

### Task 8.2: Add Newtonsoft.Json to `Communication.Contracts.csproj` (compensating)

**Files:**
- Modify: `octo-sdk/src/Communication.Contracts/Communication.Contracts.csproj`

- [ ] **Step 1: Add direct reference if not already present**

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

(Only if `Communication.Contracts.csproj` previously relied on transitive Newtonsoft from `Sdk.Common`.)

- [ ] **Step 2: Verify build**

- [ ] **Step 3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Communication.Contracts/Communication.Contracts.csproj
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "chore(comm-contracts): direct ref Newtonsoft (preserve DTO contracts)"
```

---

## Phase 9 — octo-mesh-adapter Migration

### Task 9.0: Enumerate mesh-adapter scope beyond `Nodes/`

Inspection at plan-rev1 time revealed `octo-mesh-adapter/src/MeshAdapter.Sdk/` contains its own equivalents to several `octo-sdk` orchestration types — these consume the legacy IDataContext API and need migration too:

- `octo-mesh-adapter/src/MeshAdapter.Sdk/IMeshEtlContext.cs` — extends `IEtlContext` with mesh-specific repositories.
- `octo-mesh-adapter/src/MeshAdapter.Sdk/Services/MeshEtlContext.cs` — concrete impl, derives from `DefaultEtlContext`.
- `octo-mesh-adapter/src/MeshAdapter.Sdk/Services/MeshContextCreatorService.cs` — implements `IContextCreatorService`. Likely takes a `JToken? input`.
- `octo-mesh-adapter/src/MeshAdapter.Sdk/Services/MeshAdapterTriggerContext.cs` — derives from `AdapterTriggerContext`.
- `octo-mesh-adapter/src/MeshAdapter.Sdk/Services/ServiceAccountTokenService.cs` — likely no Newtonsoft, verify.
- `octo-mesh-adapter/src/MeshAdapter.Sdk/Services/HttpRequests/**/*.cs` — verify per-file.
- `octo-mesh-adapter/src/MeshAdapter.Sdk/MeshAdapterPipelineExecutionException.cs` — verify, likely just a typed exception.
- `octo-mesh-adapter/src/MeshAdapter.Sdk/Configuration/**/*.cs` — verify.
- `octo-mesh-adapter/src/MeshAdapter.Sdk/Middlewares/DynamicRouteMiddleware.cs` — likely uses HTTP middleware patterns; verify whether it serializes pipeline results via Newtonsoft.

- [ ] **Step 1: Run an enumeration pass**

```bash
rg -l "JToken|JObject|JArray|Newtonsoft\.Json" /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/src
```

For each hit outside `Nodes/`, classify (GREEN / YELLOW / RED) per Task 6A.1's scheme.

- [ ] **Step 2: Add a per-file sub-task to Phase 9** for any file outside `Nodes/` that needs more than a `using` cleanup. Subagents can execute these in parallel against each file.

### Task 9.1: Apply transformation pattern to all mesh-adapter nodes

**Files:** every `.cs` file under `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/`. The RED case (`UpdateRecordArrayItemNode`) is already done in Task 5.1.

- [ ] **Step 1: Use the transformation rules from Task 6.1 across the directory**
- [ ] **Step 2: Build mesh-adapter**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL
```

Expected: zero errors after migration is complete.

- [ ] **Step 3: Run mesh-adapter tests**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL
```

Resolve any test failures.

- [ ] **Step 4: Commit grouped logically (Extracts, Loads, Transforms, Triggers)**

### Task 9.2: Migrate non-`Nodes/` mesh-adapter files

Apply targeted refactor to each file enumerated in Task 9.0:

- `IMeshEtlContext.cs` / `MeshEtlContext.cs`: derives from migrated base — likely just `using` cleanup.
- `MeshContextCreatorService.cs`: change `JToken? input` parameter to `JsonElement? input`. The implementation calls `_etlContextFactory(...)` and `new DataContextImpl(input ?? ...)`.
- `MeshAdapterTriggerContext.cs`: derives from migrated `AdapterTriggerContext` — `using` cleanup plus any local JToken usages.
- `MeshAdapterPipelineExecutionException.cs`: likely typed-exception only.
- `Configuration/`, `Middlewares/`, `Services/HttpRequests/`: per-file refactor.

- [ ] **Step 1: For each file, apply Task 6.1 transformation rules**.
- [ ] **Step 2: `dotnet build` after each file** to keep diffs small.
- [ ] **Step 3: Commit grouped logically (Services, Configuration, Middlewares, etc.)**.

### Task 9.3: Confirm no Newtonsoft in mesh-adapter

```bash
rg "Newtonsoft" /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/src
```

Expected: empty.

- [ ] **Commit final cleanup if any**

### Task 9.5: Restore multi-match read semantics in anomaly nodes

The legacy implementation in `StatisticalAnomalyNode` and `MachineLearningAnomalyNode` used `dataContext.Current.SelectTokens(path)` to retrieve **multiple** JsonNodes from a JSONPath query (recursive-descent paths like `$..value` produce N matches). During Phase 9 the implementer reduced this to a single-match `Get<JsonArray>(...)` call without realizing a multi-match path was available. If production pipelines feed recursive-descent paths into these nodes, the current code silently produces wrong results.

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/StatisticalAnomalyNode.cs`
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/MachineLearningAnomalyNode.cs`
- Possibly modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext/IDataContext.cs`, `DataContextImpl.cs`, `DataContextChild.cs` (if option (3) is chosen)

- [ ] **Step 1: Inspect legacy semantics**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter log -p --follow -- src/MeshAdapter.Sdk/Nodes/Transform/StatisticalAnomalyNode.cs | head -200
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter log -p --follow -- src/MeshAdapter.Sdk/Nodes/Transform/MachineLearningAnomalyNode.cs | head -200
```

Confirm both files originally called `SelectTokens(path)` (multi-match) on `dataContext.Current`.

- [ ] **Step 2: Replace single-match read with multi-match read**

Two acceptable options:

1. **Direct evaluator call** — use `JsonPathEvaluator.Evaluate(rootElement, JsonPathParser.Parse(path))` to enumerate all matches. The evaluator is public in `Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath`. Get the root via `Get<JsonNode>("$", null)` materialized to a `JsonElement` (or use the context's existing root accessor if one exists internally).
2. **(Cleaner)** — add a new `IDataContext.GetMatches<T>(string jsonPath)` or `EnumerateMatches(string jsonPath)` method returning `IEnumerable<JsonNode>` / `IEnumerable<T>`. More discoverable than calling the evaluator directly. If chosen, add the method to `IDataContext`, `DataContextImpl`, and `DataContextChild`, and update both anomaly nodes to use it.

Prefer option (2) if production pipelines use recursive-descent paths in more than just these two nodes — the cost of one new API method is small and pays off across future nodes.

- [ ] **Step 3: Build and test mesh-adapter**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL
```

- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter add src/MeshAdapter.Sdk/Nodes/Transform/StatisticalAnomalyNode.cs src/MeshAdapter.Sdk/Nodes/Transform/MachineLearningAnomalyNode.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter commit -m "fix(anomaly-nodes): restore multi-match JSONPath read semantics"
```

---

## Phase 10 — Production Pipeline Refactor + End-to-End Validation

### Task 10.1: Refactor double-dot pipeline expression

**Files:**
- Modify: `deployment/energy-community-deployment/data/_calculation/energy-create-all-billing-documents.yaml`

- [ ] **Step 1: Locate the expression `$.key..billingDocument..Items[0].RtId` in the file**

```bash
grep -n "billingDocument..Items" /Users/reimar/dev/meshmakers/branches/main/deployment/energy-community-deployment/data/_calculation/energy-create-all-billing-documents.yaml
```

- [ ] **Step 2: Inspect the surrounding context to determine the actual document shape**

Read 30 lines around the match to understand what subtree the path traverses.

- [ ] **Step 3: Rewrite the path to a single-descent equivalent**

Example: if the actual shape is `key → billingDocument → Items`, replace `$.key..billingDocument..Items[0].RtId` with `$.key.billingDocument.Items[0].RtId`. If recursive descent is genuinely needed at one level, keep it once: `$.key..billingDocument.Items[0].RtId`.

- [ ] **Step 4: Commit in `energy-community-deployment`**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/deployment/energy-community-deployment add data/_calculation/energy-create-all-billing-documents.yaml
git -C /Users/reimar/dev/meshmakers/branches/main/deployment/energy-community-deployment commit -m "refactor(pipeline): replace double-dot mid-path with single-descent equivalent"
```

### Task 10.2: Run mesh-adapter against deployment pipelines

This is the acceptance gate: the new mesh-adapter binary must execute representative pipelines from both deployments without behavioral changes.

- [ ] **Step 1: Start required infrastructure**

```bash
pwsh -c '. /Users/reimar/dev/meshmakers/branches/main/octo-tools/modules/profile.ps1; Start-OctoInfrastructure'
```

- [ ] **Step 2: Build everything in dependency order**

```bash
pwsh -c '. /Users/reimar/dev/meshmakers/branches/main/octo-tools/modules/profile.ps1; Invoke-BuildAll -configuration DebugL'
```

- [ ] **Step 3: Run mesh-adapter integration tests**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/tests/MeshAdapter.IntegrationTests/MeshAdapter.IntegrationTests.csproj -c DebugL
```

- [ ] **Step 4: Smoke-test a representative pipeline from each deployment**

For each of: a maco pipeline (e.g., `pipeline_consumer.yaml`) and an EC pipeline (e.g., `handle-cm-rev-cus-pipeline.yaml`) — execute via the existing pipeline test harness or a one-off script, capture output, compare against a stored reference.

- [ ] **Step 5: Re-run the memory benchmark from Task 0.2 with the new implementation**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj \
  --filter "FullyQualifiedName~ForEachMemoryBenchmark" -c DebugL --logger "console;verbosity=normal"
```

Compare against the baseline recorded in `baseline-perf.txt`. Expected: order-of-magnitude reduction (e.g., GBs → tens-of-MBs).

- [ ] **Step 6: Update `baseline-perf.txt` with the new numbers and commit**

```bash
echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) post-migration: <paste output>" >> /Users/reimar/dev/meshmakers/branches/main/octo-sdk/docs/superpowers/plans/baseline-perf.txt
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add docs/superpowers/plans/baseline-perf.txt
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "docs: record post-migration memory benchmark"
```

---

## Phase 11 — Restore excluded unit tests

**Goal:** bring back the ~22 mesh-adapter unit test files plus the SDK-side node test files that were excluded during the migration because they used legacy mock patterns (FakeItEasy mocks of `dataContext.Current`, `GetSimpleValueByPath`, `GetComplexObjectByPath`, `SetValueByPath`, `SelectByPath`, `IsPathSimpleArrayValue`, etc.). Each test file gets a focused rewrite to match the new path-only `IDataContext` surface.

**Independence:** this phase is independent of Phase 10 and may land in a separate PR if the user prefers. Note this in the merge plan.

**Estimate:** 2–4 hours of focused work.

### Task 11.1: Enumerate excluded test files

The exact set of files is best determined at execution time — the migration left `<Compile Remove>` blocks in test csproj files that reference each excluded test file. Search both repos:

```bash
rg "GetSimpleValueByPath|GetComplexObjectByPath|SetValueByPath|SelectByPath|IsPathSimpleArrayValue|\.Current\b" /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests
rg "GetSimpleValueByPath|GetComplexObjectByPath|SetValueByPath|SelectByPath|IsPathSimpleArrayValue|\.Current\b" /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/tests
```

Also inspect the `<Compile Remove>` blocks in:
- `octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj`
- `octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj`

Approximate count: **~22 mesh-adapter files + N octo-sdk files** (enumerate at execution time; do not pre-list every file in the plan).

- [ ] **Step 1: Produce a working list** of excluded test files from both csproj exclusion blocks.
- [ ] **Step 2: Group files by test pattern** (mocked `Current`, mocked `GetSimpleValueByPath<T>`, mocked `SetValueByPath`, mocked `SelectByPath`, mocked iteration helpers, etc.) so the rewrite can apply a consistent transformation per group.

### Task 11.2: Apply rewrite patterns per file

For each test file:

1. Remove the `<Compile Remove>` line in the corresponding csproj.
2. Apply the rewrite patterns below.
3. Run the file's tests in isolation: `dotnet test --filter "FullyQualifiedName~<TestClass>" -c DebugL`.
4. Fix anything broken.
5. Commit (group logically — e.g., per test class or per node category).

**Rewrite patterns:**

- `A.CallTo(() => ctx.GetSimpleValueByPath<int>("$.x")).Returns(42)` → `A.CallTo(() => ctx.Get<int>("$.x", null)).Returns(42)`
- `A.CallTo(() => ctx.GetComplexObjectByPath<MyDto>("$.x")).Returns(dto)` → `A.CallTo(() => ctx.Get<MyDto>("$.x", null)).Returns(dto)`
- `A.CallTo(() => ctx.SetValueByPath("$.x", 42, ...))` → `A.CallTo(() => ctx.Set("$.x", 42, ...))` (signature may differ — see new `IDataContext`)
- `A.CallTo(() => ctx.SelectByPath("$.items[*]"))` → either `A.CallTo(() => ctx.Get<JsonArray>("$.items[*]", null))` for single-match OR (post-Task-9.5) `A.CallTo(() => ctx.GetMatches<JsonNode>("$.items[*]"))` for multi-match.
- `A.CallTo(() => ctx.Current).Returns(jObject)` → typically replaced by mocking specific path reads (`Get<JsonNode>("$")`, `Get<JsonObject>("$")`) OR by constructing a real `DataContextImpl` with test data and dropping the mock entirely. Prefer the real-context approach when the test exercises more than a single path; mocks become brittle once five-plus paths are involved.
- `A.CallTo(() => ctx.IsPathSimpleArrayValue("$.x")).Returns(true)` → if the new API exposes an equivalent, mock that; otherwise use a real context with array-shaped data and remove the predicate from the assertion.

- [ ] **Step 1: For each group from Task 11.1, draft the substitution**.
- [ ] **Step 2: Apply per file, build, and test**.
- [ ] **Step 3: Commit logically grouped**.

### Task 11.3: Verify both `<Compile Remove>` blocks are empty

After Task 11.2, the test-side `<Compile Remove>` blocks must be fully drained:

```bash
grep -n "Compile Remove" /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj
grep -n "Compile Remove" /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj
```

Expected: empty (no migration-era exclusion blocks remain).

- [ ] **Step 1: Run full test suite for both repos**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/Octo.Sdk.sln -c DebugL
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL
```

- [ ] **Step 2: Commit any final csproj cleanup**.

---

## Self-Review

- **Spec coverage (rev1):**
  - §5 layered context — Phase 2 (Tasks 2.1–2.5), Phase 2A (orchestration scaffolding restoring the consumers of `IDataContext`).
  - §5.1 read–merge invariant — Task 2.2 tests directly for ancestor-read-after-descendant-write.
  - §5.2 zero-copy iteration — Tasks 2.4 (alias-based child contexts), 4.1–4.4 (rewritten iteration nodes).
  - §5.3 path-only API — Task 2.1 (`IDataContext`), Tasks 2.3–2.5 (DataContextImpl + Child); Phase 2A migrates the orchestrator and node bases to consume only the path-only surface.
  - §6 custom evaluator — Phase 1 (Tasks 1.1–1.11).
  - §6.5 double-dot refactor — Task 10.1.
  - §7.1 RED node — Task 5.1.
  - §7.2 YELLOW nodes — Tasks 5.2, 5.3.
  - §7.3 GREEN nodes — Phase 6 (Tasks 6.2–6.5), plus newly-explicit Tasks 6.6–6.12 covering configuration serializers, the polymorphic discriminator/converter, schema generation, debugger, DI registration, and `JTokenExtensions` deletion.
  - §7.4 iteration nodes — Phase 4.
  - §8 LiteDB converter — Task 7.1.
  - §9 parity harness — Phase 3 (Tasks 3.1–3.5).
  - §10 sequencing — phases mirror the spec's ordered steps; rev1 inserts Phases 2A, 2B, 6A, 6B between existing phases without renumbering.
  - §11 risk: subtree-lifting correctness — Task 2.2 covers; lift-on-first-write strategy makes the implementation trivially correct at the cost of the first-write allocation (acceptable per spec since iteration child contexts get fresh overlays).
  - **NEW (rev1) — DataPipelineException port:** Phase 2B replaces the InvalidOperationException placeholders introduced in Task 2.3 with the typed exception family, preserving the existing API contract that nodes throw `DataPipelineException`-derived errors on configuration / value-shape mismatch.
  - **NEW (rev1) — Adapters/Services/TriggerContext migration:** Phase 6A migrates `Adapters/` and `Services/`, Phase 6B migrates trigger-side context types. These weren't in the original spec because the spec focused on the JSON-handling rewrite; they are mechanical follow-on work necessary to ship.

- **Phase / task summary (rev2):**
  - Phase 0 (2 tasks)
  - Phase 1 (11 tasks: 1.1–1.11)
  - Phase 2 (5 tasks: 2.1–2.5)
  - **Phase 2A (7 tasks: 2A.1–2A.7) — NEW in rev1**
  - **Phase 2B (1 task: 2B.1) — NEW in rev1**
  - Phase 3 (5 tasks: 3.1–3.5)
  - Phase 4 (4 tasks: 4.1–4.4)
  - Phase 5 (3 tasks: 5.1–5.3)
  - Phase 6 (12 tasks: 6.1–6.12; 6.6–6.12 NEW in rev1)
  - **Phase 6A (6 tasks: 6A.1–6A.6) — NEW in rev1**
  - **Phase 6B (4 tasks: 6B.1–6B.4) — NEW in rev1**
  - Phase 7 (1 task: 7.1)
  - Phase 8 (2 tasks: 8.1–8.2)
  - Phase 9 (5 tasks: 9.0–9.3 + **9.5 NEW in rev2**; 9.0 / 9.2 were NEW in rev1)
  - Phase 10 (2 tasks: 10.1–10.2)
  - **Phase 11 (3 tasks: 11.1–11.3) — NEW in rev2**
  - **Total: ~73 tasks** (was 42 in rev0, ~69 in rev1; rev2 adds Task 9.5 and three Phase 11 tasks for anomaly multi-match correctness and excluded-test restoration).

- **Rev2 summary of changes:**
  - **Task 9.5** — fixes a correctness regression in `StatisticalAnomalyNode` and `MachineLearningAnomalyNode`: legacy `SelectTokens(path)` multi-match semantics were inadvertently reduced to single-match `Get<JsonArray>` during Phase 9. Restore via direct `JsonPathEvaluator` call or a new `IDataContext.GetMatches<T>` helper.
  - **Phase 11** — restores the ~22 mesh-adapter unit test files plus several octo-sdk node test files that were excluded with `<Compile Remove>` blocks during the migration because they mocked legacy `IDataContext` members (`Current`, `GetSimpleValueByPath`, `SelectByPath`, etc.). Independent of Phase 10 — may ship in a separate PR. Estimate 2–4 hours.
  - **Deferred perf note** — `CreateSubContext` β/γ zero-copy optimization deferred until the Phase 10 post-migration benchmark indicates whether per-iteration allocation matters against the 924 MB baseline.

- **Placeholder scan (rev1):** intentional placeholders remain:
  - Task 2.5 description ("Fill in remaining child-context methods (TDD-driven)") — bounded; the engineer follows the established pattern.
  - Task 4.2 is conditional on Task 4.1 surfacing a need.
  - Task 5.2 / 5.3 say "follow the pattern in §7.2 of the spec".
  - Task 6A.1 enumerates files but defers GREEN/YELLOW/RED classification to execution-time inspection of each file. Tasks 6A.2–6A.5 reference the classification produced in 6A.1.
  - Task 9.0 enumerates mesh-adapter non-`Nodes/` files; Task 9.2 references the per-file classification produced in 9.0.
  These are bounded and non-blocking.

- **Open questions raised in rev1:**
  - **`NJsonSchema` transitive Newtonsoft dep** (Task 6.8): default decision recorded as "accept the transitive Newtonsoft dep, since `NJsonSchema` is the in-tree schema lib and a switch to a STJ-native lib (`JsonSchema.Net`) is a separate change". The plan flags this and produces an audit step (regen `pipeline-schema.json`, diff against pre-migration). If the user wants a clean STJ-only build, an additional task is needed.
  - **`octo-mesh-adapter` Configuration / Middlewares scope** (Task 9.0/9.2): not yet enumerated file-by-file because the rev1 author didn't open the legacy commits to inspect; the plan deliberately defers this enumeration to execution time.

  **Deferred perf optimization — `CreateSubContext` zero-copy via alias-based child contexts.**
  The current `NodeContext.CreateSubContext` uses an optimized JsonNode-to-JsonElement materialization (no UTF-16 round-trip) but still allocates per call to preserve isolation semantics. The full zero-copy ideal (option β: parent-fallback aware via `CreateIterationChild` with `"$"` alias, OR option γ: explicit isolation flag on `CreateIterationChild`) was deferred pending Phase 10's post-migration benchmark. Decide whether to invest after measuring whether `ForNode`/`SwitchNode` per-iteration allocation actually shows up in the new memory profile vs. the 924 MB baseline.

- **Type consistency:** `IDataContext` signatures in Task 2.1 and the implementations in Task 2.3 use identical method names and types. `DocumentModes` / `ValueKinds` / `TargetValueWriteModes` are reused unchanged from the existing codebase to minimize churn — confirmed they exist in `Sdk.Common/EtlDataPipeline/`. The typed exception factory contract from `DataPipelineException` (Phase 2B) is preserved across the migration; only factories taking `JToken` change signature.

- **Three known plan-level tradeoffs documented:**
  - Lift-on-first-write strategy (Task 2.2) materializes the full base on first write to a context. Iteration child contexts have their own overlays — they don't inherit parent's lifted state — so the parent lifts at most once and children lift only their own (small) per-iteration data. The dominant memory savings come from the alias mechanism in Task 2.4, not from incremental lifting.
  - Task 6.1's transformation rules are general; specific edge cases per node may need manual judgment.
  - **NEW (rev1):** `NJsonSchema` keeps a transitive Newtonsoft dep; pipeline runtime is Newtonsoft-free, but `Sdk.Common.csproj` still references `NJsonSchema` for build-time pipeline-schema.json generation. Acceptance recorded in Task 6.8.

---

## Execution Handoff

**Plan complete and saved to `/Users/reimar/dev/meshmakers/branches/main/octo-sdk/docs/superpowers/plans/2026-05-06-newtonsoft-to-stj-pipeline-migration.md`.**

Two execution options:

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration. Good for the long mechanical phases (6 and 9) where each task is well-defined.

**2. Inline Execution** — Execute tasks in this session using `superpowers:executing-plans`, batch execution with checkpoints for review.

Which approach?
