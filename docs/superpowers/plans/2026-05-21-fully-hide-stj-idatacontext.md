# Fully Encapsulate System.Text.Json behind `IDataContext` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove System.Text.Json from the ETL node-author surface (read **and** write) and consolidate the reinvented JSON glue into one canonical home each, without regressing the zero-copy memory profile.

**Architecture:** Bottom-up across three repos that consume each other via `../nuget` (`DebugL`, `999.0.0`): add the scalar-boxing primitive in `octo-construction-kit-engine`; add the STJ-free surface (`GetValue`/`TryGet<T>`/`Select`/`SelectMatches`) to `IDataContext` in `octo-sdk` and route SDK nodes through it; migrate `octo-mesh-adapter` nodes onto the surface and rewrite report builders as typed records. Every behavior is pinned by a characterization test **before** it is touched, and the existing peak-heap benchmark gates the merge.

**Tech Stack:** .NET 10 (`net10.0` + `netstandard2.0`), System.Text.Json (`JsonElement`/`JsonNode`), xUnit v3 + FluentAssertions + FakeItEasy, PowerShell `octo-tools` profile, `DebugL` build configuration.

**Spec:** `octo-sdk/docs/superpowers/specs/2026-05-21-fully-hide-stj-idatacontext-design.md`

**Repo paths:**
- ck-engine: `/Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine`
- sdk: `/Users/reimar/dev/meshmakers/branches/main/octo-sdk`
- mesh-adapter: `/Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter`

**Conventions for every task below:**
- Build a single repo + publish its NuGet: `Invoke-Build -repositoryPath ./<repo> -configuration DebugL` (pwsh, profile loaded once via `. ./octo-tools/modules/profile.ps1`).
- Run one test class: `dotnet test <repo>/<sln> -c DebugL --filter "FullyQualifiedName~<ClassName>"`.
- Use `git -C <repo>`; never `cd`. Do not chain shell commands with `&&`/`;`.
- After any `octo-sdk` public-API change, rebuild+pack sdk before building mesh-adapter so `../nuget` is fresh.

---

## Phase 0 — Safety net & baselines

### Task 0: Backup branches + capture baselines

**Files:** none (git refs + benchmark output)

- [ ] **Step 1: Confirm clean working trees on all three repos**

Run: `git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine status --short`
Run: `git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk status --short`
Run: `git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter status --short`
Expected: no output on all three. If not clean, stop and surface it.

- [ ] **Step 2: Create backup refs**

Run: `git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine branch backup/pre-stj-encapsulation-2026-05-21 dev/newtonsoft-to-stj-pipeline`
Run: `git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk branch backup/pre-stj-encapsulation-2026-05-21 dev/newtonsoft-to-stj-pipeline`
Run: `git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter branch backup/pre-stj-encapsulation-2026-05-21 dev/newtonsoft-to-stj-pipeline`

- [ ] **Step 3: Capture the peak-heap benchmark baseline (the zero-copy guardrail)**

Run the existing ForEach memory benchmark (the one behind `octo-sdk/docs/superpowers/plans/baseline-perf.txt`). Locate it:
Run: `git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk grep -l "peak" -- 'tests/**/*Benchmark*.cs' 'tests/**/*Perf*.cs'`
Run the benchmark per its csproj and **save the peak-managed-heap numbers** for the deeply-nested-ForEach-over-large-data case into `octo-sdk/docs/superpowers/plans/baseline-perf.txt` under a new dated section `## 2026-05-21 pre-encapsulation`.
Expected: numbers recorded. These are the gate for Task 30.

- [ ] **Step 4: Full baseline test sweep (capture current pass counts)**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine -c DebugL --filter "FullyQualifiedName!~SystemTests"`
Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/Octo.Sdk.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"`
Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"`
Expected: all green. Record the counts. No task may end below these counts.

---

## Phase 1 — ck-engine: the `JsonScalar` primitive (single source for Cluster A)

### Task 1: Characterize `RtAttributesConverter.MaterializeValue` scalar boxing (pin current behavior)

**Files:**
- Test: `octo-construction-kit-engine/tests/Runtime.Contracts.Tests/Serialization/RtAttributesConverterCharacterizationTests.cs` (create; match the existing test project — confirm its name with `git -C octo-construction-kit-engine grep -l "RtAttributesConverter" -- 'tests/**'`)

- [ ] **Step 1: Write characterization tests capturing the CURRENT boxing rules**

```csharp
using System.Text.Json;
using FluentAssertions;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Xunit;

namespace Runtime.Contracts.Tests.Serialization;

public class RtAttributesConverterCharacterizationTests
{
    // Deserialize an RtEntity whose single attribute holds the given raw JSON value,
    // then read the materialized CLR attribute value back out.
    private static object? RoundTripAttribute(string rawJsonValue)
    {
        var json = $$"""{"CkTypeId":"Test-1.0.0/T","Attributes":{"a":{{rawJsonValue}}}}""";
        var entity = JsonSerializer.Deserialize<RtEntity>(json,
            Meshmakers.Octo.Runtime.Contracts.Serialization.RtSystemTextJsonSerializer.Default)!;
        return entity.Attributes["a"];
    }

    [Fact] public void Integer_becomes_long() => RoundTripAttribute("42").Should().BeOfType<long>().And.Be(42L);
    [Fact] public void Real_becomes_double() => RoundTripAttribute("3.5").Should().BeOfType<double>().And.Be(3.5);
    [Fact] public void IntegerValuedReal_stays_double() => RoundTripAttribute("2.0").Should().BeOfType<double>();
    [Fact] public void IsoString_becomes_DateTime() => RoundTripAttribute("\"2026-05-21T00:00:00Z\"").Should().BeOfType<DateTime>();
    [Fact] public void NonDateString_stays_string() => RoundTripAttribute("\"hello\"").Should().BeOfType<string>().And.Be("hello");
    [Fact] public void Bool_becomes_bool() => RoundTripAttribute("true").Should().BeOfType<bool>().And.Be(true);
    [Fact] public void Null_becomes_null() => RoundTripAttribute("null").Should().BeNull();
}
```

