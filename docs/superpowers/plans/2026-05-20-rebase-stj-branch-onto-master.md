# Rebase `dev/newtonsoft-to-stj-pipeline` onto master + adapt new master code Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebase the local-only `dev/newtonsoft-to-stj-pipeline` feature branch (Newtonsoft → System.Text.Json ETL migration) onto current `origin/main` in both `octo-sdk` and `octo-mesh-adapter`, and migrate the new-on-master code that uses the removed Newtonsoft API so both repos build and test green in `DebugL`.

**Architecture:** The rebase mechanics were validated on throwaway `trial/stj-rebase` branches; all conflict resolutions are recorded in git `rerere`. The real work is replaying the rebase (rerere auto-resolves the content conflicts; the modify/delete conflict needs a manual `git rm`) and then migrating master's new pipeline nodes/generators/tests to the STJ `IDataContext` API the branch introduced. `octo-sdk` is done first because `octo-mesh-adapter` consumes its `DebugL` NuGet (version `999.0.0`) from `../nuget`.

**Tech Stack:** .NET 10 (`net10.0` + `netstandard2.0`), System.Text.Json (`JsonNode`/`JsonObject`/`JsonArray`), xUnit v3 + FakeItEasy, PowerShell `octo-tools` build profile, `DebugL` build configuration.

---

## Shared reference (read once before any task)

### Branch / safety facts
- Both feature branches are **local-only (unpushed)** → rebase is safe; no force-push coordination needed.
- Merge-base is ~3 weeks old. `origin/main` is already fetched.
- `rerere` is enabled in both repos and has **recorded resolutions** for the content conflicts from the trial run.
- The validated `trial/stj-rebase` branch exists in both repos and is the reference for "what the rebased result looks like".

### The exact rebase conflicts (from the validated trial)
**octo-sdk** (147 commits replayed) — 2 content conflicts, both auto-replayed by rerere:
1. `src/Sdk.SimulationNodes/DataPipelineBuilderExtensions.cs` (commit `515add1`) — keep both: STJ structure + master's 3 `AddKeyedSingleton` Energy generator registrations.
2. `src/Sdk.Common/Sdk.Common.csproj` (commit `3c6ee27`) — keep master's bumped versions, drop the `Newtonsoft.Json` line.

