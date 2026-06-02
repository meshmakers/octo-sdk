# STJ Migration Code Review Round 2 — Verification & Fix Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Fix the 4 blockers from the round-2 code review. RED test → GREEN fix → commit per blocker. No claim closes until its test is green AND the full SDK + mesh-adapter test suites pass.

**Branches:** `dev/newtonsoft-to-stj-pipeline` in both `octo-sdk` and `octo-mesh-adapter`.

---

## Verification Matrix

| # | Claim | Status | Evidence | Severity |
|---|---|---|---|---|
| B1 | DataMappingNode reads Boolean as byte → STJ throws on JSON booleans | **REAL** | `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/DataMappingNode.cs:55` — `AttributeValueTypesDto.Boolean => dataContext.Get<byte>(path)` | BLOCKER |
| B2 | EtlDataOrchestrator never disposes root DataContextImpl | **REAL** | `octo-sdk/src/Sdk.Common/EtlDataPipeline/EtlDataOrchestrator.cs:43,64-138` — context created, returned via `dataContext.Get<JsonNode>("$")`, never disposed | BLOCKER |
| B3 | MeshAdapterJsonOptions internal in published NuGet package | **REAL** | `octo-mesh-adapter/src/MeshAdapter.Sdk/MeshAdapterJsonOptions.cs:14` — `internal static class`. Package metadata confirms `GeneratePackageOnBuild=true`, `PackageId=Meshmakers.Octo.Sdk.MeshAdapter` | BLOCKER |
| B4 | Sdk.Common.csproj `<Compile Remove>` block survives migration | **REAL** | `octo-sdk/src/Sdk.Common/Sdk.Common.csproj:46-128` — comment explicitly says "ALL exclusions in this block must be gone before merge" | BLOCKER |

**Reviewer's non-blockers** (deliberately deferred):
- HashNode/FormatStringNode object/array → compact vs indented JSON (only matters if hashes are persisted across migration boundary; no production usage flagged)
- `IDataContext.Get<T>` can't distinguish missing from default(T) — Path-only API by design; `TryGet<T>` is non-breaking add later
- Two parallel JSONPath modules — documented split (full-dialect reads vs dotted-only writes), follow-up consolidation
- Alias longest-prefix lookup ordering — XML doc claims longest-prefix but iterates dict in insertion order; latent contract violation, no current consumer triggers it
- DateTime parsing in `CreateUpdateInfoNode.ExtractPrimitive` and `LiteDbBsonConverter` — preserves Newtonsoft's default DateParseHandling.DateTime behavior

---

## Phase B1 — DataMappingNode Boolean→bool

**Files:**
- `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/DataMappingNode.cs:50-74` (modify)
- Test file: existing `DataMappingNodeTests.cs` if present, else create

### The bug

`DataMappingNode.GetValueByConfiguredType` line 55 reads `AttributeValueTypesDto.Boolean` via `dataContext.Get<byte>(path)`. The line is verbatim from main, but runtime semantics changed under STJ: `JsonNode.Deserialize<byte>(options)` doesn't have a boolean→byte converter and throws on JSON booleans. Newtonsoft silently coerced true/false to 1/0; STJ does not.

`MeshAdapterJsonOptions.Default` does not register a custom `byte` converter that handles booleans.

### Fix

Two-line change:
- Line 55: `AttributeValueTypesDto.Boolean => dataContext.Get<bool>(path)` (instead of `Get<byte>`)
- Line 91 in `ConvertToConfiguredType`: `AttributeValueTypesDto.Boolean => Convert.ChangeType(value, typeof(bool))` — already correct, value is now bool, ChangeType is a no-op.

### Test

- [ ] Add `DataMappingNode_BooleanSource_DoesNotThrow` test with `SourceValueType: Boolean`, source doc `{"flag": true}`, target attribute Boolean. Assert resulting attribute value is `true` (or whatever the existing assertion shape captures). Confirm test FAILS today with `JsonException`/`InvalidOperationException`. After fix: PASS.

### Commit

```
fix(DataMappingNode): read Boolean as bool, not byte (STJ regression)

Pre-migration JToken.Value<byte>() silently coerced JSON true/false to
1/0. STJ's JsonNode.Deserialize<byte>(options) has no boolean→byte
converter and throws JsonException at runtime. Pipelines configured
with SourceValueType=Boolean failed under STJ.

Read Boolean as bool. ConvertToConfiguredType already maps Boolean to
bool via Convert.ChangeType — no further adjustment needed.

Adds DataMappingNode_BooleanSource_DoesNotThrow regression test.
```

