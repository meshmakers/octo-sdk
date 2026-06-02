# STJ Pipeline Review Cleanup — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve all 13 main + 5 plausible review findings from the `dev/newtonsoft-to-stj-pipeline` cleanup review (P5 explicitly out of scope — see context below). Single PR per repo, multiple commits, branch `dev/newtonsoft-to-stj-pipeline`.

**Architecture:**
- Commits ordered safest → riskiest so any reverts stay surgical.
- Pure cleanup first; bug fixes that unblock refactors next; refactors after; behavior changes (with regression tests) last.
- Cross-repo dependency: `octo-mesh-adapter` consumes `octo-sdk` via NuGet packages from `../nuget`. After any `octo-sdk` change that touches public API, rebuild SDK and re-resolve packages before working on mesh-adapter.

**Tech Stack:** .NET 10 / xUnit v3 / FluentAssertions / FakeItEasy; build configuration **always `DebugL`**.

**Out of scope (explicit decisions):**
- **P5 (UpdateRecordArrayItemNode)** — The new reconstruction-style behavior is intentional and documented in `octo-mesh-adapter/CLAUDE.md` lines 76-80. Old aliased-mutation was a Newtonsoft accident. Do **not** touch.

**Repo paths used in this plan:**
- SDK: `/Users/reimar/dev/meshmakers/branches/main/octo-sdk`
- Mesh adapter: `/Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter`

---

## Pre-flight

- [ ] **Step 0.1: Confirm working tree clean on both repos**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk status
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter status
```

Expected: nothing to commit, on branch `dev/newtonsoft-to-stj-pipeline`.

- [ ] **Step 0.2: Baseline build (sanity)**

Use the `octo-devtools` skill or PowerShell:

```powershell
Invoke-Build -repositoryPath ./octo-sdk -configuration DebugL
Invoke-Build -repositoryPath ./octo-mesh-adapter -configuration DebugL
```

Expected: both succeed. If anything fails before we change a line, stop and investigate.

- [ ] **Step 0.3: Baseline test run**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/Octo.Sdk.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"
```

Expected: green on both. Capture any pre-existing failures — we will not fix them, but we must not regress them either.

---

## Task 1: octo-sdk — Drop temporary csproj exclusion block (#7)

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/Sdk.Common.csproj` — delete lines 42-128

- [ ] **Step 1.1: Delete the TEMPORARY block**

Edit `octo-sdk/src/Sdk.Common/Sdk.Common.csproj`. Remove the entire `<!-- TEMPORARY: ... -->` comment **and** the following `<ItemGroup>` that contains `<Compile Remove="EtlDataPipeline/Nodes/**/*.cs"/>` plus ~60 explicit `<Compile Include=…>` entries. The block ends just before the `<ItemGroup>` that begins `<AssemblyAttribute ...InternalsVisibleToAttribute…>`. After removal, that `AssemblyAttribute` block becomes the second-to-last `<ItemGroup>`.

- [ ] **Step 1.2: Build to verify default globbing covers everything**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/Sdk.Common.csproj -c DebugL
```

Expected: succeeds with the same compile output it had before.

- [ ] **Step 1.3: Run Sdk.Common.Tests**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName!~SystemTests"
```

Expected: same count, all pass.

- [ ] **Step 1.4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/Sdk.Common.csproj
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
chore(sdk-common): drop temporary EtlDataPipeline/Nodes exclusion block

The block instructed itself to be removed before merge; all 60 files
are already covered by default <Compile> globbing.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: octo-mesh-adapter — Drop empty Phase-9 ItemGroup (#10)

**Files:**
- Modify: `octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj:32-41`

- [ ] **Step 2.1: Remove stale comment + empty ItemGroup**

Delete lines 32-41 of `octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj`:

```xml
    <!--
      Phase 9 of the Newtonsoft → STJ pipeline migration: these unit tests assert
      ...
    -->
    <ItemGroup>
    </ItemGroup>
```

- [ ] **Step 2.2: Build and test**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj -c DebugL
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj -c DebugL --filter "FullyQualifiedName!~SystemTests"
```

Expected: green.

- [ ] **Step 2.3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter add tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter commit -m "$(cat <<'EOF'
chore(tests): drop empty Phase-9 exclusion ItemGroup