**octo-mesh-adapter** (26 commits replayed) — 3 conflicts:
1. `src/MeshAdapter.Sdk/Nodes/Extract/EnrichWithMongoDataNode.cs` (commits `8251d4c` **and** `918abc2`) — **modify/delete; recurs twice; NOT auto-resolved by rerere** (tree conflict). Resolution = accept master's deletion (`git rm`).
2. `tests/MeshAdapter.Sdk.Tests/Nodes/Load/SaveStreamDataInArchiveNodeTests.cs` (commit `727ae2d`) — content conflict, auto-replayed by rerere to the STJ side; the file is then properly rebuilt in Task B8.
3. `AnthropicAiQueryNode.cs` + `SaveStreamDataInArchive.cs` auto-merge cleanly (verified — master's named HttpClients and the `ArchiveRtId`/`InsertAsync` signature survive).

### STJ API substitution table (octo-mesh-adapter nodes)
Apply these to every new-on-master node. Confirmed against already-migrated branch nodes.

| Old (Newtonsoft, on master) | New (STJ, this branch) |
|---|---|
| `using Newtonsoft.Json.Linq;` | `using System.Text.Json.Nodes;` (add `using System.Text.Json;` only if `JsonSerializer` is used) |
| `RtNewtonsoftSerializer.DefaultSerializer` | `SystemTextJsonOptions.Default` (from `Meshmakers.Octo.Sdk.Common.EtlDataPipeline`) |
| `dataContext.GetComplexObjectByPath<T>(path, RtNewtonsoftSerializer.DefaultSerializer)` | `dataContext.Get<T>(path, SystemTextJsonOptions.Default)` |
| `dataContext.GetSimpleValueByPath<T>(path)` | `dataContext.Get<T>(path)` |
| `dataContext.SetValueByPath(path, value, docMode, valueKind, writeMode, RtNewtonsoftSerializer.DefaultSerializer)` | `dataContext.Set(path, value, docMode, valueKind, writeMode, SystemTextJsonOptions.Default)` |
| `new JArray()` | `new JsonArray()` |
| `new JObject { ["k"] = v }` | `new JsonObject { ["k"] = v }` (implicit conversions from `int`/`double`/`string`/`bool` to `JsonNode` apply) |
| `new JArray(IEnumerable<string> xs)` | `new JsonArray(xs.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray())` |
| `JObject.FromObject(dict)` | `(JsonObject)JsonSerializer.SerializeToNode(dict, SystemTextJsonOptions.Default)!` |
| `JArray.FromObject(list)` | `(JsonArray)JsonSerializer.SerializeToNode(list, SystemTextJsonOptions.Default)!` |

### Reference exemplars (already migrated on this branch — mirror them)
- Build a `JsonObject`/`JsonArray` report and `Set` it: `src/MeshAdapter.Sdk/Nodes/Transform/BuildMappingTargetsNode.cs` (esp. lines ~149-175).
- A data-point mapping transform with `Set(...)`: `src/MeshAdapter.Sdk/Nodes/Transform/ApplyDataPointMappingsNode.cs`.
- Read a typed list + write it back: `src/MeshAdapter.Sdk/Nodes/Transform/FilterLatestUpdateInfoNode.cs`, `.../CreateUpdateInfoNode.cs`.
- Build rows then `Set`: `src/MeshAdapter.Sdk/Nodes/Transform/ImportFromCsvNode.cs`.
- Test scaffolding (STJ): tests derive from `NodeTestBase` and use `PrepareTest<TConfig>(config)`. Mirror an existing migrated load/transform test, e.g. `tests/MeshAdapter.Sdk.Tests/Nodes/Load/ApplyChangesNodeTests.cs` or the existing `SaveInTimeSeries`-style tests already on the branch.

### Build / test commands (run from workspace root `/Users/reimar/dev/meshmakers/branches/main`)
Load the PowerShell profile once per shell (use `pwsh`):
```
. ./octo-tools/modules/profile.ps1
```
- Build a single repo + publish its NuGet to `../nuget`: `Invoke-Build -repositoryPath ./octo-sdk -configuration DebugL`
- Direct build of a repo: `dotnet build <repo>/<sln> -c DebugL`
- Run a repo's tests excluding system tests: `dotnet test <repo>/<sln> -c DebugL --filter "FullyQualifiedName!~SystemTests"`
- Run one test class: `dotnet test -c DebugL --filter "FullyQualifiedName~SaveStreamDataInArchiveNodeTests"`

Do not chain shell commands with `&&`/`;`. Use `git -C <repo>` for git; never `cd` into a sub-repo.

---

## Phase 0 — Safety net

### Task 0: Back up both feature branches before rewriting history

**Files:** none (git refs only)

- [ ] **Step 1: Confirm working trees are clean**

Run: `git -C octo-sdk status --short`
Run: `git -C octo-mesh-adapter status --short`
Expected: no output (clean) on both. If not clean, stop and surface it.

- [ ] **Step 2: Create backup refs of the pre-rebase feature branch**

```bash
git -C octo-sdk branch backup/pre-stj-rebase-2026-05-20 dev/newtonsoft-to-stj-pipeline
git -C octo-mesh-adapter branch backup/pre-stj-rebase-2026-05-20 dev/newtonsoft-to-stj-pipeline
```

- [ ] **Step 3: Verify backups exist**

Run: `git -C octo-sdk branch --list 'backup/*'`
Run: `git -C octo-mesh-adapter branch --list 'backup/*'`
Expected: `backup/pre-stj-rebase-2026-05-20` listed in both.

---

## Phase A — octo-sdk (must complete before Phase B)

### Task A1: Rebase octo-sdk feature branch onto origin/main

**Files:** none (rebase + rerere replay)

- [ ] **Step 1: Switch to the feature branch**

Run: `git -C octo-sdk switch dev/newtonsoft-to-stj-pipeline`
Expected: `Switched to branch 'dev/newtonsoft-to-stj-pipeline'`

- [ ] **Step 2: Start the rebase**

Run: `git -C octo-sdk rebase origin/main`
Expected: stops at commit `515add1` with a conflict in `DataPipelineBuilderExtensions.cs` that rerere **already resolved** (message: `Resolved '...DataPipelineBuilderExtensions.cs' using previous resolution.`). The file should have no conflict markers.

- [ ] **Step 3: Verify rerere resolved it, then stage + continue**

Run: `grep -c '^<<<<<<<' octo-sdk/src/Sdk.SimulationNodes/DataPipelineBuilderExtensions.cs`
Expected: `0` (no markers). If non-zero, resolve per Shared reference (keep both) before continuing.
Run: `git -C octo-sdk add src/Sdk.SimulationNodes/DataPipelineBuilderExtensions.cs`
Run: `git -C octo-sdk rebase --continue`
Expected: proceeds, then stops again at `3c6ee27` for `Sdk.Common.csproj` (rerere-resolved).

- [ ] **Step 4: Verify + stage + continue the csproj conflict**

Run: `grep -c '^<<<<<<<' octo-sdk/src/Sdk.Common/Sdk.Common.csproj`
Expected: `0`.
Run: `git -C octo-sdk add src/Sdk.Common/Sdk.Common.csproj`
Run: `git -C octo-sdk rebase --continue`
Expected: `Successfully rebased and updated refs/heads/dev/newtonsoft-to-stj-pipeline.`

- [ ] **Step 5: Sanity-check the rebased tip**

Run: `git -C octo-sdk log --oneline -1 origin/main`
Run: `git -C octo-sdk log --oneline -3 dev/newtonsoft-to-stj-pipeline`
Expected: the feature branch's first-parent history sits directly on top of `origin/main`'s tip (`db35a6c …`).

### Task A2: Migrate `EnergyGenerators.cs` to STJ (the only sdk compile break)

**Files:**
- Modify: `octo-sdk/src/Sdk.SimulationNodes/Generators/EnergyGenerators.cs`

Only two kinds of change: the `using`, and `JObject` → `JsonObject` (3 occurrences). The `configuration.GetValue<T>("key", default)` calls work unchanged — the branch defines `JsonObjectExtensions.GetValue<T>(this JsonObject, string, T)` in `src/Sdk.SimulationNodes/JsonObjectExtensions.cs`, visible to the `Generators` sub-namespace without an extra `using`.

- [ ] **Step 1: Replace the full file content**

```csharp
using System.Globalization;
using Bogus;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.Services;
using System.Text.Json.Nodes;

namespace Meshmakers.Octo.Sdk.SimulationNodes.Generators;

/// <summary>
/// Returns <c>startDate + index * stepSize</c> as a UTC DateTime. Deterministic — used to
/// emit equally-spaced slot boundaries (e.g. 15-min EDA windows) inside a <c>For@1</c> loop
/// where <c>index</c> comes from the loop iterator.
/// </summary>
internal class SteppedDateTimeGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, Faker faker, JsonObject configuration)
    {
        var startDateString = configuration.GetValue("startDate", DateTime.UtcNow.ToString("o"));
        var stepSizeString = configuration.GetValue("stepSize", "PT15M");
        var index = configuration.GetValue<int>("index", 0);

        var startDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var step = System.Xml.XmlConvert.ToTimeSpan(stepSizeString);

        return DateTime.SpecifyKind(startDate.Add(TimeSpan.FromTicks(step.Ticks * index)), DateTimeKind.Utc);
    }
}

/// <summary>
/// Returns the energy (in kWh) for a 15-min slot of a German BDEW standard load profile.
/// Configuration: <c>profile</c> (H0/G0/L0; default H0), <c>dailyEnergyKwh</c> (total daily
/// energy in kWh; default 10), <c>slotIndex</c> (0..95; default 0). The math is provided by
/// <see cref="EnergyProfiles.LoadProfileSlot"/>.
/// </summary>
internal class LoadProfileGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, Faker faker, JsonObject configuration)
    {
        var profile = configuration.GetValue("profile", "H0");
        var dailyEnergyKwh = configuration.GetValue("dailyEnergyKwh", 10.0);
        var slotIndex = configuration.GetValue<int>("slotIndex", 0);

        try
        {
            return EnergyProfiles.LoadProfileSlot(profile, dailyEnergyKwh, slotIndex);
        }
        catch (ArgumentException ex)
        {
            throw new PipelineNodeExecutionException(ex.Message);
        }
    }
}

/// <summary>
/// Returns the PV-production energy (in kWh) for a 15-min slot.
/// Configuration: <c>peakKwp</c> (default 5), <c>dayOfYear</c> (1..366; default 172),
/// <c>slotIndex</c> (0..95). The math is provided by <see cref="EnergyProfiles.PvProfileSlot"/>.
/// </summary>
internal class PvProfileGenerator : IValueGenerator
{
    public object? Generate(IEtlContext etlContext, Faker faker, JsonObject configuration)
    {
        var peakKwp = configuration.GetValue("peakKwp", 5.0);
        var dayOfYear = configuration.GetValue<int>("dayOfYear", 172);
        var slotIndex = configuration.GetValue<int>("slotIndex", 0);

        try
        {
            return EnergyProfiles.PvProfileSlot(peakKwp, dayOfYear, slotIndex);
        }
        catch (ArgumentException ex)
        {
            throw new PipelineNodeExecutionException(ex.Message);
        }
    }
}
```

- [ ] **Step 2: Confirm no other sdk file references the old API**

Run: `git -C octo-sdk grep -nE "RtNewtonsoftSerializer|Newtonsoft\.Json\.Linq" -- 'src/Sdk.SimulationNodes/**/*.cs'`
Expected: no output. (`EnergyProfiles.cs` is pure math and needs no change.)

### Task A3: Build octo-sdk in DebugL, run tests, publish NuGet

**Files:** none

- [ ] **Step 1: Build + pack to ../nuget**

Run (pwsh, profile loaded): `Invoke-Build -repositoryPath ./octo-sdk -configuration DebugL`
Expected: build succeeds (warnings-as-errors is on; zero errors). `Meshmakers.Octo.Sdk.*` `999.0.0` packages refreshed in `../nuget`.

- [ ] **Step 2: Run unit tests (exclude system tests)**

Run: `dotnet test octo-sdk/Octo.Sdk.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"`
Expected: all pass. If `Sdk.SimulationNodes`/parity tests fail, fix before proceeding — do not advance to Phase B on a red sdk.

- [ ] **Step 3: Commit the sdk adaptation**

```bash
git -C octo-sdk add src/Sdk.SimulationNodes/Generators/EnergyGenerators.cs
git -C octo-sdk commit -m "refactor(simulation): migrate EnergyGenerators to STJ JsonObject after rebase"
```

---

## Phase B — octo-mesh-adapter (after Phase A NuGet is published)

### Task B0: Rebase octo-mesh-adapter feature branch onto origin/main

**Files:** none (rebase + rerere replay + manual `git rm` for the modify/delete)

- [ ] **Step 1: Switch + start rebase**

Run: `git -C octo-mesh-adapter switch dev/newtonsoft-to-stj-pipeline`
Run: `git -C octo-mesh-adapter rebase origin/main`
Expected: stops at `8251d4c` with a modify/delete conflict on `EnrichWithMongoDataNode.cs` (rerere does NOT resolve tree conflicts; `AnthropicAiQueryNode.cs`/`SaveStreamDataInArchive.cs` auto-merge with no markers).

- [ ] **Step 2: Accept master's deletion + continue**

Run: `git -C octo-mesh-adapter rm src/MeshAdapter.Sdk/Nodes/Extract/EnrichWithMongoDataNode.cs`
Run: `git -C octo-mesh-adapter rebase --continue`
Expected: proceeds, then stops at `727ae2d` for `SaveStreamDataInArchiveNodeTests.cs` (rerere-resolved to STJ side).

- [ ] **Step 3: Verify + stage the test conflict + continue**

Run: `grep -c '^<<<<<<<' octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/Nodes/Load/SaveStreamDataInArchiveNodeTests.cs`
Expected: `0`.
Run: `git -C octo-mesh-adapter add tests/MeshAdapter.Sdk.Tests/Nodes/Load/SaveStreamDataInArchiveNodeTests.cs`
Run: `git -C octo-mesh-adapter rebase --continue`
Expected: stops again at `918abc2` — `EnrichWithMongoDataNode.cs` modify/delete recurs.

- [ ] **Step 4: Accept deletion again + finish**

Run: `git -C octo-mesh-adapter rm src/MeshAdapter.Sdk/Nodes/Extract/EnrichWithMongoDataNode.cs`
Run: `git -C octo-mesh-adapter rebase --continue`
Expected: `Successfully rebased and updated refs/heads/dev/newtonsoft-to-stj-pipeline.`

> Note: the `SaveStreamDataInArchiveNodeTests.cs` left by rerere is the old-name STJ test — it is **rebuilt properly in Task B8**. It may not compile until then; that is expected.

### Task B1: Migrate `BackfillFromRtEntityNode` + its test

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Extract/BackfillFromRtEntityNode.cs`
- Modify: `octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/Nodes/Extract/BackfillFromRtEntityNodeTests.cs`

Known old-API call sites in the node:
- L31-32: `GetComplexObjectByPath<List<EntityUpdateInfo<RtEntity>>>(c.Path, RtNewtonsoftSerializer.DefaultSerializer)`
- L116-117: `SetValueByPath(c.Path, updateInfos, DocumentModes.Replace, ValueKinds.Simple, TargetValueWriteModes.Overwrite, RtNewtonsoftSerializer.DefaultSerializer)`

- [ ] **Step 1: Apply the substitution table to the node**

Replace the read with `dataContext.Get<List<EntityUpdateInfo<RtEntity>>>(c.Path, SystemTextJsonOptions.Default)` and the write with `dataContext.Set(c.Path, updateInfos, DocumentModes.Replace, ValueKinds.Simple, TargetValueWriteModes.Overwrite, SystemTextJsonOptions.Default)`. Update `using` if a Newtonsoft import is present. Mirror `FilterLatestUpdateInfoNode.cs`.

- [ ] **Step 2: Migrate the test data scaffolding to STJ**

In `BackfillFromRtEntityNodeTests.cs`, replace any `JObject`/`JArray`/`JToken` test inputs and `RtNewtonsoftSerializer` usage with `JsonObject`/`JsonArray` and the `NodeTestBase`/`PrepareTest<TConfig>` pattern used by existing migrated tests. Keep assertions semantically identical.

- [ ] **Step 3: Build just this project to check it compiles**

Run: `dotnet build octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/MeshAdapter.Sdk.Tests.csproj -c DebugL`
Expected: this file no longer reports `RtNewtonsoftSerializer`/`JObject` errors (other unmigrated nodes may still error — that's fine until Task B7).

- [ ] **Step 4: Run this node's tests**

Run: `dotnet test octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL --filter "FullyQualifiedName~BackfillFromRtEntityNodeTests"`
Expected: PASS (only meaningful once Task B7 makes the whole solution compile; if it can't compile yet, defer this run to Task B8 and just confirm the file is migrated).

### Task B2: Migrate `SaveTimeRangeStreamDataInArchive` + its test

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Load/SaveTimeRangeStreamDataInArchive.cs`
- Modify: `octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/Nodes/Load/SaveTimeRangeStreamDataInArchiveNodeTests.cs`

Known old-API call site in the node:
- L39-40: `GetComplexObjectByPath<List<EntityUpdateInfo<RtEntity>>>(c.Path, RtNewtonsoftSerializer.DefaultSerializer)`

- [ ] **Step 1: Apply the substitution table to the node**

Replace the read with `dataContext.Get<List<EntityUpdateInfo<RtEntity>>>(c.Path, SystemTextJsonOptions.Default)`. Mirror the already-correct `SaveStreamDataInArchive.cs` (it uses `Get<...>(c.Path, SystemTextJsonOptions.Default)` and `streamDataRepo.InsertAsync(archiveRtId, …)`).

- [ ] **Step 2: Migrate the test data scaffolding to STJ** (same approach as Task B1 Step 2).

- [ ] **Step 3: Compile-check** (same command as Task B1 Step 3).

### Task B3: Migrate `UpdateRtEntityIfNewerNode` (largest — also walks JToken)

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Load/UpdateRtEntityIfNewerNode.cs`

Known old-API call sites:
- L22-23: `GetComplexObjectByPath<List<EntityUpdateInfo<RtEntity>>>(c.InputPath, RtNewtonsoftSerializer.DefaultSerializer)`
- L178-179: `GetComplexObjectByPath<List<AssociationUpdateInfo>>(c.CandidateAssociationsInputPath!, RtNewtonsoftSerializer.DefaultSerializer)`
- L191-193 & L238-241: `SetValueByPath(..., RtNewtonsoftSerializer.DefaultSerializer)` (3 writes)
- ~L258-261: `switch` over `Newtonsoft.Json.Linq.JObject` / `JTokenType.Object` / `token.ToObject<object?>()`

- [ ] **Step 1: Apply the substitution table to reads/writes** (`Get<T>`/`Set` + `SystemTextJsonOptions.Default`).

- [ ] **Step 2: Rewrite the JToken type-switch to JsonNode**

Replace `case Newtonsoft.Json.Linq.JObject jo:` style branches with `JsonNode`/`JsonObject`/`JsonArray`/`JsonValue` pattern matching. For "is this an object?", use `node is JsonObject`; to materialize a scalar, use `JsonValue` + `GetValue<T>()`. Mirror how the branch's migrated transform nodes inspect `JsonNode` (see `DataMappingNode.cs`, `CreateUpdateInfoNode.cs`).

- [ ] **Step 3: Compile-check this project** (Task B1 Step 3 command).

### Task B4: Migrate `GenerateDataPointMappingsNode` + its test (builds JSON report)

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/GenerateDataPointMappingsNode.cs`
- Modify: `octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/Nodes/Transforms/GenerateDataPointMappingsNodeTests.cs`

Known old-API call sites: L14 `using Newtonsoft.Json.Linq;`; L53 `new JArray()`; L104-105 + L119-120 `SetValueByPath(..., RtNewtonsoftSerializer.DefaultSerializer)`; L109-117 + L263 `new JObject { … }` with `new JArray(...)` and `JObject.FromObject(rulesHitByRuleId)`.

This node is the direct sibling of the already-migrated `BuildMappingTargetsNode` / `ApplyDataPointMappingsNode` — **mirror them exactly**.

- [ ] **Step 1: Swap the `using` + builders + writes**

`using Newtonsoft.Json.Linq;` → `using System.Text.Json.Nodes;` (+ `using System.Text.Json;` because of `JObject.FromObject`). `new JArray()` → `new JsonArray()`. The statistics object becomes:

```csharp
var statistics = new JsonObject
{
    ["totalContainers"] = containers.Count,
    ["matchedContainers"] = matched,
    ["unmatchedContainers"] = unmatched.Count,
    ["unmatchedContainerNames"] = new JsonArray(unmatched.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray()),
    ["totalSuggestions"] = suggestions.Count,
    ["ruleHits"] = (JsonNode?)JsonSerializer.SerializeToNode(rulesHitByRuleId, SystemTextJsonOptions.Default),
    ["definedRuleIds"] = new JsonArray(rulesById.Keys.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray())
};
```

Both writes become `dataContext.Set(<path>, <value>, …, SystemTextJsonOptions.Default)`. The per-suggestion `new JObject { … }` at ~L263 becomes `new JsonObject { … }`.

- [ ] **Step 2: Migrate the test to STJ assertions** (read the produced `JsonObject`/`JsonArray` instead of `JObject`/`JArray`; mirror the branch's transform-node tests).

- [ ] **Step 3: Compile-check this project** (Task B1 Step 3 command).

### Task B5: Migrate `SimulateEnergyMeasurementsNode`

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/SimulateEnergyMeasurementsNode.cs`

Known old-API call sites: L120-123 two `SetValueByPath(..., RtNewtonsoftSerializer.DefaultSerializer)` writes (`entities`, `associations`).

- [ ] **Step 1: Convert both writes to `dataContext.Set(<path>, <value>, DocumentModes.Extend, ValueKinds.Simple, TargetValueWriteModes.Overwrite, SystemTextJsonOptions.Default)`.** Mirror `CreateAssociationUpdateNode.cs`. If `entities`/`associations` were `JArray`/`JObject`, switch to `JsonArray`/`JsonObject`.

- [ ] **Step 2: Compile-check this project** (Task B1 Step 3 command). (No new test file for this node on master — covered by build only.)

### Task B6: Migrate `ValidateDataPointCoverageNode` + its test (builds JSON report)

**Files:**
- Modify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/ValidateDataPointCoverageNode.cs`
- Modify: `octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/Nodes/Transforms/ValidateDataPointCoverageNodeTests.cs`

Known old-API call sites: L11 `using Newtonsoft.Json.Linq;`; L148-161 `new JObject { … }` with nested `new JObject`/`new JArray(...)`; L164-165 `SetValueByPath(..., RtNewtonsoftSerializer.DefaultSerializer)`; L192 `GetSimpleValueByPath<string>(c.RootRtIdPath)`; L326-337 `SerialiseNode` returns `new JObject { … }` with `JArray.FromObject(...)`.

- [ ] **Step 1: Swap `using` + builders + read + write**

`using Newtonsoft.Json.Linq;` → `using System.Text.Json.Nodes;` + `using System.Text.Json;`. `GetSimpleValueByPath<string>(c.RootRtIdPath)` → `dataContext.Get<string>(c.RootRtIdPath)`. `new JObject`/`new JArray` → `new JsonObject`/`new JsonArray`. `JArray.FromObject(r.Required)` → `(JsonArray)JsonSerializer.SerializeToNode(r.Required, SystemTextJsonOptions.Default)!` (and likewise for the other collections). The write becomes `dataContext.Set(c.TargetPath, report, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode, SystemTextJsonOptions.Default)`. Mirror `BuildMappingTargetsNode.cs`.

- [ ] **Step 2: Migrate the test to STJ assertions** (same approach as Task B4 Step 2).

- [ ] **Step 3: Compile-check this project** (Task B1 Step 3 command).

### Task B7: Verify the auto-merged `SaveStreamDataInArchive` node (HIGH-RISK read)

**Files:**
- Read/verify: `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Load/SaveStreamDataInArchive.cs`

The trial confirmed this node auto-merged to: `c.ArchiveRtId` guard → `new OctoObjectId(c.ArchiveRtId)` → `dataContext.Get<List<EntityUpdateInfo<RtEntity>>>(c.Path, SystemTextJsonOptions.Default)` → `streamDataRepo.InsertAsync(archiveRtId, toInsert)`. This is the one spot where master's rename/new-signature physically interleaved with the STJ rewrite, so it gets explicit eyes.

- [ ] **Step 1: Read the whole file and confirm coherence**

Run: `cat octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Load/SaveStreamDataInArchive.cs`
Confirm: no `RtNewtonsoftSerializer`/`JObject`/`JToken`; uses `SystemTextJsonOptions.Default`; `InsertAsync` is called with `archiveRtId` as first arg; the data path read matches what the branch's other load nodes do. If anything looks half-merged, reconcile by hand (STJ read + master's `ArchiveRtId`/`InsertAsync`).

- [ ] **Step 2: Confirm no residual old API anywhere in src/**

Run: `git -C octo-mesh-adapter grep -nE "RtNewtonsoftSerializer" -- 'src/**/*.cs'`
Expected: no output. (If any remain, a node migration was missed — go back to B1-B6.)

### Task B8: Rebuild `SaveStreamDataInArchiveNodeTests` (rename + STJ)

**Files:**
- Rewrite: `octo-mesh-adapter/tests/MeshAdapter.Sdk.Tests/Nodes/Load/SaveStreamDataInArchiveNodeTests.cs`

The rebase left the old-name STJ test here. Target: master's **names + new signature** (`SaveStreamDataInArchiveNode`, `SaveStreamDataInArchiveNodeConfiguration`, `ArchiveRtId`, `InsertAsync(ArchiveRtId, …)`) with **STJ test scaffolding** (the branch's `NodeTestBase`/`PrepareTest`, `JsonObject` test data — not `new JObject()`).