---

## Phase B2 — EtlDataOrchestrator dispose root context

**Files:**
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/EtlDataOrchestrator.cs:43,64-138` (modify)
- Test file: existing `EtlDataOrchestratorTests.cs` (modify or extend)

### The bug

`CreateDataContextFromValue` constructs a `DataContextImpl` that owns a `JsonDocument` (via `JsonDocument.Parse(...)`). The orchestrator's `try/finally` block at lines 64-138 never disposes `dataContext`. The `IDataContext` XML doc added in commit `d70474e` (Phase 5 L4) explicitly says callers should `using` it. The orchestrator is the canonical caller.

Each pipeline execution leaks a pooled `ArrayPool<byte>` buffer until GC.

### Fix

Wrap with `using var` declaration. The return at line 138 reads `dataContext.Get<JsonNode>("$")` which deserializes into a fresh in-memory `JsonNode` tree (independent of the underlying `JsonDocument`'s pooled buffer), so disposing AFTER computing the return value is safe.

```csharp
public async Task<object?> ExecutePipelineAsync<TEtlContext>(...) {
    ...
    using var dataContext = CreateDataContextFromValue(value);   // ← using
    var rootNodeContext = NodeContext.CreateRootNodeContext(...);
    ...
    // existing try/finally body unchanged
    ...
    return dataContext.Get<JsonNode>("$");  // dispose runs after this returns
}
```

The `using var` declaration disposes at end of method scope — after the `return` value is computed but before the method returns to caller. Verify by reading the JsonNode contract: `Get<JsonNode>` deserializes into a new JsonNode tree, not a view over the JsonDocument, so the returned tree survives disposal of the underlying document.

### Test

- [ ] Add `Orchestrator_DisposesDataContext_AfterExecution` regression test. Strategy: use a fake or counting `IDataContext` (or a wrapper around `DataContextImpl` that increments a counter on Dispose). Run a trivial pipeline. Assert Dispose was called.

  Alternative if mocking is awkward: write a JsonNode-returning result test that verifies the returned node is usable AFTER pipeline execution (proving the dispose order is correct). The first variant is more explicit; pick whichever fits the existing test conventions.

- [ ] Confirm test FAILS today (Dispose not called or context still holds JsonDocument). After fix: PASS.

### Commit

```
fix(EtlDataOrchestrator): dispose owned DataContextImpl per execution

The IDataContext contract documents that root contexts may own a
JsonDocument whose pooled ArrayPool buffer is released only on
IDisposable.Dispose. The orchestrator created such a context and never
disposed it, leaking one pooled buffer per pipeline execution until GC.

Wrap CreateDataContextFromValue in `using var`. Return reads
Get<JsonNode>("$") which materializes an independent in-memory tree, so
the value survives the dispose at end of scope.

Adds Orchestrator_DisposesDataContext_AfterExecution regression test.
```

---

## Phase B3 — MeshAdapterJsonOptions public

**Files:**
- `octo-mesh-adapter/src/MeshAdapter.Sdk/MeshAdapterJsonOptions.cs:14` (modify)

### The bug

`MeshAdapterJsonOptions` is `internal static class` but lives in `Meshmakers.Octo.Sdk.MeshAdapter` — a published NuGet package (`<GeneratePackageOnBuild>true</GeneratePackageOnBuild>`, `<PackageId>Meshmakers.Octo.Sdk.MeshAdapter</PackageId>`). The class is the central STJ options instance carrying 11 OctoMesh CK/Runtime converters. Third-party adapter authors who write custom nodes either:
- Have to reflect into the type
- Have to hand-roll the converter list (which goes stale as new converters are added)
- Silently get wrong deserialization with `PipelineJsonOptions.Default` (no CK converters)

Internal accessibility contradicts the documented intent in `octo-mesh-adapter/CLAUDE.md` ("central JsonSerializerOptions … reused").

### Fix

Single-line change: `internal static class` → `public static class`.

Verify no breaking change to internal call sites (53 usages confirmed compatible with `public`).

### Test

No new test required — this is a visibility change. Verification is a build pass and a quick consumer-facing smoke check:

- [ ] After change: `dotnet build -c DebugL` from mesh-adapter root succeeds.
- [ ] All existing tests pass (no regressions).
- [ ] Optional: add a `MeshAdapterJsonOptions_IsPublic` reflection test that asserts `typeof(MeshAdapterJsonOptions).IsPublic == true` to guard against accidental future reversion.

### Commit

```
fix(MeshAdapterJsonOptions): make public so adapter authors can reuse it