- [ ] **Step 2: Run — expected PASS (these pin existing behavior)**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine -c DebugL --filter "FullyQualifiedName~RtAttributesConverterCharacterizationTests"`
Expected: all PASS. If any fail, the assumed boxing rules are wrong — read `RtAttributesConverter.MaterializeValue` and correct the test to match reality before continuing.

- [ ] **Step 3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine add tests/Runtime.Contracts.Tests/Serialization/RtAttributesConverterCharacterizationTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine commit -m "$(cat <<'EOF'
test(serialization): characterize RtAttributesConverter scalar boxing

Pins the JValue-parity boxing rules (int->long, real->double, ISO->DateTime,
else string) before extracting them into JsonScalar.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 2: Add `JsonScalar.ToClr` (extract the boxing logic)

**Files:**
- Create: `octo-construction-kit-engine/src/Runtime.Contracts/Serialization/JsonScalar.cs`
- Test: `octo-construction-kit-engine/tests/Runtime.Contracts.Tests/Serialization/JsonScalarTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Text.Json;
using FluentAssertions;
using Meshmakers.Octo.Runtime.Contracts.Serialization;
using Xunit;

namespace Runtime.Contracts.Tests.Serialization;

public class JsonScalarTests
{
    private static object? ToClr(string rawJson, bool parseDates = true)
    {
        using var doc = JsonDocument.Parse(rawJson);
        return JsonScalar.ToClr(doc.RootElement, parseDates);
    }

    [Fact] public void Integer_is_long() => ToClr("42").Should().BeOfType<long>().And.Be(42L);
    [Fact] public void Real_is_double() => ToClr("3.5").Should().BeOfType<double>().And.Be(3.5);
    [Fact] public void IntegerValuedReal_is_double() => ToClr("2.0").Should().BeOfType<double>();
    [Fact] public void IsoString_is_DateTime_when_parseDates_true() => ToClr("\"2026-05-21T00:00:00Z\"").Should().BeOfType<DateTime>();
    [Fact] public void IsoString_is_string_when_parseDates_false() => ToClr("\"2026-05-21T00:00:00Z\"", parseDates: false).Should().BeOfType<string>();
    [Fact] public void NonDateString_is_string() => ToClr("\"hello\"").Should().Be("hello");
    [Fact] public void True_is_bool() => ToClr("true").Should().Be(true);
    [Fact] public void Null_is_null() => ToClr("null").Should().BeNull();
    [Fact] public void Object_is_null() => ToClr("{\"x\":1}").Should().BeNull();
    [Fact] public void Array_is_null() => ToClr("[1,2]").Should().BeNull();
}
```

- [ ] **Step 2: Run — expected FAIL (JsonScalar does not exist)**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine -c DebugL --filter "FullyQualifiedName~JsonScalarTests"`
Expected: compile error / type not found.

- [ ] **Step 3: Implement `JsonScalar`**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Meshmakers.Octo.Runtime.Contracts.Serialization;

/// <summary>
/// Converts a scalar JSON value to its Newtonsoft-parity CLR boxing — the single source
/// of the "JValue.Value" rules previously hand-rolled across many pipeline nodes and inside
/// <see cref="RtAttributesConverter"/>. Integers box to <see cref="long"/>, reals to
/// <see cref="double"/>, ISO-8601 strings to <see cref="DateTime"/> (when requested), bools to
/// <see cref="bool"/>; objects/arrays return null (callers navigate those structurally).
/// </summary>
public static class JsonScalar
{
    /// <summary>Newtonsoft-parity scalar boxing of <paramref name="element"/>.</summary>
    public static object? ToClr(JsonElement element, bool parseDateStrings = true)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                // Mirror Newtonsoft DateParseHandling.DateTime: ISO-8601 -> DateTime, else string.
                return parseDateStrings && element.TryGetDateTime(out var dt) ? dt : element.GetString();
            case JsonValueKind.Number:
                // Explicit if/return, NOT a ternary: a `long : double` conditional has common type
                // double and would widen every integer to double before boxing.
                if (element.TryGetInt64(out var l)) return l;
                return element.GetDouble();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            default:
                return null; // Object / Array / Null / Undefined
        }
    }

    /// <summary>Newtonsoft-parity scalar boxing of a <see cref="JsonValue"/>.</summary>
    public static object? ToClr(JsonValue value, bool parseDateStrings = true) =>
        ToClr(value.GetValue<JsonElement>(), parseDateStrings);
}
```

- [ ] **Step 4: Run — expected PASS**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine -c DebugL --filter "FullyQualifiedName~JsonScalarTests"`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine add src/Runtime.Contracts/Serialization/JsonScalar.cs tests/Runtime.Contracts.Tests/Serialization/JsonScalarTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine commit -m "$(cat <<'EOF'
feat(serialization): add JsonScalar.ToClr — single-source Newtonsoft-parity boxing

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 3: Add `JsonScalar.TryToNumber<T>` (replaces AnomalyNodeHelpers.TryReadNumeric)

**Files:**
- Modify: `octo-construction-kit-engine/src/Runtime.Contracts/Serialization/JsonScalar.cs`
- Test: `octo-construction-kit-engine/tests/Runtime.Contracts.Tests/Serialization/JsonScalarTests.cs`

- [ ] **Step 1: Add failing tests**

```csharp
    [Fact]
    public void TryToNumber_reads_json_number()
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse("3.5")!;
        JsonScalar.TryToNumber<double>(node, out var v).Should().BeTrue();
        v.Should().Be(3.5);
    }

    [Fact]
    public void TryToNumber_parses_numeric_string_invariant()
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse("\"3.5\"")!;
        JsonScalar.TryToNumber<double>(node, out var v).Should().BeTrue();
        v.Should().Be(3.5);
    }

    [Fact]
    public void TryToNumber_rejects_non_numeric_string()
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse("\"banana\"")!;
        JsonScalar.TryToNumber<double>(node, out _).Should().BeFalse();
    }
```