- [ ] **Step 1: Read both source versions to merge intent**

Run: `git -C octo-mesh-adapter show origin/main:tests/MeshAdapter.Sdk.Tests/Nodes/Load/SaveStreamDataInArchiveNodeTests.cs`
Run: `git -C octo-mesh-adapter show backup/pre-stj-rebase-2026-05-20:tests/MeshAdapter.Sdk.Tests/Nodes/Load/SaveInTimeSeriesNodeTests.cs`

- [ ] **Step 2: Write the reconciled test**

Use master's class name/structure and its `ArchiveRtId` + `InsertAsync(ArchiveRtId, A<IEnumerable<StreamDataPoint>>._)` assertions. Replace its Newtonsoft test data (`dataContext.Current` returning `new JObject()`, `JToken` inputs) with the STJ `NodeTestBase`/`PrepareTest<SaveStreamDataInArchiveNodeConfiguration>(config)` pattern and `JsonObject`/`JsonArray` inputs, mirroring the migrated `ApplyChangesNodeTests`/load-node tests. Every config uses `{ Path = DataPath, ArchiveRtId = ArchiveRtIdString }`.

- [ ] **Step 3: Confirm the test references the right node**

Run: `git -C octo-mesh-adapter grep -nE "SaveInTimeSeries" -- 'tests/**/*.cs' 'src/**/*.cs'`
Expected: no output (the old name is fully gone; node + test both use `SaveStreamDataInArchive`).