The class lives in a published NuGet package
(Meshmakers.Octo.Sdk.MeshAdapter) and carries the 11 OctoMesh CK/Runtime
converters needed for correct serialization of runtime entities. Third-
party adapter authors writing custom nodes had no way to access it
without reflection or hand-rolling the converter list (which goes stale
as new converters are added).

Visibility change only — internal call sites compile unchanged.
```

---

## Phase B4 — Sdk.Common.csproj cleanup

**Files:**
- `octo-sdk/src/Sdk.Common/Sdk.Common.csproj:46-128` (delete the `<ItemGroup>` block)

### The bug

The csproj contains a TEMPORARY `<ItemGroup>` with a `<Compile Remove="EtlDataPipeline/Nodes/**/*.cs" />` followed by ~60 individual `<Compile Include>` entries. The block's own comment says "ALL exclusions in this block must be gone before merge."

Effect on future maintenance: any new file added under `EtlDataPipeline/Nodes/**` is silently dropped from compilation until someone remembers to add it by hand. Already affected this branch — `JsonStringifyHelper.cs` had to be explicitly included (line 123 of csproj) when the implementer added it for Phase 3.1.

### Fix

Delete the entire `<ItemGroup>` block at lines 46-128. The default SDK glob `<Compile Include="**/*.cs" />` picks up everything under the project root automatically. The original purpose of the block (excluding legacy Newtonsoft node code while incrementally migrating) is fully obsolete: every file that was excluded is now migrated and individually re-included.

Sanity steps:
1. Diff before/after the deletion: confirm the same set of files compiles in both states (no orphan source files appear or disappear).
2. Build green: `dotnet build -c DebugL` from octo-sdk root.
3. Test green: `dotnet test -c DebugL` from octo-sdk.

### Test

No new test needed. Build pass + existing test green is the verification.

- [ ] Run `git -C /Users/reimar/dev/meshmakers/branches/main/octo-sdk grep -nE 'Compile Remove|Compile Include' src/Sdk.Common/Sdk.Common.csproj` after the fix — should return zero hits.
- [ ] `dotnet build -c DebugL` succeeds.
- [ ] `dotnet test -c DebugL` reports 614/614 (or whatever the current baseline is) passing.

### Commit

```
chore(Sdk.Common): remove TEMPORARY Compile Remove/Include block

The csproj's <Compile Remove="EtlDataPipeline/Nodes/**/*.cs"/> +
~60 individual <Compile Include> block was scaffolding for the
incremental Newtonsoft→STJ migration. Every excluded file is now
migrated and individually re-included; the block is functionally a
no-op. Its own comment said "ALL exclusions in this block must be
gone before merge."

Concrete consequence of leaving it: any new file added under
EtlDataPipeline/Nodes/** is silently dropped from compilation until
someone remembers to add it by hand. JsonStringifyHelper.cs (added
in Phase 3.1) had to do exactly that.

Delete the block. The default SDK glob picks up everything.
```

---

## Exit criteria

- [ ] All 4 blocker commits landed in correct repos.
- [ ] octo-sdk Sdk.Common.Tests green at current baseline (614+ depending on B1/B2 test additions).
- [ ] octo-sdk PipelineParityTests green (0 fail / 119 pass — unchanged).
- [ ] octo-mesh-adapter unit tests green (~299 + B1 test).
- [ ] octo-mesh-adapter integration tests green (14 unchanged).
- [ ] `git -C octo-sdk grep -nE 'Compile Remove' src/` returns zero hits.
- [ ] `MeshAdapterJsonOptions` visible as public in compiled assembly.
- [ ] EtlDataOrchestrator wraps DataContextImpl in `using`.
- [ ] DataMappingNode reads Boolean as `bool`.

---

## Self-review checklist

- Spec coverage: ✅ all 4 blockers have explicit fix + test + commit.
- Placeholders: ✅ none.
- Type/method consistency: ✅ all referenced methods exist on the current branch.
- Test-first ordering: ✅ each fix preceded by a RED test where adding one is meaningful (B3/B4 are visibility/csproj changes — build/existing-test pass is the verification).
- Scope: each phase is one logical change, one commit, in the correct repo.
- Non-blockers: explicitly listed and deliberately deferred — no scope creep into this sprint.