- [ ] **Step 2: Run — expected FAIL**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine -c DebugL --filter "FullyQualifiedName~JsonScalarTests.TryToNumber"`
Expected: method not found.

- [ ] **Step 3: Add `TryToNumber<T>` to `JsonScalar` (copy the proven AnomalyNodeHelpers logic verbatim)**

```csharp
    /// <summary>
    /// Reads <paramref name="node"/> as numeric <typeparamref name="T"/>. Accepts JSON numbers
    /// natively; parses JSON strings under invariant culture. Returns false otherwise.
    /// </summary>
    public static bool TryToNumber<T>(System.Text.Json.Nodes.JsonNode node, out T value)
        where T : struct, IParsable<T>
    {
        try
        {
            value = node.GetValue<T>();
            return true;
        }
        catch (FormatException)
        {
            value = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            if (node is System.Text.Json.Nodes.JsonValue jv
                && jv.TryGetValue<string>(out var s)
                && T.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
            value = default;
            return false;
        }
    }
```

- [ ] **Step 4: Run — expected PASS**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine -c DebugL --filter "FullyQualifiedName~JsonScalarTests"`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine commit -m "$(cat <<'EOF'
feat(serialization): add JsonScalar.TryToNumber<T> (tolerant numeric read)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 4: Route `RtAttributesConverter.MaterializeValue` through `JsonScalar`

**Files:**
- Modify: `octo-construction-kit-engine/src/Runtime.Contracts/Serialization/RtAttributesConverter.cs` (the `MaterializeValue` `String`/`Number`/`True`/`False` arms)

- [ ] **Step 1: Replace the scalar arms with `JsonScalar.ToClr`**

In `MaterializeValue`, keep the `Object` (CkRecordId → RtRecord / nested map) and `Array` recursion unchanged. Replace the `String`, `Number`, `True`/`False`, `Null`/`Undefined` arms with a single delegation:

```csharp
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return element.TryGetProperty("CkRecordId", out _)
                    ? element.Deserialize<RtRecord>(options)
                    : MaterializeObject(element, options);
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray()) list.Add(MaterializeValue(item, options));
                return list;
            default:
                // Scalars (string/number/bool/null) box via the shared primitive.
                return JsonScalar.ToClr(element, parseDateStrings: true);
        }
```

- [ ] **Step 2: Run the characterization tests from Task 1 — expected PASS (unchanged behavior)**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine -c DebugL --filter "FullyQualifiedName~RtAttributesConverterCharacterizationTests"`
Expected: all PASS — behavior identical, logic now single-sourced.

- [ ] **Step 3: Full ck-engine test sweep**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine -c DebugL --filter "FullyQualifiedName!~SystemTests"`
Expected: at or above the Task 0 baseline count.

- [ ] **Step 4: Build + pack ck-engine to ../nuget**

Run: `Invoke-Build -repositoryPath ./octo-construction-kit-engine -configuration DebugL`
Expected: success; `Meshmakers.Octo.ConstructionKit.*` `999.0.0` refreshed.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-construction-kit-engine commit -m "$(cat <<'EOF'
refactor(serialization): RtAttributesConverter scalar arms via JsonScalar

Single-sources the JValue-parity boxing; characterization tests confirm
identical behavior.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2 — octo-sdk: the STJ-free `IDataContext` surface

> All new `IDataContext` members must be implemented on **both** `DataContextImpl` and its nested `DataContextChild` (see `src/Sdk.Common/EtlDataPipeline/DataContext.cs`). Read-side additions must use the `JsonElement` zero-copy path (no `JsonNode.Parse` per match) per spec §7.

### Task 5: Add `object? GetValue(string path, bool parseDateStrings = true)` to `IDataContext`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/IDataContext.cs`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext.cs` (`DataContextImpl` + `DataContextChild`)
- Test: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/DataContextGetValueTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Text.Json;
using FluentAssertions;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class DataContextGetValueTests
{
    private static IDataContext Ctx(string json) => new DataContextImpl(JsonDocument.Parse(json));

    [Fact] public void Integer_is_long() => Ctx("""{"a":42}""").GetValue("$.a").Should().BeOfType<long>().And.Be(42L);
    [Fact] public void Real_is_double() => Ctx("""{"a":3.5}""").GetValue("$.a").Should().BeOfType<double>().And.Be(3.5);
    [Fact] public void IsoString_is_DateTime() => Ctx("""{"a":"2026-05-21T00:00:00Z"}""").GetValue("$.a").Should().BeOfType<DateTime>();
    [Fact] public void String_stays_string_when_parseDates_false() => Ctx("""{"a":"2026-05-21T00:00:00Z"}""").GetValue("$.a", parseDateStrings: false).Should().BeOfType<string>();
    [Fact] public void Bool_is_bool() => Ctx("""{"a":true}""").GetValue("$.a").Should().Be(true);
    [Fact] public void Missing_is_null() => Ctx("""{"a":1}""").GetValue("$.missing").Should().BeNull();
    [Fact] public void Object_is_null() => Ctx("""{"a":{"b":1}}""").GetValue("$.a").Should().BeNull();
}
```

- [ ] **Step 2: Run — expected FAIL (method not found)**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~DataContextGetValueTests"`
Expected: compile error.

- [ ] **Step 3: Add the interface member**

In `IDataContext.cs`, in the reads region:

```csharp
    /// <summary>
    /// Reads the value at <paramref name="path"/> as its natural CLR scalar
    /// (bool / long / double / DateTime / string / null), via the shared JsonScalar rules.
    /// Object and array kinds return null — navigate those via <see cref="Select"/> or <c>Get&lt;T&gt;</c>.
    /// </summary>
    object? GetValue(string path, bool parseDateStrings = true);
```

- [ ] **Step 4: Implement on `DataContextImpl` (zero-copy: read off the JsonElement when not lifted)**

Add `using Meshmakers.Octo.Runtime.Contracts.Serialization;` then:

```csharp
    /// <inheritdoc />
    public object? GetValue(string path, bool parseDateStrings = true)
    {
        if (!_overlay.HasWrites)
        {
            var match = JsonPathEvaluator.Evaluate(_base, JsonPathParser.Parse(path)).FirstOrDefault();
            return match.CanonicalPath is null ? null : JsonScalar.ToClr(match.Element, parseDateStrings);
        }
        var node = GetAsNode(path);
        return node is JsonValue jv ? JsonScalar.ToClr(jv, parseDateStrings) : null;
    }
```

- [ ] **Step 5: Implement on `DataContextChild`**

```csharp
        public object? GetValue(string path, bool parseDateStrings = true)
        {
            var node = GetAsNode(path);
            return node is JsonValue jv ? JsonScalar.ToClr(jv, parseDateStrings) : null;
        }
```

- [ ] **Step 6: Run — expected PASS**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~DataContextGetValueTests"`
Expected: all PASS.

- [ ] **Step 7: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
feat(datacontext): add GetValue(path) — natural CLR scalar via JsonScalar

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 6: Add `bool TryGet<T>(string path, out T? value)` to `IDataContext`

**Files:**
- Modify: `IDataContext.cs`, `DataContext.cs` (both implementations)
- Test: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/DataContextTryGetTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System.Text.Json;
using FluentAssertions;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class DataContextTryGetTests
{
    private static IDataContext Ctx(string json) => new DataContextImpl(JsonDocument.Parse(json));

    [Fact] public void Present_returns_true_and_value()
    {
        Ctx("""{"a":5}""").TryGet<int>("$.a", out var v).Should().BeTrue();
        v.Should().Be(5);
    }
    [Fact] public void Missing_returns_false()
    {
        Ctx("""{"a":5}""").TryGet<int>("$.missing", out _).Should().BeFalse();
    }
    [Fact] public void ExplicitNull_returns_true_with_default()
    {
        Ctx("""{"a":null}""").TryGet<int?>("$.a", out var v).Should().BeTrue();
        v.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run — expected FAIL**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~DataContextTryGetTests"`
Expected: compile error.

- [ ] **Step 3: Add interface member + both implementations**

`IDataContext.cs`:
```csharp
    /// <summary>
    /// Reads <paramref name="path"/> as <typeparamref name="T"/>. Returns false when the path is
    /// absent (distinguishing missing from a present default), true otherwise (including explicit null).
    /// </summary>
    bool TryGet<T>(string path, out T? value);
```

`DataContextImpl` and `DataContextChild` (identical body):
```csharp
        public bool TryGet<T>(string path, out T? value)
        {
            if (!Exists(path)) { value = default; return false; }
            value = Get<T>(path);
            return true;
        }
```

- [ ] **Step 4: Run — expected PASS**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~DataContextTryGetTests"`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
feat(datacontext): add TryGet<T> distinguishing missing from default

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 7: Add `Select` + `SelectMatches` returning `IDataContext` (zero-copy views)

**Files:**
- Modify: `IDataContext.cs`, `DataContext.cs` (both implementations)
- Test: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/DataContextSelectTests.cs`

- [ ] **Step 1: Write failing tests (incl. detached-write semantics)**

```csharp
using System.Text.Json;
using FluentAssertions;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Xunit;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class DataContextSelectTests
{
    private static IDataContext Ctx(string json) => new DataContextImpl(JsonDocument.Parse(json));

    [Fact] public void Select_returns_subcontext_rooted_at_path()
    {
        var sub = Ctx("""{"a":{"b":7}}""").Select("$.a");
        sub.Should().NotBeNull();
        sub!.Get<int>("$.b").Should().Be(7);
    }

    [Fact] public void Select_missing_returns_null() => Ctx("""{"a":1}""").Select("$.missing").Should().BeNull();

    [Fact] public void SelectMatches_yields_one_context_per_match()
    {
        var matches = Ctx("""{"items":[{"v":1},{"v":2}]}""").SelectMatches("$.items[*]").ToList();
        matches.Select(m => m.Get<int>("$.v")).Should().Equal(1, 2);
    }

    [Fact] public void SelectMatches_results_are_detached_writes_do_not_merge_back()
    {
        var ctx = Ctx("""{"items":[{"v":1}]}""");
        foreach (var m in ctx.SelectMatches("$.items[*]")) m.Set("$.v", 99);
        ctx.Get<int>("$.items[0].v").Should().Be(1); // unchanged
    }
}
```

- [ ] **Step 2: Run — expected FAIL**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~DataContextSelectTests"`
Expected: compile error.

- [ ] **Step 3: Add interface members**

```csharp
    /// <summary>Returns a sub-context rooted at <paramref name="path"/>, or null if absent.
    /// The returned context is a non-owning read view; its Dispose is a no-op.</summary>
    IDataContext? Select(string path);

    /// <summary>Returns a detached, read-oriented sub-context per match of <paramref name="jsonPath"/>.
    /// Writes to a returned context do NOT merge back (use <see cref="UpdateMatchesAsync"/> for that).
    /// Replaces the former EnumerateMatches JsonNode escape hatch.</summary>
    IEnumerable<IDataContext> SelectMatches(string jsonPath);
```

- [ ] **Step 4: Implement on `DataContextImpl` (reuse the existing no-writes `JsonElement` fast path)**

```csharp
    /// <inheritdoc />
    public IDataContext? Select(string path)
    {
        if (!_overlay.HasWrites)
        {
            var match = JsonPathEvaluator.Evaluate(_base, JsonPathParser.Parse(path)).FirstOrDefault();
            if (match.CanonicalPath is null) return null;
            using var doc = JsonDocument.Parse(match.Element.GetRawText());
            return new DataContextImpl(doc.RootElement); // detached read view
        }
        var node = GetAsNode(path);
        if (node is null) return null;
        return new DataContextImpl(JsonDocument.Parse(node.ToJsonString()));
    }

    /// <inheritdoc />
    public IEnumerable<IDataContext> SelectMatches(string jsonPath)
    {
        var expr = JsonPathParser.Parse(jsonPath);
        var source = _overlay.HasWrites
            ? JsonDocument.Parse(GetAsNode("$")?.ToJsonString() ?? _base.GetRawText()).RootElement
            : _base;
        var results = new List<IDataContext>();
        foreach (var match in JsonPathEvaluator.Evaluate(source, expr))
        {
            results.Add(new DataContextImpl(JsonDocument.Parse(match.Element.GetRawText())));
        }
        return results;
    }
```

> Note for the implementer: this mirrors the existing `EnumerateMatches` allocation shape (one `JsonDocument.Parse` per match), so it is **equal** to today — not a regression. The benchmark in Task 30 confirms it. If profiling later shows the per-match document parse is hot, replace it with a thin `JsonElement`-wrapping read context, but only behind the benchmark.

- [ ] **Step 5: Implement on `DataContextChild`** (delegate through its snapshot+alias root, mirroring its existing `EnumerateMatches`)

```csharp
        public IDataContext? Select(string path)
        {
            var node = GetAsNode(path);
            return node is null ? null : new DataContextImpl(JsonDocument.Parse(node.ToJsonString()));
        }

        public IEnumerable<IDataContext> SelectMatches(string jsonPath)
        {
            var expr = JsonPathParser.Parse(jsonPath);
            var snapshotJson = GetAsNode("$")?.ToJsonString();
            using var snapshot = snapshotJson is not null ? JsonDocument.Parse(snapshotJson) : JsonDocument.Parse("{}");
            var root = BuildSnapshotRoot(snapshot.RootElement);
            using var rootDoc = JsonDocument.Parse(root.ToJsonString());
            var results = new List<IDataContext>();
            foreach (var match in JsonPathEvaluator.Evaluate(rootDoc.RootElement, expr))
            {
                results.Add(new DataContextImpl(JsonDocument.Parse(match.Element.GetRawText())));
            }
            return results;
        }
```

- [ ] **Step 6: Run — expected PASS**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~DataContextSelectTests"`
Expected: all PASS.

- [ ] **Step 7: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
feat(datacontext): add Select / SelectMatches returning IDataContext

Replaces the EnumerateMatches JsonNode escape hatch with detached read
sub-contexts; same per-match allocation shape as before.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 8: Make `JsonStringifyHelper` public (Cluster B home)

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/Internal/JsonStringifyHelper.cs:18` (`internal static` → `public static`)
- Test: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/Nodes/Transforms/Internal/JsonStringifyHelperTests.cs` (add visibility guard)

- [ ] **Step 1: Add a reflection guard test**

```csharp
    [Fact]
    public void Helper_is_public()
    {
        typeof(Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms.Internal.JsonStringifyHelper)
            .IsPublic.Should().BeTrue();
    }
```

- [ ] **Step 2: Run — expected FAIL**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~JsonStringifyHelperTests.Helper_is_public"`
Expected: FAIL (currently internal).

- [ ] **Step 3: Change `internal static class JsonStringifyHelper` → `public static class JsonStringifyHelper`**

- [ ] **Step 4: Run + full Sdk.Common.Tests — expected PASS**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName!~SystemTests"`
Expected: green at/above baseline.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
refactor(stringify): make JsonStringifyHelper public for cross-assembly reuse

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 9: Route `JoinNode.SelectRelativeNode` through `JsonNodePath.Select` (Cluster C)

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/JoinNode.cs:126-145` (delete `SelectRelativeNode`, call `JsonNodePath.Select`)
- Test: `octo-sdk/tests/Sdk.Common.Tests/.../JoinNodeTests.cs`

- [ ] **Step 1: Characterize current behavior, then add the bracket-key case as the conscious fix**

Add to the existing `JoinNodeTests`: a test that a simple dotted join key still works (pin current), and a test that a bracket/index join key (`$['order-id']` or `$.ids[0]`) now resolves (the fix). The bracket test FAILS today.

```csharp
    [Fact] public async Task Join_simpleDottedKey_matches() { /* existing-shape test asserting current behavior */ }

    [Fact]
    public async Task Join_bracketKey_nowResolves()
    {
        // Regression fix: SelectRelativeNode (Split('.')) silently failed on bracket keys.
        // After routing through JsonNodePath.Select, "$['order-id']" resolves.
        // ... build join with JoinKeyPath = "$['order-id']" and assert a match is produced.
    }
```

- [ ] **Step 2: Run — expected: dotted PASS, bracket FAIL**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~JoinNodeTests.Join_"`

- [ ] **Step 3: Replace the helper with `JsonNodePath.Select`**

In `JoinNode.cs`, delete `private static JsonNode? SelectRelativeNode(JsonNode root, string path) { ... }` (lines ~126-145) and change both call sites (`joinKey = SelectRelativeNode(joinNode, joinKeyPath)` line ~67) to `joinKey = JsonNodePath.Select(joinNode, joinKeyPath)`. Add `using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;` if absent.

- [ ] **Step 4: Run — expected both PASS**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~JoinNodeTests"`
Expected: all PASS, including the bracket case.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
refactor(join): use JsonNodePath.Select; fixes bracket/index join keys

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 10: Migrate SDK `DistinctNode` + `ConvertDataTypeNode` onto `JsonScalar`

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/DistinctNode.cs:100-113`
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/ConvertDataTypeNode.cs:84-130`
- Tests: existing `DistinctNodeTests`, `ConvertDataTypeNodeTests`

- [ ] **Step 1: Confirm existing tests pin the behavior (esp. DistinctNode's no-date-parse)**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~DistinctNodeTests|FullyQualifiedName~ConvertDataTypeNodeTests"`
Expected: green. If DistinctNode lacks a test proving `"2024-01-01"` stays a distinct string (not parsed to DateTime), ADD it now and confirm it passes.

- [ ] **Step 2: Replace DistinctNode's inline `JsonElement` coercion with `JsonScalar.ToClr(element, parseDateStrings: false)`**

In `DistinctNode.cs`, where it does `jv.GetValue<JsonElement>()` then `TryGetInt64 ? long : double` and returns `element.GetString()` for strings, replace with `JsonScalar.ToClr(element, parseDateStrings: false)` (the `false` preserves the deliberate no-DateTime behavior). Add the ck-engine `using`.

- [ ] **Step 3: For ConvertDataTypeNode, leave its explicit target-type conversion logic but read the source scalar via `GetValue`/`JsonScalar` where it currently does `node.GetValue<string>()`/`GetValue<double>()` ad hoc** (only where it does not change behavior — the characterization tests are the gate). Do not force-fit; if an arm doesn't map cleanly, leave it.

- [ ] **Step 4: Run — expected PASS (no behavior change)**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~DistinctNodeTests|FullyQualifiedName~ConvertDataTypeNodeTests"`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
refactor(transforms): Distinct/ConvertDataType scalar reads via JsonScalar

DistinctNode keeps parseDateStrings:false to preserve string distinctness.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 11: Remove the `JsonSerializerOptions?` parameter from the `IDataContext` surface

**Files:**
- Modify: `IDataContext.cs` (`Get<T>`, `GetArray<T>`, `Set<T>` overloads — drop the `JsonSerializerOptions? options` param)
- Modify: `DataContext.cs` (both implementations — use `SystemTextJsonOptions.Default` internally)
- Modify: every SDK call site passing `SystemTextJsonOptions.Default` (drop the argument)

- [ ] **Step 1: Drop the parameter from the interface and both implementations**

Change `T? Get<T>(string path, JsonSerializerOptions? options = null)` → `T? Get<T>(string path)`; same for `GetArray<T>` and the 6-arg `Set<T>`. Inside the implementations, replace the former `options ??= SystemTextJsonOptions.Default;` parameter use with the constant `SystemTextJsonOptions.Default` directly.

- [ ] **Step 2: Fix SDK call sites (compiler will list them)**

Run: `dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/Sdk.Common.csproj -c DebugL`
For each error, delete the trailing `, SystemTextJsonOptions.Default` argument. Grep to confirm none remain on the surface:
Run: `git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk grep -n "Get<.*SystemTextJsonOptions\|Set(.*SystemTextJsonOptions" -- 'src/Sdk.Common/EtlDataPipeline/Nodes/**/*.cs'`
Expected: no node-level hits (internal serializer use inside the impl is fine).

- [ ] **Step 3: Build + full Sdk.Common.Tests**

Run: `dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/Octo.Sdk.sln -c DebugL`
Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/Octo.Sdk.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"`
Expected: green at/above baseline.

- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
refactor(datacontext): drop JsonSerializerOptions param from the surface

Context always uses SystemTextJsonOptions.Default; the param was ceremony.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 12: Convert `EnumerateMatches` to internal + migrate the SDK `JoinNode` consumer

**Files:**
- Modify: `IDataContext.cs` (remove `EnumerateMatches` from the public interface), `DataContext.cs` (keep as a private/internal helper only if still needed internally; otherwise delete)
- Modify: `JoinNode.cs` (the one SDK consumer — switch to `SelectMatches`)

- [ ] **Step 1: Switch `JoinNode`'s `EnumerateMatches` use to `SelectMatches`**

`JoinNode` enumerates join records and reads a relative key from each. Replace `foreach (var node in EnumerateMatches(...))` + `JsonNodePath.Select(node, keyPath)` with `foreach (var ctx in dataContext.SelectMatches(...))` + `ctx.GetValue(keyPath)` (or `ctx.Get<string>(keyPath)` to match the current `joinKey.GetValue<string>()` shape). Run `JoinNodeTests` to confirm parity.

- [ ] **Step 2: Remove `EnumerateMatches` from `IDataContext`**

Delete the `IEnumerable<JsonNode?> EnumerateMatches(string jsonPath);` interface member and both implementations (now unused in the SDK; mesh-adapter consumers are migrated in Phase 3, which builds against this updated NuGet).

- [ ] **Step 3: Build + test**

Run: `dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/Octo.Sdk.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"`
Expected: green. (If anything else in the SDK used `EnumerateMatches`, the compiler flags it — migrate it to `SelectMatches` the same way.)

- [ ] **Step 4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
refactor(datacontext): remove EnumerateMatches; JoinNode uses SelectMatches

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 13: Build + pack SDK; run the perf benchmark (early zero-copy check)

- [ ] **Step 1: Build + pack**

Run: `Invoke-Build -repositoryPath ./octo-sdk -configuration DebugL`
Expected: success; sdk `999.0.0` refreshed in `../nuget`.

- [ ] **Step 2: Run the peak-heap benchmark and compare to the Task 0 baseline**

Run the ForEach memory benchmark again; append results to `baseline-perf.txt` under `## 2026-05-21 post-sdk-surface`.
Expected: peak heap within noise of baseline (no regression). If it regressed, `Select`/`SelectMatches` is materializing more than it should — revisit Task 7 before proceeding.

- [ ] **Step 3: Commit the benchmark record**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add docs/superpowers/plans/baseline-perf.txt
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
test(perf): record post-sdk-surface peak heap (no regression vs baseline)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 3 — octo-mesh-adapter: migrate nodes onto the surface

> **Per-node recipe (applies to every Task 14–27):** (1) confirm/add a characterization test that pins the node's CURRENT output for the relevant input matrix and run it GREEN; (2) apply the listed edit; (3) re-run the node's tests — must stay GREEN except the explicitly-noted conscious changes; (4) `dotnet build` the node's project; (5) commit. Build the SDK NuGet (Task 13) must be done first. Keep characterization tests that add coverage the node lacked.

### Task 14: `AnomalyNodeHelpers` → delete `TryReadNumeric`, use `JsonScalar.TryToNumber`

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/Internal/AnomalyNodeHelpers.cs`
- Modify: `StatisticalAnomalyNode.cs:84`, `MachineLearningAnomalyNode.cs:89` (call sites)

- [ ] **Step 1: Pin anomaly behavior** — run `StatisticalAnomalyNodeTests`, `MachineLearningAnomalyNodeTests`; if numeric-string coercion / non-numeric-throw isn't covered, add it. GREEN.
- [ ] **Step 2:** Delete `AnomalyNodeHelpers.TryReadNumeric<T>`; replace its two call sites with `JsonScalar.TryToNumber<double>` / `<float>` (add `using Meshmakers.Octo.Runtime.Contracts.Serialization;`). Keep `GetPropertyAsString` for now (handled in Task 26).
- [ ] **Step 3:** Run the two anomaly test classes — GREEN.
- [ ] **Step 4:** Commit `refactor(anomaly): use JsonScalar.TryToNumber, drop local helper`.

### Task 15: `CreateUpdateInfoNode.ExtractPrimitive` → `GetValue` / `JsonScalar`

**Files:** `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/CreateUpdateInfoNode.cs:219-227` (+ its `EnumerateMatches`/`Get<JsonNode>` use)

- [ ] **Step 1:** Run `CreateUpdateInfoNodeTests` GREEN; ensure the int→long, ISO→DateTime, bool, string cases are covered (add if missing). Also pin the timestamp-fallback case flagged in review (`TimestampPath` configured but missing → current `DateTime.MinValue`).
- [ ] **Step 2:** Replace `EnumerateMatches(...)` with `SelectMatches(...)`; for each match context read scalars via `m.GetValue(relativePath)` instead of `ExtractPrimitive(JsonValue)`. Delete `ExtractPrimitive`. Where it read `Get<JsonNode>` then unwrapped, use `GetValue`.
- [ ] **Step 3:** Run `CreateUpdateInfoNodeTests` — GREEN.
- [ ] **Step 4:** Commit `refactor(create-update-info): read values via GetValue/SelectMatches`.

### Task 16: `ApplyDataPointMappingsNode.UnwrapJsonNode` → `GetValue`

**Files:** `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/ApplyDataPointMappingsNode.cs:386-395` (+ `Get<JsonNode>` at the call site)

- [ ] **Step 1:** Run `ApplyDataPointMappingsNode` tests GREEN (cover int→long, real→double, bool, DateTime, string, object→ToJsonString fallback if currently relied on).
- [ ] **Step 2:** Delete `UnwrapJsonNode`; replace its caller with `dataContext.GetValue(path)`. If a current test proves the object→`ToJsonString()` fallback is used, keep that one path explicit (object case) — `GetValue` returns null for objects, so the caller stringifies via public `JsonStringifyHelper.ToLegacyString` (or `Get<JsonNode>`-free `Select` + serialize) only where the test demands it.
- [ ] **Step 3:** Run tests — GREEN.
- [ ] **Step 4:** Commit `refactor(apply-datapoint-mappings): GetValue replaces UnwrapJsonNode`.

### Task 17: `FieldFilterExtensions.GetComparisonValue` → `GetValue` (conscious long fix)

**Files:** `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/FieldFilterExtensions.cs:55-74` (+ `EnumerateMatches`, `Get<JsonNode>`)

- [ ] **Step 1:** Run `FieldFilterExtensionsTests` GREEN. Add a test that pins the CURRENT integer behavior (integer comparison value is `double` today). Add a second test asserting the DESIRED post-fix behavior (integer → `long`) — it FAILS now.
- [ ] **Step 2:** Replace the `bool/DateTime/double/string` ladder with `dataContext.GetValue(path)` (which includes the `long` rung). Replace `EnumerateMatches` with `SelectMatches`. Update the CURRENT-behavior test to the new `long` expectation, with a comment noting the conscious fix (matches `JsonScalar`/Newtonsoft parity).
- [ ] **Step 3:** Run tests — GREEN.
- [ ] **Step 4:** Commit `fix(field-filter): integer comparison value is long via GetValue`.

### Task 18: `MinMaxNode` inline coercion → `GetValue`

**Files:** `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/MinMaxNode.cs:30-52`

- [ ] **Step 1:** Run `MinMaxNodeTests` GREEN (DateTime, double, long-as-comparable cases covered).
- [ ] **Step 2:** Replace `JsonNodePath.Select(item, ValuePath)` + the `TryGetValue<DateTime>/<double>/<long>` ladder with `matchContext.GetValue(ValuePath)` (via `SelectMatches` over the source array), casting the result to `IComparable` and normalizing `long`→`double` for uniform comparison as today.
- [ ] **Step 3:** Run `MinMaxNodeTests` — GREEN.
- [ ] **Step 4:** Commit `refactor(minmax): comparable values via GetValue`.

### Task 19: `UpdateRtEntityIfNewerNode` inline coercion → `GetValue`/`JsonScalar`

**Files:** `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Load/UpdateRtEntityIfNewerNode.cs:278-280` (+ `Get<JsonNode>` reads)

- [ ] **Step 1:** Run `UpdateRtEntityIfNewerNodeTests` GREEN (string/DateTime/DateTimeOffset comparison-path cases covered; add if missing — esp. the record-typed path from commit bad4dcc).
- [ ] **Step 2:** Replace the `string/DateTime/DateTimeOffset` `TryGetValue` ladder with `GetValue` where the natural type suffices; keep the explicit `DateTimeOffset` handling only if a test proves it is needed beyond `DateTime`.
- [ ] **Step 3:** Run tests — GREEN.
- [ ] **Step 4:** Commit `refactor(update-rt-if-newer): comparison values via GetValue`.

### Task 20: `GetRtEntitiesByWellKnownNameTypeNode` → `Select`/`GetValue`, write via JsonNodePath stays internal

**Files:** `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Extract/GetRtEntitiesByWellKnownNameTypeNode.cs` (`Get<JsonNode>`, `JsonNodePath.Select`, `JsonNodePath.Set`)

- [ ] **Step 1:** Run `GetRtEntitiesByWellKnownNameTypeNodeTests` GREEN.
- [ ] **Step 2:** Replace node-level `JsonNodePath.Select`/`Get<JsonNode>` reads with `Select`/`GetValue`/`Get<T>`. For the writes that build `JsonObject` then set RtId/CkTypeId, prefer `dataContext.Set(targetPath, value)` of CLR values; if the node mutates a match in place, restructure to read via `SelectMatches` and write back through `dataContext.Set` on the parent path.
- [ ] **Step 3:** Run tests — GREEN.
- [ ] **Step 4:** Commit `refactor(get-rt-by-wellknown): surface-only data access`.

### Task 21: `GetAssociationTargetsNode` → `SelectMatches`

**Files:** `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Extract/GetAssociationTargetsNode.cs`

- [ ] **Step 1:** Run `GetAssociationTargetsNodeTests` GREEN.
- [ ] **Step 2:** Replace `EnumerateMatches` with `SelectMatches`; read needed values via `Get<T>`/`GetValue` on each match context.
- [ ] **Step 3:** Run tests — GREEN.
- [ ] **Step 4:** Commit `refactor(get-association-targets): SelectMatches`.

### Tasks 22–24: remaining `Get<JsonNode>` read consumers

For each of `CheckDuplicateNode.cs`, `MakeHttpRequestNode.cs:212`, `ToDiscordNode.cs`, `ImportFromExcelNode.cs`, `MapToRecordArrayNode.cs`, `AnthropicAiQueryNode.cs`: pin tests GREEN, replace `Get<JsonNode>`-and-inspect with `GetValue`/`Get<T>`/`Select`/`GetArray<T>`, and where the node stringifies an object/array, route through public `JsonStringifyHelper.ToLegacyString` (note: `MakeHttpRequestNode:212` currently emits **compact** `ToJsonString()` — preserve that exact output unless a test consciously changes it; do not silently switch to indented). One task per node, one commit each: `refactor(<node>): surface-only data access`.

> `ColumnContext` (the severe Excel finding) is handled here: replace `node.GetValue<T>()` (strict) with tolerant reads via the owning node's `IDataContext.Get<T>`/`GetValue`, and add the missing Excel-import characterization tests first (the node currently has none — pin number/bool cells reading without throwing).

### Tasks 25–27: write-side report builders → typed records

For each fixed-shape builder — `ValidateDataPointCoverageNode` (12 constructions), `GenerateDataPointMappingsNode` (6), `BuildMappingTargetsNode`, `ToDiscordNode` payload, `MapToRecordArrayNode`, anomaly result builders — one task each:

- [ ] **Step 1:** Pin the produced output with a characterization test (serialize the node's result and snapshot it) GREEN.
- [ ] **Step 2:** Define a `record` (or nested records) mirroring the report shape; build the record instead of `new JsonObject{...}`/`new JsonArray(...)`; `dataContext.Set(targetPath, report)`.
- [ ] **Step 3:** Run the node's tests + the snapshot — GREEN (the serialized shape must match; `SystemTextJsonOptions.Default` preserves nulls, so include nullable members exactly where the old `JsonObject` had explicit nulls).
- [ ] **Step 4:** Commit `refactor(<node>): build report as typed record, not JsonObject`.

`ImportFromCsvNode` (dynamic columns) uses `Dictionary<string, object?>` per row + `Set` (no fixed record possible); same test-first discipline.

### Task 28: Schema-gen string extraction (Cluster D, opportunistic)

**Files:** `octo-sdk` `NodeSchemaRegistry.cs`, `PipelineSchemaGenerator.cs`

- [ ] **Step 1:** Add a tiny internal helper `static string? AsString(JsonNode?)` in the schema-gen namespace (build-time code, stays JsonNode-based — this is framework, below the line). Replace the ~7 repeated `is JsonValue v && v.TryGetValue<string>(out s)` snippets with it.
- [ ] **Step 2:** Run schema-gen tests GREEN; commit `refactor(schema): dedupe JsonValue string extraction`.

---

## Phase 4 — Integration & the zero-copy gate

### Task 29: Cross-repo build + full test sweep

- [ ] **Step 1:** `Invoke-BuildAll -configuration DebugL` — expected all green.
- [ ] **Step 2:** Full test sweep on all three repos (commands from Task 0 Step 4) — expected at/above baseline counts.
- [ ] **Step 3:** Confirm no STJ on the node surface remains:

Run: `git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter grep -nE "EnumerateMatches|Get<JsonNode>|new JsonObject|new JsonArray|JsonValue.Create|JsonSerializerOptions" -- 'src/MeshAdapter.Sdk/Nodes/**/*.cs'`
Expected: only the framework-classified exceptions from spec §6 (none in node bodies that should be clean). Investigate any remaining hit.

### Task 30: Peak-heap benchmark gate (defends the migration's reason for existing)

- [ ] **Step 1:** Run the ForEach peak-managed-heap benchmark; append to `baseline-perf.txt` under `## 2026-05-21 post-encapsulation`.
- [ ] **Step 2:** Compare to the Task 0 pre-encapsulation numbers. Expected: **no regression** on the deeply-nested-ForEach-over-large-data case (within measurement noise). If regressed, the read-side surface is materializing copies — fix before merge (do not merge on a regression).
- [ ] **Step 3:** Commit the final benchmark record: `test(perf): post-encapsulation peak heap — no regression`.

### Task 31: Cleanup

- [ ] **Step 1:** Delete characterization tests that were pure scaffolding (duplicated by stronger existing tests); keep those that add lasting node coverage. Document in the commit which were kept and why.
- [ ] **Step 2:** Delete the `backup/pre-stj-encapsulation-2026-05-21` branches only after the user confirms the result is good.

---

## Self-Review

**Spec coverage:**
- §4.1 typed reads / drop options → Tasks 6, 11. §4.2 GetValue → Task 5. §4.4 Select/SelectMatches → Tasks 7, 12. §4.6 removals → Tasks 11, 12. ✅
- §5 primitive homes → Tasks 2–4 (ck-engine JsonScalar + RtAttributesConverter), 8 (JsonStringifyHelper public), 9 (JsonNodePath). ✅
- §6 typed-record write side → Tasks 25–27. ✅
- §7 zero-copy → enforced via Task 7 implementation note + Tasks 13, 30 benchmark gates. ✅
- §8 two test guardrails → characterization (every node task Step 1) + perf (Tasks 0, 13, 30). ✅
- §9 sequencing → Phases 1→2→3→4 bottom-up with NuGet repack between layers. ✅
- §3 clusters: A → Tasks 2–6, 10, 14–19; B → Tasks 8, 16, 22–24; C → Task 9; D → Task 28. ✅

**Placeholder scan:** Tasks 22–24 and 25–27 are grouped recipes rather than per-node full code because the edits are mechanical applications of the now-defined `GetValue`/`SelectMatches`/typed-record pattern against inventoried call sites; each names exact files and the exact transformation. The per-node *current* bodies must be read at execution time to write the characterization test — that is inherent to characterization testing, not a deferred design decision. Foundational tasks (1–13) carry complete code.

**Type consistency:** `JsonScalar.ToClr(JsonElement|JsonValue, bool)` and `TryToNumber<T>(JsonNode, out T)` are used consistently in Tasks 2–6, 10, 14. `GetValue(string, bool)`, `TryGet<T>(string, out T?)`, `Select(string)`, `SelectMatches(string)` signatures match between IDataContext (Tasks 5–7) and all consumers (Tasks 12, 14–24). ✅