### Task B9: Verify the 6 new nodes are DI-registered

**Files:**
- Read/verify: the adapter builder registration (search for it).

A migrated node that loses its registration fails **silently at runtime**, not at compile time — so verify explicitly.

- [ ] **Step 1: Confirm each node type is registered**

Run: `git -C octo-mesh-adapter grep -nE "BackfillFromRtEntityNode|SaveTimeRangeStreamDataInArchive|UpdateRtEntityIfNewerNode|GenerateDataPointMappingsNode|SimulateEnergyMeasurementsNode|ValidateDataPointCoverageNode" -- 'src/**/DependencyInjection/**/*.cs' 'src/**/*Extensions*.cs'`
Expected: each node (or its `[NodeConfiguration]`-driven auto-registration) is present. Master added explicit registrations (e.g. `ValidateDataPointCoverageNode`); confirm they survived the rebase. If any is missing, add it next to its siblings.

### Task B10: Build octo-mesh-adapter in DebugL against fresh sdk NuGet + full test

**Files:** none

- [ ] **Step 1: Build the repo (resolves octo-sdk 999.0.0 from ../nuget)**

Run: `Invoke-Build -repositoryPath ./octo-mesh-adapter -configuration DebugL`
Expected: clean build (warnings-as-errors on). If a missing-package error appears, re-run Phase A Task A3 Step 1 first.