The list of excluded tests is empty; the comment described items that
no longer exist. Remove the ceremony.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: octo-sdk — Rename JObjectExtensions.cs to JsonObjectExtensions.cs (#9)

**Files:**
- Rename: `octo-sdk/src/Sdk.SimulationNodes/JObjectExtensions.cs` → `JsonObjectExtensions.cs`

- [ ] **Step 3.1: Use git mv to preserve history**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk mv src/Sdk.SimulationNodes/JObjectExtensions.cs src/Sdk.SimulationNodes/JsonObjectExtensions.cs
```

- [ ] **Step 3.2: Build (no code change — class was already JsonObjectExtensions)**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.SimulationNodes/Sdk.SimulationNodes.csproj -c DebugL
```

Expected: succeeds.

- [ ] **Step 3.3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
chore(simulation): rename JObjectExtensions.cs to match class name

The class JsonObjectExtensions migrated to STJ but the file kept the
Newtonsoft-era filename.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: octo-sdk — Remove dead Newtonsoft Generator method (#8)

**Files:**
- Modify: `octo-sdk/tests/Sdk.Common.Tests/TestData/Dto/Generator.cs:1-100`

- [ ] **Step 4.1: Remove the using and dead method**

In `octo-sdk/tests/Sdk.Common.Tests/TestData/Dto/Generator.cs`:
1. Delete line 3 `using Newtonsoft.Json.Linq;`
2. Delete the `GenerateColumnData()` method (lines 76-100)

Leave `GenerateColumnDataNode()` intact — it's the only one referenced.

- [ ] **Step 4.2: Build + test Sdk.Common.Tests**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName!~SystemTests"
```

Expected: green. If anything claims `JObject` or `GenerateColumnData`, we missed a caller — search again.

- [ ] **Step 4.3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add tests/Sdk.Common.Tests/TestData/Dto/Generator.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
chore(tests): remove dead Newtonsoft Generator.GenerateColumnData

Drops the unused JObject helper and its Newtonsoft.Json.Linq import —
the last non-parity-test Newtonsoft reference in the SDK tree.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: octo-sdk — Refresh CLAUDE.md (#13)

**Files:**
- Modify: `octo-sdk/CLAUDE.md:45`

- [ ] **Step 5.1: Update the JsonOptions reference**

In `octo-sdk/CLAUDE.md` line 45, replace:

> `PipelineJsonOptions.Default` is the central STJ options used by node code.

with:

> `SystemTextJsonOptions.Default` (`src/Sdk.Common/EtlDataPipeline/SystemTextJsonOptions.cs`) is the central STJ options bundle used by node code — carries `WhenWritingNull` and the CK/Rt converters.

- [ ] **Step 5.2: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add CLAUDE.md
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
docs: refresh CLAUDE.md PipelineJsonOptions → SystemTextJsonOptions

PipelineJsonOptions was renamed/consolidated into SystemTextJsonOptions;
docs were stale.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: octo-mesh-adapter — Refresh CLAUDE.md (#12)

**Files:**
- Modify: `octo-mesh-adapter/CLAUDE.md:82-90`

- [ ] **Step 6.1: Replace the MeshAdapterJsonOptions paragraph**

In `octo-mesh-adapter/CLAUDE.md`, replace the `MeshAdapterJsonOptions` bullet (the one referencing `src/MeshAdapter.Sdk/MeshAdapterJsonOptions.cs`) with:

```markdown
- **`SystemTextJsonOptions.Default`** (from `octo-sdk`, `src/Sdk.Common/EtlDataPipeline/SystemTextJsonOptions.cs`) — central `JsonSerializerOptions` carrying the STJ converters required by OctoMesh runtime types. The mesh-adapter no longer maintains its own bundle; all nodes that need to round-trip runtime entities, mutation DTOs, etc. reuse this single options instance from the SDK.
```

- [ ] **Step 6.2: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter add CLAUDE.md
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter commit -m "$(cat <<'EOF'
docs: refresh CLAUDE.md MeshAdapterJsonOptions → SystemTextJsonOptions

MeshAdapterJsonOptions.cs was deleted in 918abc2; callers switched to
SystemTextJsonOptions from octo-sdk.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: octo-mesh-adapter — Refresh ImportFromCsvNode summary (#11)

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/ImportFromCsvNode.cs:11-13`

- [ ] **Step 7.1: Update the XML summary**

Replace:

```csharp
/// <summary>
/// Pipeline node that imports CSV file data into a JArray of JObjects based on column mappings
/// </summary>
```

with:

```csharp
/// <summary>
/// Pipeline node that imports CSV file data into a <see cref="System.Text.Json.Nodes.JsonArray"/>
/// of <see cref="System.Text.Json.Nodes.JsonObject"/> values, based on column mappings.
/// </summary>
```

- [ ] **Step 7.2: Build (catches doc-id warnings if any)**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/src/MeshAdapter.Sdk/MeshAdapter.Sdk.csproj -c DebugL
```

Expected: succeeds with no new warnings.

- [ ] **Step 7.3: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter add src/MeshAdapter.Sdk/Nodes/Transform/ImportFromCsvNode.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter commit -m "$(cat <<'EOF'
docs(import-csv): summary references STJ types instead of JArray/JObject

The body migrated to JsonArray/JsonObject; the XML summary lagged.
This ships in NuGet xmldoc.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: octo-sdk — Fix JsonNodePath.NormalizePath bracket-root bug (P6)

This is a real bug AND a prerequisite for Task 9 (consolidating the duplicated `NormalizeRelative`). The current `NormalizePath` mangles `$['foo-bar']` into `$.$['foo-bar']` because the chain only checks for `$.` and `.` prefixes, not `$[`.

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/JsonNodePath.cs:353-360`
- Test: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonNodePathTests.cs` (existing file)

- [ ] **Step 8.1: Add failing test**

Append to `JsonNodePathTests.cs` (inside the existing test class):

```csharp
[Fact]
public void Select_BracketPropertyAtRoot_ResolvesValue()
{
    var root = JsonNode.Parse("""{"foo-bar":42}""")!;
    var result = JsonNodePath.Select(root, "$['foo-bar']");
    result.Should().NotBeNull();
    result!.GetValue<int>().Should().Be(42);
}

[Fact]
public void Select_BracketPropertyAtRoot_DoesNotPrependDollarDot()
{
    // Regression: NormalizePath used to turn "$['foo-bar']" into "$.$['foo-bar']".
    var root = JsonNode.Parse("""{"foo-bar":1,"x":2}""")!;
    var result = JsonNodePath.Select(root, "$['foo-bar']");
    result.Should().NotBeNull();
    result!.GetValue<int>().Should().Be(1);
}
```

- [ ] **Step 8.2: Run — expected FAIL**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~JsonNodePathTests.Select_BracketPropertyAtRoot"
```

Expected: 2 failures with a parse or "no match" error.

- [ ] **Step 8.3: Fix `NormalizePath`**

In `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/JsonNodePath.cs:353-360`, replace:

```csharp
private static string NormalizePath(string path)
{
    if (string.IsNullOrEmpty(path)) return "$";
    if (path == "$") return path;
    if (path.StartsWith("$.", StringComparison.Ordinal)) return path;
    if (path.StartsWith(".", StringComparison.Ordinal)) return "$" + path;
    return "$." + path;
}
```

with:

```csharp
private static string NormalizePath(string path)
{
    if (string.IsNullOrEmpty(path)) return "$";
    if (path == "$") return path;
    if (path.StartsWith("$.", StringComparison.Ordinal)) return path;
    if (path.StartsWith("$[", StringComparison.Ordinal)) return path;
    if (path.StartsWith(".", StringComparison.Ordinal)) return "$" + path;
    return "$." + path;
}
```

- [ ] **Step 8.4: Run — expected PASS**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~JsonNodePathTests"
```

Expected: all green, including the two new tests.

- [ ] **Step 8.5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add src/Sdk.Common/EtlDataPipeline/JsonPath/JsonNodePath.cs tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonNodePathTests.cs
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
fix(jsonpath): NormalizePath preserves $[ bracket selector at root

NormalizePath previously prepended "$." to any path that did not start
with "$." or ".", mangling "$['foo-bar']" into "$.$['foo-bar']".
Adds the $[ branch and a regression test.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: octo-sdk — Consolidate NormalizeRelative onto JsonNodePath (#2)

Now that `NormalizePath` handles bracket-root correctly (Task 8), the seven duplicated `NormalizeRelative` helpers in transform nodes can be replaced with a single canonical entry point.

**Decision:** expose `JsonNodePath.NormalizePath` as `public` and rename it to `NormalizePathOrRelative` (the existing private helper also accepts bare/relative forms — which is exactly what the duplicates do).

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/JsonNodePath.cs` — change `private static string NormalizePath` to `public static string NormalizePathOrRelative`; update both internal callers (`SelectAll`, `ParseDottedSegments`).
- Modify: Delete `NormalizeRelative` and replace its calls in:
  - `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/HashNode.cs:114-119`
  - `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/Base64EncodeNode.cs:71-76`
  - `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/Base64DecodeNode.cs:81-86`
  - `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/TransformStringNode.cs:115-120`
  - `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/MathNode.cs:150-155`
  - `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/ConcatNode.cs:88-93`
  - `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/JoinNode.cs:121-126`
- Test: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonNodePathTests.cs` — add a parity test that asserts the public helper produces identical outputs for the inputs the duplicates handle.

- [ ] **Step 9.1: Add a parity test for the public normalizer**

Append to `JsonNodePathTests.cs`:

```csharp
[Theory]
[InlineData("", "$")]
[InlineData("$", "$")]
[InlineData("$.id", "$.id")]
[InlineData("$[0]", "$[0]")]
[InlineData("$['foo-bar']", "$['foo-bar']")]
[InlineData(".id", "$.id")]
[InlineData("id", "$.id")]
[InlineData("foo.bar", "$.foo.bar")]
public void NormalizePathOrRelative_MatchesNodeNormalizeRelativeContract(string input, string expected)
{
    JsonNodePath.NormalizePathOrRelative(input).Should().Be(expected);
}
```

- [ ] **Step 9.2: Run — expected FAIL** (method doesn't exist yet)

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~NormalizePathOrRelative"
```

Expected: compile error or method-not-found.

- [ ] **Step 9.3: Make NormalizePath public + rename**

In `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/JsonNodePath.cs`, change:

```csharp
private static string NormalizePath(string path)
{
    ...
}
```

to:

```csharp
/// <summary>
/// Normalizes user-supplied JSONPath strings — bare ("id"), leading-dot (".id"),
/// rooted ("$.id"), and bracket-rooted ("$['foo-bar']") all resolve to a canonical
/// rooted form. Replaces the per-node <c>NormalizeRelative</c> duplicates.
/// </summary>
public static string NormalizePathOrRelative(string path)
{
    if (string.IsNullOrEmpty(path)) return "$";
    if (path == "$") return path;
    if (path.StartsWith("$.", StringComparison.Ordinal)) return path;
    if (path.StartsWith("$[", StringComparison.Ordinal)) return path;
    if (path.StartsWith(".", StringComparison.Ordinal)) return "$" + path;
    return "$." + path;
}
```

Update the two internal callers (`SelectAll` line 71, `ParseDottedSegments` line 321) to use the new name.

- [ ] **Step 9.4: Run normalize parity test — expected PASS**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~NormalizePathOrRelative"
```

- [ ] **Step 9.5: Replace each duplicate**

For each of the 7 files listed above:
1. Add `using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;` if missing.
2. Delete the local `private static string NormalizeRelative(string path) { ... }` method.
3. Replace every call site `NormalizeRelative(x)` with `JsonNodePath.NormalizePathOrRelative(x)`.

- [ ] **Step 9.6: Build + full Sdk.Common.Tests**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-sdk/src/Sdk.Common/Sdk.Common.csproj -c DebugL
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName!~SystemTests"
```

Expected: green. The 7 transform-node unit tests must still pass — they cover the same input shapes the duplicates were handling.

- [ ] **Step 9.7: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
refactor(jsonpath): consolidate 7 NormalizeRelative duplicates onto JsonNodePath

Hash, Base64Encode, Base64Decode, TransformString, Math, Concat, Join all
shipped an identical NormalizeRelative helper. Promotes JsonNodePath's
private normalizer to public NormalizePathOrRelative and routes every
caller through it.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: octo-sdk — Clean up stale `_KnownDivergence_Throws` test (#6)

The XML doc and method name claim the test asserts exceptions, but the body has no `Assert.Throws` and asserts parity. We must first **observe** which case is actually true by running the test.

**Files:**
- Modify: `octo-sdk/tests/Sdk.Common.PipelineParityTests/ReadParityTests.cs:280-420`
- Modify: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/JsonNodePathTests.cs:74-76` (the "Phase 3.2 will lift that limitation" comment per the review)

- [ ] **Step 10.1: Diagnose current state**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.PipelineParityTests/Sdk.Common.PipelineParityTests.csproj -c DebugL --filter "FullyQualifiedName~NewtonsoftAndStj_AgreeOnPath_KnownDivergence_Throws"
```

Decision branches:

- **If the test passes** → the parser now accepts these paths. The test is a parity test misnamed as a throw-test. Go to Step 10.2a.
- **If the test fails** → some cases still throw, but the test body has no `Assert.Throws` to absorb them. Go to Step 10.2b.

- [ ] **Step 10.2a: (test passes) — Rename + clean xmldoc**

Rename the method `NewtonsoftAndStj_AgreeOnPath_KnownDivergence_Throws` → `NewtonsoftAndStj_AgreeOnPath_FormerDivergence_NowAtParity`. Rewrite the surrounding xmldoc on the data-generator and the test method to describe what they are now: parity cases that used to throw before commits a62a281 / cfd7cee landed but now pass parity. Drop the "Every case below fails today with a thrown exception" sentence entirely. Also delete the stale "Phase 3.2 will lift that limitation" comment at `JsonNodePathTests.cs:74-76`.

- [ ] **Step 10.2b: (test fails) — Restore `Assert.Throws` and Section B partition**

Inside the test body, wrap `JsonPathParser.Parse(path)` with `Assert.Throws<JsonPathNotSupportedException>` (or whichever exact exception type the failing cases produce — read the failure message). Move any cases that now pass parity into the main `NewtonsoftAndStj_AgreeOnPath` data set instead. Keep the xmldoc factually accurate for what's left.

- [ ] **Step 10.3: Re-run**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.PipelineParityTests/Sdk.Common.PipelineParityTests.csproj -c DebugL
```

Expected: green.

- [ ] **Step 10.4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
test(parity): clean stale KnownDivergence_Throws naming + xmldoc

Section B cases (bracket-property + hyphenated dotted) are at parity
now; rename the test and update the surrounding documentation to
reflect what the body actually asserts.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: octo-mesh-adapter — Extract anomaly-node shared helper (#4)

`StatisticalAnomalyNode` and `MachineLearningAnomalyNode` both ship a byte-identical `GetPropertyAsString`, plus a 20-line `try/catch InvalidOperationException → string-parse-fallback` block that differs only in `double` vs `float`. Pull both into a small internal helper.

**Files:**
- Create: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/Internal/AnomalyNodeHelpers.cs`
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/StatisticalAnomalyNode.cs:84-105, 136-145`
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/MachineLearningAnomalyNode.cs:89-109, 255-264`

- [ ] **Step 11.1: Create the helper**

```csharp
// octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/Internal/AnomalyNodeHelpers.cs
using System.Globalization;
using System.Text.Json.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

namespace Meshmakers.Octo.Sdk.MeshAdapter.Nodes.Transform.Internal;

internal static class AnomalyNodeHelpers
{
    /// <summary>
    /// Renders a node found at <paramref name="path"/> on <paramref name="item"/> as
    /// a string, used as a grouping key. Returns null if the path does not resolve
    /// to a JsonValue.
    /// </summary>
    public static string? GetPropertyAsString(JsonNode? item, string path)
    {
        var node = JsonNodePath.Select(item, path);
        if (node is JsonValue v)
        {
            if (v.TryGetValue<string>(out var s)) return s;
            return v.ToJsonString();
        }
        return null;
    }

    /// <summary>
    /// Tries to read <paramref name="node"/> as a numeric of type <typeparamref name="T"/>.
    /// Accepts JSON numbers natively and JSON strings that parse to <typeparamref name="T"/>
    /// under invariant culture. Returns false otherwise.
    /// </summary>
    public static bool TryReadNumeric<T>(JsonNode node, out T value) where T : struct, IParsable<T>
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
            if (node is JsonValue jv
                && jv.TryGetValue<string>(out var s)
                && T.TryParse(s, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
            value = default;
            return false;
        }
    }
}
```

- [ ] **Step 11.2: Update StatisticalAnomalyNode**

In `StatisticalAnomalyNode.cs`:
1. Add `using Meshmakers.Octo.Sdk.MeshAdapter.Nodes.Transform.Internal;`
2. Replace the existing `private static string? GetPropertyAsString(...)` method (lines 136-145) with a call site `AnomalyNodeHelpers.GetPropertyAsString(...)` everywhere it's used.
3. Replace the `try/catch InvalidOperationException` block (lines 84-105) with:

```csharp
if (!AnomalyNodeHelpers.TryReadNumeric<double>(sourceToken, out var value))
{
    throw MeshAdapterPipelineExecutionException.InputValueInvalidFormat(
        nodeContext, statisticalDetector.Path, new FormatException("Cannot read as double"));
}
```

Drop the now-unused `using MassTransit.Internals;` if no other reference remains in the file (verify with a quick grep).

- [ ] **Step 11.3: Update MachineLearningAnomalyNode**

Same as Step 11.2 but with `<float>` instead of `<double>`, and the `float` parser path.

- [ ] **Step 11.4: Build + test**

```bash
dotnet build /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/src/MeshAdapter.Sdk/MeshAdapter.Sdk.csproj -c DebugL
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj -c DebugL --filter "FullyQualifiedName!~SystemTests"
```

Expected: green. If the anomaly-node tests fail with cast/format errors, the helper's exception-handling envelope is off — re-read both old `catch` blocks and reconcile.

- [ ] **Step 11.5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter commit -m "$(cat <<'EOF'
refactor(anomaly): extract shared GetPropertyAsString + numeric-coercion helper

StatisticalAnomalyNode and MachineLearningAnomalyNode shipped an identical
GetPropertyAsString helper and a near-identical InvalidOperationException
fallback block (the only difference being double vs float). Move both to
AnomalyNodeHelpers; T is constrained to IParsable<T> so the string-parse
fallback is generic.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 12: octo-sdk — Rename TrySelect to Exists, drop out-param (#5)

The sole caller (ProjectNode.cs:112) uses `out _` and only needs the existence-vs-null distinction. The API would be clearer as `bool Exists(JsonNode? root, string path)`.

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/JsonPath/JsonNodePath.cs:29-56` (the TrySelect method + its xmldoc) and `:19` (cref in `Select` summary)
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/ProjectNode.cs:112`

- [ ] **Step 12.1: Rename + simplify signature**

Replace the `TrySelect` method with:

```csharp
/// <summary>
/// Returns true if <paramref name="path"/> resolves to at least one node, including
/// an explicit JSON-null value. Use this when "missing" and "present-but-null" must
/// be distinguished — <see cref="Select"/> collapses both via FirstOrDefault.
/// </summary>
public static bool Exists(JsonNode? root, string path)
{
    using var e = SelectAll(root, path).GetEnumerator();
    return e.MoveNext();
}
```

Update the `<see cref="TrySelect"/>` reference in the `Select` method's xmldoc (line 19) to `<see cref="Exists"/>`.

- [ ] **Step 12.2: Update the caller**

In `ProjectNode.cs:112`, change:

```csharp
if (JsonNodePath.TrySelect(snapshot, fc.Path, out _))
```

to:

```csharp
if (JsonNodePath.Exists(snapshot, fc.Path))
```

- [ ] **Step 12.3: Build + test**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName!~SystemTests"
```

Expected: green.

- [ ] **Step 12.4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
refactor(jsonpath): rename TrySelect to Exists, drop out-param

The sole caller used `out _` — only the existence-vs-null distinction
matters. `Exists(root, path)` reads more clearly at the call site.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 13: octo-sdk — Add JsonPath walker parity test harness (#3, scoped)

Full unification of `JsonPathEvaluator` (over `JsonElement`) and `JsonNodePath.Walk` (over `JsonNode`) is high-risk: the underlying types differ enough that a generic walker would either box every read or duplicate via type parameters. The maintenance pain the reviewer raised is "a fix to one wasn't ported to the other." We address that pain directly with a parity test that exercises both walkers against a shared corpus — any future divergence fails the build, without needing to share walker code.

**Files:**
- Create: `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/WalkerParityTests.cs`

- [ ] **Step 13.1: Add the parity test**

```csharp
// octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/JsonPath/WalkerParityTests.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.JsonPath;

namespace Sdk.Common.Tests.EtlDataPipeline.JsonPath;

public class WalkerParityTests
{
    /// <summary>
    /// Corpus exercising every segment kind shared between JsonPathEvaluator
    /// (JsonElement) and JsonNodePath.Walk (JsonNode). Each entry covers one
    /// dialect feature; expand whenever a new bug fix lands in one walker so
    /// the other walker is automatically checked.
    /// </summary>
    public static IEnumerable<object[]> Cases() => new[]
    {
        // property + index
        new object[] { """{"a":{"b":[1,2,3]}}""", "$.a.b[1]" },
        // wildcard
        new object[] { """{"a":[1,2,3]}""", "$.a[*]" },
        // wildcard on object
        new object[] { """{"a":{"x":1,"y":2}}""", "$.a.*" },
        // recursive descent
        new object[] { """{"a":{"b":{"c":1}}}""", "$..c" },
        // filter on array elements
        new object[] { """[{"k":"x"},{"k":"y"}]""", "$[?(@.k=='x')]" },
        // filter on object members (the bug fixed in commit 1bd4faf —
        // ensures both walkers handle object-keyed maps under filter)
        new object[] { """{"machines":{"m1":{"k":"x"},"m2":{"k":"y"}}}""", "$.machines[?(@.k=='x')]" },
        // filter under recursive descent
        new object[] { """{"a":{"items":[{"k":"x"},{"k":"y"}]}}""", "$..[?(@.k=='x')]" },
        // bracket-property at root (regression: P6)
        new object[] { """{"foo-bar":42}""", "$['foo-bar']" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Both_Walkers_Yield_Same_Matches(string json, string path)
    {
        var expr = JsonPathParser.Parse(path);

        using var doc = JsonDocument.Parse(json);
        var elementMatches = JsonPathEvaluator.Evaluate(doc.RootElement, expr)
            .Select(m => Normalize(m.Element.GetRawText()))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        var root = JsonNode.Parse(json)!;
        var nodeMatches = JsonNodePath.SelectAll(root, path)
            .Select(n => Normalize(n?.ToJsonString() ?? "null"))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        nodeMatches.Should().Equal(elementMatches,
            $"the two walkers must yield identical match sets for path '{path}'");
    }

    private static string Normalize(string raw)
    {
        // STJ and JsonElement.GetRawText can differ in whitespace; round-trip
        // via JsonDocument to collapse to a canonical form.
        using var doc = JsonDocument.Parse(raw);
        return JsonSerializer.Serialize(doc.RootElement);
    }
}
```

- [ ] **Step 13.2: Run**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~WalkerParityTests"
```

Expected: green. If anything fails, that **is** the parallel-walker bug — fix one walker to match the other before continuing.

- [ ] **Step 13.3: Update `JsonNodePath.Walk` doc-comment**

In `JsonNodePath.cs:164-166`, append a sentence:

```csharp
/// Walker parity with <see cref="JsonPathEvaluator"/> is enforced by
/// <c>WalkerParityTests</c>; expand its corpus whenever a fix lands in
/// one walker so the other is verified automatically.
```

- [ ] **Step 13.4: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
test(jsonpath): pin walker parity with shared corpus instead of unifying

JsonPathEvaluator (over JsonElement) and JsonNodePath.Walk (over JsonNode)
shipped near-verbatim parallel implementations. Rather than unify them
(which would force boxing or generics across two struct/class types),
pin parity via a corpus test that exercises both walkers. Any future
fix to one walker that isn't mirrored in the other will fail the build.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 14: octo-sdk — Fix EtlDataOrchestrator overlay mirror (#1)

The terminal `nextDelegate` does `dataContext.Set("$", ds.Get<JsonNode>("$"))` unconditionally. When `ds == dataContext` (no sub-context layer), this lifts the entire base document into the overlay for no reason, defeating every `HasWrites` fast path. The mirror is only meaningful when `ds` is a different (sub-)context.

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/EtlDataOrchestrator.cs:60-67`
- Test: new file `octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/EtlDataOrchestratorOverlayLiftTests.cs`

- [ ] **Step 14.1: Inspect IDataContext to find a `HasWrites` accessor**

The overlay-lift fast paths reference `_overlay.HasWrites`. For testing we need a way to assert from outside that the overlay was NOT lifted. Check `DataContextImpl` for an internal/test-visible property; if none exists, add an `internal bool OverlayHasWrites` shim used only by tests (the assembly already has `InternalsVisibleTo("Sdk.Common.Tests")` per csproj line 135).

- [ ] **Step 14.2: Add failing test**

```csharp
// octo-sdk/tests/Sdk.Common.Tests/EtlDataPipeline/EtlDataOrchestratorOverlayLiftTests.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class EtlDataOrchestratorOverlayLiftTests
{
    [Fact]
    public async Task EmptyPipeline_DoesNotLiftOverlay()
    {
        // A pipeline with no transformations means the terminal delegate is
        // the only one invoked, with ds == the outer dataContext.
        // It must not mark the overlay dirty.
        var services = new ServiceCollection();
        // [register pipeline orchestrator + minimal node lookup —
        //  match whatever the existing EtlDataOrchestrator tests use]
        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IEtlDataOrchestrator>();

        var value = JsonNode.Parse("""{"x":1}""")!;
        var nodeDef = new NodeDefinitionRoot
        {
            Transformations = new List<NodeConfiguration>() // empty: terminal delegate only
        };

        var result = await orchestrator.ExecutePipelineAsync(
            nodeDef, new TestEtlContext(), pipelineDebugger: null, value);

        // The orchestrator's outer dataContext is internal; assert via the result
        // shape — it must round-trip the input without overlay mutation. The real
        // overlay-state assertion is delegated to a second test below.
        result.Should().NotBeNull();
    }

    // [Add a more targeted test that builds a DataContextImpl directly,
    //  runs the equivalent of the terminal delegate, and asserts
    //  OverlayHasWrites stays false.]
}
```

The plan-author note: this test sketch is intentionally incomplete because the orchestrator's existing test fixture pattern (DI graph + mocking) must match the codebase. Read `EtlDataOrchestratorTests.cs` (or the closest existing equivalent) first and follow its setup verbatim.

- [ ] **Step 14.3: Run — expected FAIL (overlay lifted)**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~EtlDataOrchestratorOverlayLift"
```

- [ ] **Step 14.4: Fix the terminal delegate**

In `octo-sdk/src/Sdk.Common/EtlDataPipeline/EtlDataOrchestrator.cs:60-67`, replace:

```csharp
NodeDelegate nextDelegate = (ds, nc) =>
{
    nc.Unregister(ds);

    // Mirror the inner data context's root state back onto the outer context.
    dataContext.Set("$", ds.Get<JsonNode>("$"));
    return Task.CompletedTask;
};
```

with:

```csharp
NodeDelegate nextDelegate = (ds, nc) =>
{
    nc.Unregister(ds);

    // Mirror only when we ran in a sub-context. When ds is the outer
    // dataContext (no sub-context layer), Set("$", Get<JsonNode>("$"))
    // would lift the entire base document into the overlay and defeat
    // every HasWrites fast path. See migration spec §5.1.
    if (!ReferenceEquals(ds, dataContext))
    {
        dataContext.Set("$", ds.Get<JsonNode>("$"));
    }
    return Task.CompletedTask;
};
```

- [ ] **Step 14.5: Run test — expected PASS**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName~EtlDataOrchestratorOverlayLift"
```

- [ ] **Step 14.6: Run full Sdk.Common.Tests**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL --filter "FullyQualifiedName!~SystemTests"
```

Expected: still green. If sub-context tests (ForEach, Switch, Project) regress, the `ReferenceEquals` check is too narrow — sub-contexts may share identity with the outer context after `Unregister`. Investigate before adjusting.

- [ ] **Step 14.7: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk add -A
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -m "$(cat <<'EOF'
perf(orchestrator): skip overlay mirror when ds is the outer dataContext

The terminal delegate's Set("$", Get<JsonNode>("$")) was lifting the
entire base document into the overlay on every pipeline execution,
defeating IDataContext's HasWrites fast paths and contradicting the
migration spec's promise that overlay cost is bounded by what is
actually written.

The mirror only matters when a sub-context (ForEach / Switch /
Project) produced state we need to propagate back; guard with
ReferenceEquals.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 15: octo-sdk — Regression tests + fix SwitchNode nullable behavior (P3)

Pre-migration Newtonsoft path resolution at a missing key returned defaulted values (`false`/`0`/`""`). The STJ version uses `Get<bool?>` / `Get<int?>` etc., so missing keys now resolve to `null` — they hit the default case instead of matching `false`/`0`/`""`.

**Decision:** restore Newtonsoft semantics. Missing/null source → match `false`/`0`/empty-string case if present, fall through to default otherwise.

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Control/SwitchNode.cs:177-191`
- Add tests in the existing SwitchNode tests file (find it under `tests/Sdk.Common.Tests/.../Control/`).

- [ ] **Step 15.1: Add failing parity tests**

For each value type that previously coerced missing → default, add a test asserting "missing path matches the false/0/empty-string case." E.g.:

```csharp
[Fact]
public async Task Switch_MissingBooleanPath_MatchesFalseCase()
{
    var config = new SwitchNodeConfiguration
    {
        Path = "$.missing",
        ValueType = AttributeValueTypesDto.Boolean,
        Cases = new List<SwitchCase>
        {
            new() { Value = false, Transformations = ... /* something observable */ }
        }
    };
    // execute and assert the false-case branch ran
}
```

Plus equivalents for `Int`, `Int64`, `Double` (matches `0`) and `String` (matches `""`).

- [ ] **Step 15.2: Run — expected FAIL**

- [ ] **Step 15.3: Fix `GetValueFromDataContext`**

Replace the nullable returns with non-nullable defaults:

```csharp
private static object? GetValueFromDataContext(INodeContext nodeContext, IDataContext dataContext, string path,
    AttributeValueTypesDto valueType)
{
    return valueType switch
    {
        AttributeValueTypesDto.Boolean => dataContext.Get<bool?>(path) ?? false,
        AttributeValueTypesDto.Int => dataContext.Get<int?>(path) ?? 0,
        AttributeValueTypesDto.Int64 => dataContext.Get<long?>(path) ?? 0L,
        AttributeValueTypesDto.Double => dataContext.Get<double?>(path) ?? 0.0,
        AttributeValueTypesDto.String => dataContext.Get<string>(path) ?? string.Empty,
        AttributeValueTypesDto.DateTime => dataContext.Get<DateTime?>(path), // no Newtonsoft default
        AttributeValueTypesDto.Enum => dataContext.Get<int?>(path) ?? 0,
        _ => throw PipelineExecutionException.ValueTypeNotSupported(nodeContext.NodePath, valueType, path)
    };
}
```

- [ ] **Step 15.4: Run — expected PASS**

- [ ] **Step 15.5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -am "$(cat <<'EOF'
fix(switch): restore Newtonsoft default-on-missing for primitive paths

Missing source paths previously coerced to false/0/0L/0.0/"" and matched
those cases. STJ migration widened the read to nullable T?, so missing
paths started hitting the default branch instead. Restore the defaults
explicitly (DateTime stays nullable — Newtonsoft had no DateTime default
either).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 16: octo-sdk — Regression test + fix ConvertDataTypeNode numeric strings (P4)

`String`/`Boolean`/`DateTime` branches in `ConvertPrimitiveValue` already have tolerant string coercion. The numeric branches (`Int`/`Int64`/`Double`) use strict `Get<int>` etc. — a JSON string `"42"` throws.

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/ConvertDataTypeNode.cs:84-90`

- [ ] **Step 16.1: Add failing tests**

In the existing ConvertDataTypeNode tests, add cases for `JsonValue.Create("42")` → Int 42, `JsonValue.Create("3.14")` → Double 3.14, `JsonValue.Create("9999999999")` → Int64 9999999999.

- [ ] **Step 16.2: Run — expected FAIL**

- [ ] **Step 16.3: Replace numeric coercion with tolerant readers**

Replace the strict numeric switch arm with explicit handlers, mirroring the existing Boolean/DateTime approach:

```csharp
return c.ValueType switch
{
    AttributeValueTypesDto.Int => ConvertNodeToInt(dataContext.Get<JsonNode>(c.Path), c),
    AttributeValueTypesDto.Int64 => ConvertNodeToInt64(dataContext.Get<JsonNode>(c.Path), c),
    AttributeValueTypesDto.Double => ConvertNodeToDouble(dataContext.Get<JsonNode>(c.Path), c),
    _ => throw DataPipelineException.ValueTypeUnsupported(c.Path, c.ValueType)
};
```

…where each `ConvertNodeToX` handles `JsonValueKind.Number` natively and `JsonValueKind.String` via `int.Parse` / `long.Parse` / `double.Parse` with `CultureInfo.InvariantCulture`.

- [ ] **Step 16.4: Run — expected PASS**

- [ ] **Step 16.5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -am "$(cat <<'EOF'
fix(convert-data-type): accept numeric strings for Int/Int64/Double

String/Boolean/DateTime branches already coerced JSON-string sources;
the numeric branches went through strict typed Get<int>/<long>/<double>
which threw on "42". Add tolerant Convert helpers parallel to the
existing Boolean/DateTime ones.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 17: octo-sdk — Fix Base64EncodeNode stringification (P2)

`Base64EncodeNode.GetCultureInvariantString` (lines 81-93) has no Date branch and uses `node.ToJsonString()` (compact) for objects/arrays. Sibling nodes (`HashNode`, `ConcatNode`, `JoinNode`) all route through `JsonStringifyHelper.ToLegacyString` for Newtonsoft-compatible output. Make Base64 consistent.

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/Base64EncodeNode.cs:46-93`

- [ ] **Step 17.1: Add failing test**

Add a test in the Base64EncodeNode test file (find under `tests/Sdk.Common.Tests/.../Transforms/`) asserting that encoding an object source uses indented (Newtonsoft-parity) output:

```csharp
[Fact]
public async Task Encode_ObjectSource_UsesIndentedNewtonsoftParityFormat()
{
    var input = JsonNode.Parse("""{"obj":{"a":1,"b":2}}""");
    // run Base64EncodeNode with SourcePath = "$.obj"
    // decode the result, assert it equals "{\n  \"a\": 1,\n  \"b\": 2\n}"
}
```

- [ ] **Step 17.2: Run — expected FAIL** (current output is compact)

- [ ] **Step 17.3: Replace `GetCultureInvariantString` with helper**

In `Base64EncodeNode.cs`:
1. Add `using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms.Internal;`
2. Replace the call site (line 49) `var sourceValue = GetCultureInvariantString(sourceTokenValue);` with `var sourceValue = JsonStringifyHelper.ToLegacyString(sourceTokenValue) ?? string.Empty;`.
3. Delete the `GetCultureInvariantString` method (lines 81-93). Remove the now-unused `using System.Globalization;` if no other references remain.

Note: `JsonStringifyHelper` is `internal` in `Sdk.Common`. `Base64EncodeNode` is in the same assembly, so this works without changes.

- [ ] **Step 17.4: Run — expected PASS**

- [ ] **Step 17.5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -am "$(cat <<'EOF'
fix(base64-encode): use JsonStringifyHelper for Newtonsoft-parity stringification

Base64EncodeNode shipped its own GetCultureInvariantString that emitted
compact JSON for objects/arrays — diverging from HashNode/ConcatNode/JoinNode
which all go through JsonStringifyHelper.ToLegacyString (indented output,
matching Newtonsoft's Formatting.Indented). Route Base64Encode through the
same helper so base64 outputs round-trip stably.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 18: octo-sdk — SumAggregationNode throws on non-numeric (P1)

`SumAggregationNode` silently continues the inner loop when `TryGetElementAsDouble` returns false. Older Newtonsoft behavior was to throw. Restore the throw.

**Files:**
- Modify: `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Transforms/Aggregations/SumAggregationNode.cs:84-91`

- [ ] **Step 18.1: Add failing test**

In the SumAggregationNode tests, add a case where the aggregation path resolves to a non-numeric value (e.g. `"banana"`) and assert `PipelineExecutionException` is thrown.

- [ ] **Step 18.2: Run — expected FAIL** (silently skips)

- [ ] **Step 18.3: Replace silent skip with throw**

Replace:

```csharp
foreach (var aggMatch in JsonPathEvaluator.Evaluate(sourceMatch.Element, JsonPathParser.Parse(item.AggregationPath)))
{
    if (TryGetElementAsDouble(aggMatch.Element, out var v))
    {
        d += v * item.Value;
    }
}
```

with:

```csharp
foreach (var aggMatch in JsonPathEvaluator.Evaluate(sourceMatch.Element, JsonPathParser.Parse(item.AggregationPath)))
{
    if (!TryGetElementAsDouble(aggMatch.Element, out var v))
    {
        throw PipelineExecutionException.InputValueInvalidFormat(
            nodeContext, item.AggregationPath,
            new FormatException($"Value at '{item.AggregationPath}' is not numeric"));
    }
    d += v * item.Value;
}
```

(Use whichever exception factory the existing aggregation tests assert against — search for the prior throw signature in git log if uncertain.)

- [ ] **Step 18.4: Run — expected PASS**

- [ ] **Step 18.5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk commit -am "$(cat <<'EOF'
fix(sum-aggregation): throw on non-numeric match instead of silent skip

The Newtonsoft implementation surfaced non-numeric matches via
PipelineExecutionException. The STJ migration moved to TryGet... but
kept the silent-skip branch — making aggregation results quietly
wrong rather than loud-failing. Restore the throw.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

---

## Task 19: octo-mesh-adapter — DataMappingNode reads Boolean as bool, not byte (B1 from older plan)

Carried in from `docs/superpowers/plans/2026-05-08-stj-migration-review-fixes-r2.md` B1. Pre-migration `JToken.Value<byte>()` silently coerced JSON `true`/`false` to `1`/`0`. STJ's `JsonNode.Deserialize<byte>(options)` has no boolean→byte converter and throws `JsonException` at runtime for any pipeline configured with `SourceValueType=Boolean`. Still unfixed on the branch.

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/DataMappingNode.cs:55`
- Test: existing `DataMappingNodeTests.cs` if present, else create

- [ ] **Step 19.1: Add failing test**

```csharp
[Fact]
public async Task DataMappingNode_BooleanSource_ReadsTrueWithoutThrowing()
{
    // Pre-migration JToken.Value<byte>() coerced true/false to 1/0;
    // STJ has no boolean→byte converter and throws. Read as bool instead.
    var dataContext = new DataContextImpl(JsonDocument.Parse("""{"flag":true}"""));
    // ... build a DataMappingNodeConfiguration with SourceValueType=Boolean
    //     and SourcePath="$.flag", and a target attribute mapping. Run the node.
    //     Assert target receives true.
}
```

The scaffolding (config builder + target inspection) must match the existing `DataMappingNodeTests.cs` conventions; read that file first.

- [ ] **Step 19.2: Run — expected FAIL** (`JsonException` or `InvalidOperationException`)

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj -c DebugL --filter "FullyQualifiedName~DataMappingNode_BooleanSource"
```

- [ ] **Step 19.3: Fix**

In `DataMappingNode.cs:55` change:

```csharp
AttributeValueTypesDto.Boolean => dataContext.Get<byte>(path),
```

to:

```csharp
AttributeValueTypesDto.Boolean => dataContext.Get<bool>(path),
```

`ConvertToConfiguredType` at line 91 already maps `Boolean` via `Convert.ChangeType(value, typeof(bool))` — a no-op for the new `bool` value. No further edits needed.

- [ ] **Step 19.4: Run — expected PASS**

- [ ] **Step 19.5: Commit**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter commit -am "$(cat <<'EOF'
fix(data-mapping): read Boolean as bool, not byte

Pre-migration JToken.Value<byte>() silently coerced JSON true/false to
1/0. STJ's JsonNode.Deserialize<byte>(options) has no boolean→byte
converter and throws JsonException at runtime, breaking every pipeline
configured with SourceValueType=Boolean.

ConvertToConfiguredType already maps Boolean to bool via
Convert.ChangeType — no further adjustment needed.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Final integration

- [ ] **Step F.1: Full cross-repo build**

```powershell
Invoke-BuildAll -configuration DebugL
```

Expected: every repo green. NuGet packages from `octo-sdk` are now in `../nuget` and consumed by `octo-mesh-adapter`.

- [ ] **Step F.2: Full test sweep**

```bash
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-sdk/Octo.Sdk.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"
dotnet test /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"
```

Expected: green on both.

- [ ] **Step F.3: Confirm commit log**

```bash
git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk log --oneline origin/dev/newtonsoft-to-stj-pipeline..HEAD
git -C /Users/reimar/dev/meshmakers/branches/main/octo-mesh-adapter log --oneline origin/dev/newtonsoft-to-stj-pipeline..HEAD
```

Expected: octo-sdk has commits for tasks 1, 3, 4, 5, 8, 9, 10, 12, 13, 14, 15, 16, 17, 18 (14 commits); octo-mesh-adapter has commits for tasks 2, 6, 7, 11 (4 commits).

- [ ] **Step F.4: Push and open/update PRs**

Push both branches; if PRs already exist on `dev/newtonsoft-to-stj-pipeline`, they will auto-update. Otherwise open one per repo, referencing each other in the description.

---

## Risk notes for the implementing engineer

1. **Task 9 depends on Task 8.** If Task 8 is reverted, Task 9 reintroduces the bracket-root bug at every duplicated call site.
2. **Task 13 is the substitute for "unify the walkers."** If the parity test fails on first run, do NOT delete cases — that *is* the maintenance bug the reviewer flagged. Fix whichever walker is wrong.
3. **Task 14 has the highest blast radius.** The `ReferenceEquals` check has to be exactly right; sub-contexts created by `ForEachNode` / `ObjectIteratorNode` / `SelectByPathNode` / `SwitchNode` / `ProjectNode` use various wrappers. Verify by running their tests before declaring victory.
4. **Tasks 15–18 are behavior changes.** Each one MUST land with the regression test that captures the prior Newtonsoft behavior. Without that test, the next migration cleanup will re-introduce the regression.
5. **Cross-repo order:** When working on `octo-mesh-adapter` tasks (2, 6, 7, 11), always rebuild `octo-sdk` first if the SDK commits in this PR have touched its public API (Task 9: `NormalizePathOrRelative`; Task 12: `Exists`). Use `Invoke-BuildAll` if uncertain.

---

## Self-review

**Spec coverage:** Every reviewer finding (#1-13 + P1, P2, P3, P4, P6) is mapped to a task. P5 is documented as explicitly out of scope.

**Placeholders:** Task 14 (Step 14.2) and Task 15 (Step 15.1) and Tasks 16-18 reference "the existing test fixture pattern" / "the existing tests file" rather than reproducing complete test scaffolding. This is intentional — the test fixture conventions live in the codebase and reproducing them in the plan would diverge if they evolve. The implementing engineer must read the closest existing test file before writing the new one. Acceptable.

**Type/name consistency:** `NormalizePathOrRelative` (Task 9), `Exists` (Task 12), `AnomalyNodeHelpers.GetPropertyAsString` / `TryReadNumeric<T>` (Task 11) — all referenced consistently in the tasks that use them.