- [ ] **Step 2: Run unit tests (exclude system tests)**

Run: `dotnet test octo-mesh-adapter/Octo.MeshAdapter.sln -c DebugL --filter "FullyQualifiedName!~SystemTests"`
Expected: all pass, including the 4 migrated node test classes and the rebuilt `SaveStreamDataInArchiveNodeTests`.

- [ ] **Step 3: Commit the mesh-adapter adaptation**

```bash
git -C octo-mesh-adapter add -A
git -C octo-mesh-adapter commit -m "refactor(meshadapter): migrate new master nodes + tests to STJ after rebase"
```

---

## Phase C — Finalize

### Task C1: Whole-stack confirmation

- [ ] **Step 1: Confirm both branches sit on origin/main and trees are clean**

Run: `git -C octo-sdk status --short`
Run: `git -C octo-mesh-adapter status --short`
Run: `git -C octo-sdk log --oneline --graph -5`
Run: `git -C octo-mesh-adapter log --oneline --graph -5`
Expected: clean trees; feature commits stacked on `origin/main` tips.

- [ ] **Step 2: Report the final test results verbatim** (counts pass/fail from Task A3 Step 2 and Task B10 Step 2). Do not claim success without the test output.

### Task C2: Clean up scratch refs (only after the user confirms the result is good)

- [ ] **Step 1: Delete the trial branches**

```bash
git -C octo-sdk branch -D trial/stj-rebase
git -C octo-mesh-adapter branch -D trial/stj-rebase
```

- [ ] **Step 2: Keep the `backup/pre-stj-rebase-2026-05-20` branches** until the work is pushed/merged, then delete them the same way.

---

## Decisions / risks carried into execution
- **Decided:** accept master's deletion of `EnrichWithMongoDataNode` (+ its config). Recurs at 2 commits during rebase (`git rm` each time).
- **Confirm during B7/B8:** `SaveStreamDataInArchive` keeps master's rename + `ArchiveRtId`/`InsertAsync`; the branch's STJ read sits on top. Node already auto-merged coherently; only the test is rebuilt.
- **Cannot be auto-validated:** behavioral STJ↔Newtonsoft parity of the 6 migrated nodes beyond their own unit tests (no parity-corpus coverage like the original nodes have); integration/system tests needing live services; end-to-end pipeline execution.
- **Highest residual risk:** semantic drift in the 6 migrated nodes (null-vs-missing, number coercion, `JToken.ToString` formatting) — the exact bug classes the branch already fixed for the original nodes. Mitigate by mirroring the named reference nodes and keeping each migrated test's assertions semantically identical.
