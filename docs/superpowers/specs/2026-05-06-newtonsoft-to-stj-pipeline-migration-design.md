# Newtonsoft.Json → System.Text.Json Pipeline Migration

**Status:** Draft for review
**Author:** Reimar Klammer (with Claude)
**Date:** 2026-05-06
**Scope:** `octo-sdk` (Sdk.Common, Sdk.SimulationNodes), `octo-mesh-adapter`

---

## 1. Why we're doing this

The pipeline framework today uses Newtonsoft.Json's mutable tree types (`JToken` / `JObject` / `JArray`) as its native data model. The original reason for choosing Newtonsoft over System.Text.Json was JSONPath support. Two costs have grown important since:

**Memory.** Iteration nodes (`ForEachNode`, `ObjectIteratorNode`, `SelectByPathNode`) `DeepClone()` per iteration item. `ForEachNode` in particular wraps the full input subtree under `$.full`, then clones the entire template once per item:

- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Control/ForEachNode.cs:66-69, 100`
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/DataContext.cs:343` — `CreateChildDataContext` calls `input.DeepClone()`
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Control/ObjectIteratorNode.cs:61` — clones each array element
- `octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Control/SelectByPathNode.cs:95` — clones each path match

For a 50 MB input document with a 10 000-element iteration, this is hundreds of GB of allocation churn.

**Library tax.** Newtonsoft is no longer the modern default for .NET JSON; the rest of the .NET ecosystem has converged on `System.Text.Json`. Every dependency we ship with Newtonsoft is friction for downstream consumers.

## 2. Goals

1. Eliminate per-iteration deep clones in pipeline iteration nodes.
2. Replace Newtonsoft.Json with System.Text.Json across the pipeline framework (`octo-sdk` pipeline assemblies + `octo-mesh-adapter`).
3. Preserve full functional parity with existing pipelines — every production pipeline in `deployment/maco-deployment` and `deployment/energy-community-deployment` must continue to behave identically.
4. Hide JSON library specifics from pipeline-node authors. Public `IDataContext` exposes neither `JToken` nor `JsonNode`/`JsonElement`.

## 3. Non-goals

- DTO migration in `octo-sdk/src/Communication.Contracts/`. Those use `[JsonProperty]` attributes for SignalR/REST contracts for separate reasons (RestSharp, wire compatibility). Out of scope; they keep Newtonsoft via a direct package reference if needed.
- Migration of downstream adapter repos (`octo-adapter-eda`, `octo-plug-zenon`, `pipeline-editor`, etc.) within this work. Those will be updated in follow-up PRs after the SDK stabilizes; **temporary build breakage in those repos is accepted**.
- New JSONPath features beyond what current pipelines use.

## 4. Constraints

- `Sdk.Common` continues to multi-target `net10.0` and `netstandard2.0`.
- License-clean (MIT/Apache-2.0/BSD); no EULA on dependencies.
- No new third-party JSONPath library — see §6.

## 5. Architecture: layered data context

### 5.1 Concept

The new `IDataContext` is a **two-layer view**:

| Layer | Storage | Mutability |
|---|---|---|
| **Read base** | `JsonElement` (struct) referencing a `ReadOnlyMemory<byte>` UTF-8 buffer owned by an enclosing `JsonDocument`. | Read-only. Shared across iteration child contexts (zero-copy). |
| **Write overlay** | `Dictionary<string, JsonNode>` keyed by **subtree root paths**. Each entry holds the lifted, mutable `JsonNode` for that subtree. | Mutable. Per-context, never shared. |

The overlay is **subtree-rooted with copy-on-write semantics** (not a flat path-keyed dictionary). Mechanics:

- **First write at any descendant of path *P*** triggers materialization: the framework copies the base value at the nearest currently-tracked ancestor (or the smallest containing subtree) into a `JsonNode` in the overlay, and then applies the write to that node. Subsequent writes under the same subtree mutate the same `JsonNode` directly — no further allocation.
- **Reads at path *Q*** check whether `Q` falls inside any lifted subtree in the overlay (own context, then parent chain). If yes, they read from the lifted `JsonNode`, which already reflects every write under it. If no, they walk the read base via `JsonElement` (zero-copy).
- **The base is never mutated.**

This gives full Newtonsoft parity for the case where one sub-node writes a descendant and another reads its ancestor:

> **Example.** Read base has `$.a = { b: 1, c: 2 }`. Sub-node A writes `$.a.b = 99`. The framework lifts `$.a` from base into the overlay as a `JsonNode` and sets `b = 99` on that node. Sub-node B then calls `Get<JsonElement>("$.a")` or `IterateObjectAsync("$.a", ...)` — both observe the lifted node and see `{ b: 99, c: 2 }`. Reads of unrelated paths like `$.x.y` still walk the read base with zero allocation.

**Memory cost** is bounded by what is actually written, not by document size. In iteration, each child context typically writes a small value at `MergePath` — one `JsonNode` of the size of that value, allocated only when the write happens. The full document under `$.full` (the common iteration template) stays as a shared `JsonElement` view, untouched, zero copies — which is the whole point.

#### Parity invariant (the read–merge contract)

The overlay implementation **must** satisfy:

> For any write at path *P* in some context *C*, every subsequent read in *C* of *P* or any ancestor of *P* (including the root `$`) must observe the written value as if Newtonsoft had mutated the equivalent `JToken` tree in place. Reads in sibling and unrelated paths must be unaffected. Reads in the parent context (or any ancestor up the context chain) must also be unaffected.

The exact lifting/materialization algorithm — when to copy a subtree from the base, how coarse the lifted granularity should be, how the overlay key-set is maintained — is an implementation choice. The parity harness (§9) is the conformance test: any algorithm that satisfies the invariant *and* keeps overlay allocation proportional to writes (not to document size) is acceptable.

**Sub-nodes remain blind to iteration.** A node only knows its configured `TargetPath` and writes to it. Whether that write goes to a parent's overlay or a per-iteration child's overlay is determined by *which context* the node is invoked with — and the iteration framework owns that choice. Pipeline authors configure `MergePath` (on `ForEachNode`) or the equivalent collection path (on `SelectByPathNode`) to match the sub-node's `TargetPath`, and the parent extracts merged results from each child overlay after the iteration body completes.

### 5.2 Iteration without cloning

`IterateArrayAsync(path, body)`, `IterateObjectAsync(path, body)`, `IterateMatchesAsync(jsonpath, body)`:

- Each item produces a **child `IDataContext`** whose read base is the **same** `JsonElement` view as the parent (struct copy, no allocation).
- Each child gets a **fresh empty overlay** plus a small set of synthetic entries — e.g., `ForEachNode` writes the current iteration item under `$.key` in the child's overlay. No clone of the template.
- Read fallback chains through the parent context: child → parent → grandparent. So a child's read of `$.full.deep.path` resolves against the shared base via the parent's `JsonElement`.

The only "real" allocation in iteration is the merge step at the end, which reads each child's overlay value at `MergePath` and accumulates into the parent's target array. That single materialization is unavoidable and is what today's `targetArray.Add(mergeItem)` already does.

### 5.3 Public `IDataContext` API (path-only)

The node-author surface is path-only at the routine read/write level: `Get<T>(path)`, `Set<T>(path, value, ...)`, `Iterate*Async(path, body)`, `UpdateMatchesAsync(jsonPath, body)`. JSON types appear only at four deliberate boundary points:

1. **`EnumerateMatches(jsonPath)` returns `IEnumerable<JsonNode?>`** — for nodes that need synchronous multi-match read semantics matching the legacy `JToken.SelectTokens(...)`. Use `UpdateMatchesAsync` for the read-write case; `EnumerateMatches` is for read-only multi-match.
2. **`JsonSerializerOptions?`** as the optional last parameter on `Get<T>`/`Set<T>` — the standard STJ knob for type-conversion behavior. Defaulting to `PipelineJsonOptions.Default` keeps node code from passing it.
3. **`INodeContext.CreateSubContext(JsonNode?)`** — orchestrator extension point used by iteration nodes that need an explicit isolated sub-pipeline. Not part of the routine node-author surface.
4. **`Get<JsonNode>(path)` / `Set<JsonNode>(path, ...)`** — the generic `Get<T>`/`Set<T>` accept `JsonNode` for plumbing call sites that already hold a parsed subtree (orchestrator wiring, `NodeContext` debug logging, transform-internal subtree handling). Roughly ~50 call sites total. This is intentional, not aspirational — node authors writing fresh logic should still use a strongly-typed `T`, but plumbing code that already speaks `JsonNode` can keep doing so without forcing a round-trip through serialization.

```csharp
public interface IDataContext
{
    IDataContext? Parent { get; }

    // Inspection
    bool Exists(string path);
    DataKind GetKind(string path);          // Object | Array | String | Number | Boolean | Null | Undefined
    int Length(string path);                // for arrays / objects / strings
    IEnumerable<string> Keys(string path);  // object property names

    // Reads — typed projection
    T? Get<T>(string path, JsonSerializerOptions? options = null);
    IEnumerable<T?>? GetArray<T>(string path);

    // Writes — go to overlay
    void Set<T>(string path, T? value);                                              // simple, default modes
    void Set<T>(string path, T? value, DocumentMode docMode, ValueKind valKind,
                WriteMode writeMode, JsonSerializerOptions? options = null);
    void Clear(string path);

    // Iteration — yields child contexts internally; no clones
    Task IterateArrayAsync(string path, Func<IDataContext, Task> body);
    Task IterateObjectAsync(string path, Func<string, IDataContext, Task> body);
    Task IterateMatchesAsync(string jsonPath, Func<IDataContext, Task> body);

    // Multi-match read-write: invokes body for each match; mutations write back to this context's overlay
    Task UpdateMatchesAsync(string jsonPath, Func<IDataContext, Task> body);
    // (An IAsyncEnumerable<IDataContext> variant was considered during design and rejected
    // during implementation as redundant: the body itself already does per-match work, and the
    // caller never needs to enumerate matches independently of mutating them. Read-only
    // multi-match is covered by EnumerateMatches.)

    // Multi-match read-only: boundary leak — JsonNode for synchronous JToken.SelectTokens parity
    IEnumerable<JsonNode?> EnumerateMatches(string jsonPath);

    // Path manipulation
    void CopyTo(string sourcePath, string targetPath);

    // Raw JSON escape hatches (for HTTP, hashing, debug serialization)
    void WriteJsonTo(string path, Stream destination);
    void SetFromJson(string path, ReadOnlyMemory<byte> utf8Json);
}

public enum DataKind { Undefined, Null, Object, Array, String, Number, Boolean }
```

No `Current` property. Nodes that today read `Current` use `Get<T>("$")` or the `Iterate*Async` helpers. Nodes that today set `Current = ...` use `Set("$", value, ...)`. Today's `CreateCurrentIfNull` semantics are absorbed by the overlay — writing to a missing path auto-creates intermediate objects, same as `JTokenExtensions.ReplaceNested` does today.

### 5.4 What dies

- `JTokenExtensions.cs` — the `ReplaceNested` create-on-missing logic moves into the internal overlay implementation, not exposed.
- `IDataContext.Current` — replaced by path-rooted reads.
- `JsonSerializer` (Newtonsoft) parameter overloads — replaced by `JsonSerializerOptions?`.
- `JArray.FromObject(...)` / `JObject.FromObject(...)` — internal calls become `JsonSerializer.SerializeToNode(...)` or direct overlay writes.

## 6. Custom JSONPath evaluator

### 6.1 Why custom

The audit of in-tree code + 116 production pipelines confirms a small dialect (§6.2). Existing libraries either:
- don't support `JsonElement` (JsonPath.Net — operates on JsonNode, materializes)
- don't support netstandard2.0 (Hyperbee.Json — net8+ only)
- are stale or netstandard2.1+ (JsonCons.JsonPath)
- carry an EULA on the published binary (JsonPath.Net OSMF fee)

A custom evaluator is ~500–800 LOC, netstandard2.0-clean, JsonElement-native, no dependencies, dialect tuned to actual usage.

### 6.2 Required dialect

Minimum surface that handles 100% of in-tree + production usage:

| Feature | Example | Required |
|---|---|---|
| Root | `$` | yes |
| Dotted property | `$.foo.bar` | yes |
| Numeric array index (positive) | `$.arr[0]`, `$.arr[1]` | yes |
| Wildcard | `$.arr[*]`, `$.arr[*].name` | yes |
| Recursive descent | `$..foo`, `$..[*]`, `$..[?(...)]` | yes |
| Equality filter | `[?(@.field == 'literal')]` | yes |
| Array slice `[a:b]` | — | **no** — zero usage |
| Bracket-property `$['name']` | — | **no** — zero usage |
| Negative array index | — | **no** — zero usage |
| Regex match `=~` | — | **no** — zero usage |
| Function calls (`length()`, `keys()`) | — | **no** — zero usage |
| Bare paths (no `$`) | — | **no** — zero usage |
| Multi-clause filter (`&&`, `||`) | — | **no** — zero usage |

### 6.3 Implementation shape

Three components:

1. **Lexer + parser** producing a small AST: `Root`, `Property(name)`, `Index(int)`, `Wildcard`, `RecursiveDescent`, `Filter(propPath, operator, literal)`. ~200 LOC.
2. **Evaluator** walking a `JsonElement` against the AST, yielding `IEnumerable<JsonElement>` results with their canonicalized path strings (for overlay key matching). ~300 LOC.
3. **Path normalizer** producing canonical write paths from a JSONPath query, used to merge overlay writes onto the read base. ~50 LOC.

The evaluator never materializes a `JsonNode`. It works exclusively in struct-space.

### 6.4 Unsupported feature behavior

The evaluator throws `JsonPathNotSupportedException` (with the offending substring) for any unsupported feature. This makes future production pipelines using exotic features fail loud and early, instead of silently misbehaving.

### 6.5 The double-dot mid-path expression

`$.key..billingDocument..Items[0].RtId` (one occurrence in `energy-community-deployment/data/_calculation/energy-create-all-billing-documents.yaml`) uses recursive descent twice. Decision: **refactor that pipeline** to a more conventional shape (likely `$.key.billingDocument..Items[0].RtId` with single descent, or rewrite to follow the actual document shape exactly). Done as part of the migration PR.

## 7. Node refactors

### 7.1 RED: `UpdateRecordArrayItemNode` (octo-mesh-adapter)

**File:** `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/UpdateRecordArrayItemNode.cs:24-118`

**Problem:** Today it obtains a `JArray` view via `dataContext.Current.SelectToken(path)`, mutates a record's `Attributes` in place at line 112, and writes the same array reference back. Relies on Newtonsoft's parent-pointer mutation propagation. Breaks under read-only `JsonElement` base.

**Fix:** Iterate the matched records via `IterateArrayAsync`, and for each match build the updated attributes via overlay writes, then re-serialize the array to the target path. Reconstruction-style, not in-place. ~30 LOC change.

### 7.2 YELLOW: `ProjectNode`, `MapNode`, `DistinctNode`

Light refactors to use overlay writes instead of in-place token mutation. All three retain their public configuration shape. ~20 LOC each.

### 7.3 GREEN: everything else (≈70 nodes)

Mechanical migration only:
- `Newtonsoft.Json.Linq` using → removed
- `JToken` parameter and return types → removed (replaced by typed `Get<T>` / `Set<T>` calls)
- `JsonSerializer` overload parameter → `JsonSerializerOptions? options`
- `IsPathSimpleArrayValue` → `GetKind(path) == DataKind.Array`

### 7.4 Iteration nodes (`ForEachNode`, `ObjectIteratorNode`, `SelectByPathNode`)

Rewritten against the new layered API. The deep-clone-per-iteration disappears. `ObjectIteratorNode.ProcessToken`'s 36-line array-handling block becomes ~5 lines of `await dataContext.IterateArrayAsync(...)`.

## 8. LiteDB BSON converter

`octo-sdk/src/Sdk.Common/EtlDataPipeline/Nodes/Buffering/LiteDbBsonConverter.cs` is rewritten to convert between LiteDB BSON and `JsonNode` (since the buffering layer needs to materialize an explicit value to persist anyway). The buffering nodes (`BufferNode`, `BufferRetrievalNode`) call into the converter via the public `IDataContext` API — they `Get<JsonNode>` on commit, persist the resulting BsonValue, and `Set` on retrieval.

## 9. Parity test harness

A new test project: `octo-sdk/tests/Sdk.Common.PipelineParityTests/`.

**Setup:**
- References both `Newtonsoft.Json` 13.0.4 *and* the new STJ-based `Sdk.Common`.
- Bundles a **corpus of input documents** representative of production pipeline payloads. Sourcing strategy is an open question (see §12).
- Bundles every JSONPath expression extracted from in-tree code + the 116 production pipeline configs.

**Tests:**
For each (input, path) pair:
- Run Newtonsoft's `SelectToken` / `SelectTokens` against the input.
- Run the new evaluator against the same input.
- Assert structural equivalence of results (compare via JSON normalization — both serialized, parsed back, deep-equal-compared).

For each (input, path, value, write-mode) write tuple:
- Run today's `JTokenExtensions.SetValueByPath` to produce the expected output JSON.
- Run the new `IDataContext.Set` against an equivalent context.
- Assert the materialized result matches.

This harness is the single biggest mitigation against silent regressions. It runs in CI as part of `Sdk.Common` tests. After the migration is complete and stable, the parity harness can be archived (it requires Newtonsoft to keep working, which we want to drop eventually).

## 10. Migration sequencing

Strategy **A** (coordinated cutover) confirmed earlier. Order of work in one branch / one PR (or a small chain of PRs in lockstep):

1. **Pre-work parity harness scaffolding.** Set up the `Sdk.Common.PipelineParityTests` project structure with both packages referenced. Add corpus loaders. (Tests fail until step 4 is done — that's expected.)
2. **Internal migration in `Sdk.Common`.** Implement the layered context, custom evaluator, new `IDataContext`, internal types. Old `JTokenExtensions` and old `DataContext` deleted. Newtonsoft package reference removed from `Sdk.Common.csproj`.
3. **Refactor RED + YELLOW nodes** in `Sdk.Common`. Refactor iteration nodes against the new API.
4. **Migrate GREEN nodes** in `Sdk.Common` (mechanical). Migrate `Sdk.SimulationNodes`. Migrate buffering + LiteDB converter.
5. **Migrate tests** in `tests/Sdk.Common.Tests/` and friends. Run parity harness; resolve mismatches.
6. **Migrate `octo-mesh-adapter`.** All nodes in `MeshAdapter.Sdk/Nodes/**` migrated. `UpdateRecordArrayItemNode` gets its reconstruction refactor. Tests migrated.
7. **Refactor double-dot pipeline** in `energy-community-deployment/data/_calculation/energy-create-all-billing-documents.yaml`.
8. **End-to-end test of `octo-mesh-adapter`** against the deployment pipeline corpus. Run the actual mesh-adapter against representative pipelines from `maco-deployment` and `energy-community-deployment` and confirm equivalent behavior.
9. **Coordinated commit.** All of the above lands together. Other repos break — that's accepted.
10. **Follow-up PRs** for downstream adapter repos (`octo-adapter-eda`, `octo-plug-zenon`, `pipeline-editor`) in priority order.

## 11. Risks & mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Production pipelines use a path pattern not in our audit | medium | Custom evaluator throws `JsonPathNotSupportedException` for unrecognized features → fails loud, fast. Easy to add support if a real case appears. |
| Parity harness misses an edge case (e.g., specific number-format quirk) | medium | Run mesh-adapter end-to-end against deployment pipelines as the final acceptance gate (step 8). |
| `JsonElement.GetProperty` case-sensitivity differs from Newtonsoft's defaults | low | Normalize property name casing in evaluator if needed; verify in parity tests. |
| `JsonValueKind.Number` collapsing int/float breaks numeric-typed reads | low | `Get<int>`, `Get<double>` etc. handle conversion; covered in parity tests. |
| Overlay-key canonicalization bug → reads return base value when overlay should win | medium | Comprehensive overlay tests in `Sdk.Common.Tests`. Specifically test path equivalence: `$.foo[0].bar` vs `$['foo'][0]['bar']` resolve to the same canonical form. |
| Subtree-lifting algorithm fails the read–merge invariant — i.e., a read of an ancestor path doesn't observe a prior descendant write | high | The parity invariant in §5.1 is the contract; the parity harness (§9) is the conformance test. Add targeted overlay tests: write at `$.a.b.c.d`, then read `$.a`, `$.a.b`, `$.a.b.c`, root `$`; assert each reflects the write. Test the same with multiple writes to disjoint subtrees, and with writes that overlap (write `$.a.b`, then write `$.a.b.c`). |
| Hot-path perf regression vs. Newtonsoft despite zero-clone | low | Benchmark `ForEachNode` with a realistic input size before/after. Include in CI. |
| netstandard2.0 target restricts use of Span APIs | low | `JsonElement` itself is netstandard2.0-compatible; we don't need exotic Span features. |

## 12. Open questions

- **Parity harness corpus.** Where do the synthesized inputs come from? Proposal: extract a sample document per pipeline by running the existing mesh-adapter once with payload capture enabled. Decide as part of step 1.
- **Schema generation.** `PipelineSchemaGenerator` uses `JsonSchema` from NJsonSchema (which transitively pulls Newtonsoft). Need to verify NJsonSchema can run in pure-STJ mode, or accept a transitive Newtonsoft via that one dependency. Likely a small follow-up.

---

## Summary

- **Public API:** path-only `IDataContext` for routine reads/writes; JSON types appear only at three deliberate boundary points (`EnumerateMatches` return, `JsonSerializerOptions?` on `Get`/`Set`, `INodeContext.CreateSubContext`).
- **Internal model:** layered (read-only `JsonElement` base + sparse `JsonNode`/POCO write overlay), zero-copy iteration.
- **JSONPath:** custom evaluator, ~500–800 LOC, dialect tuned to actual usage.
- **Migration:** coordinated cutover (`octo-sdk` + `octo-mesh-adapter` together), one branch.
- **Safety net:** Newtonsoft↔STJ parity test harness with corpus from production pipelines.
- **Refactors required:** 1 RED node (`UpdateRecordArrayItemNode`), 3 YELLOW nodes, 3 iteration nodes, 1 production pipeline (double-dot expression).

---

## Addendum (2026-05-28): Numeric round-trip parity — Newtonsoft as the oracle

### Background

Post-migration, the `octogrid` tenant's `RtEntity_EnergyCommunityEnergyQuantity` collection began producing rows with the wrong BSON types — `dataQuality` (declared `Enum`, underlying `Int32`) was being written as `BsonInt64`, and `quantity` (declared `Double`) was occasionally written as `BsonInt64` for whole-number sources. GraphQL.NET's strict-type enum resolver failed on every such row.

Root-cause investigation traced both regressions to the **typed → JsonNode → typed** round-trip inside `DataContext.Set(typed) → Get<typed>` (via `JsonSerializer.SerializeToNode` + `JsonNode.Deserialize<T>`):

1. STJ's `JsonNode` does not preserve the source CLR type for numbers (`int 1` and `long 1` both serialize to the JSON literal `1`).
2. `JsonScalar.ToClr` (the centralized boxing primitive used by `RtAttributesConverter` and every pipeline node that reads scalars) widened all integers to `long`.
3. `JsonSerializer.Serialize(double 0.0)` writes the literal `0` (no trailing `.0`) — indistinguishable from an integer on the wire, so `JsonScalar.ToClr` re-boxes it as `long`.

The pre-migration Newtonsoft path **did not** have these issues. `JObject.FromObject(int 1)` produces a `JValue` whose internal `Value` is `Int32`; the in-memory round-trip via `RtNewtonsoftAttributesConverter` preserves the source CLR type via that side-channel. `JsonConvert.ToString(double 0.0)` emits `0.0` with the trailing decimal point.

### Parity contract — Newtonsoft is the oracle

`Sdk.Common.PipelineParityTests` is the authoritative contract for STJ ↔ Newtonsoft behavioural parity in the pipeline data path. The suite:

- Enumerates a corpus of attribute-value CLR types in `AttributeValueParityCorpus` (ints across the Int32 boundary, doubles/floats/decimals including whole-number cases, DateTimes / DateTimeOffsets across Kinds and offsets, strings with non-ASCII and HTML-sensitive characters, etc.).
- For each case, runs the **in-memory** round-trip through both libraries: Newtonsoft (`JObject.FromObject` → `JToken.ToObject<Dictionary<string, object?>>`, using `RtNewtonsoftSerializer.DefaultSerializer`) and STJ (`JsonSerializer.SerializeToNode` → `JsonNode.Deserialize<IReadOnlyDictionary<string, object?>>`, using `RtSystemTextJsonSerializer.Default`). The in-memory paths reproduce production (`DataContext.Set/Get<T>`) — *text* round-tripping is more lossy than the in-memory round-trip and would mask the regressions.
- Asserts the deserialized CLR types match. Newtonsoft's behaviour defines correctness; any STJ divergence is a bug unless listed in `AttributeValueParityCorpus.IrreducibleDivergences`.

If the parity rules drift again (e.g. a future STJ converter change), this suite catches it immediately. No prose-only spec is needed — the tests are the spec.

### Irreducible divergences

Some divergences are physical: JSON has one number token type, no source-CLR-type marker. Newtonsoft preserves these via `JValue`'s typed `Value` slot; STJ cannot.

| Source CLR type | Newtonsoft round-trip | STJ round-trip | Notes |
|---|---|---|---|
| `float` | `float` | `double` | JSON has no `float` marker. |
| `decimal` | `decimal` | `double` | Precision loss possible for high-precision decimals. |
| `DateTimeOffset` | `DateTimeOffset` | `DateTime` | JSON encodes offset in the string but STJ's deserialize path returns `DateTime`. |

Consumers that need exact source-CLR-type preservation must use the typed accessor path (`GetAttributeValue<decimal>`, the typed property on the DTO) rather than the raw attribute-dict round-trip. Documented in `AttributeValueParityCorpus.IrreducibleDivergences` with per-case reasons.

### Implementation

- `octo-construction-kit-engine/src/Runtime.Contracts/Serialization/JsonScalar.cs` — `ToClr` prefers `TryGetInt32`, falls back to `TryGetInt64`, then `GetDouble`. Boxing matches `JObject.FromObject(int)` → `JValue.Value=Int32`.
- `octo-construction-kit-engine/src/Runtime.Contracts/Serialization/NewtonsoftParityNumberConverters.cs` — new STJ converters for `double` / `float` / `decimal` that emit `.0` for integral values, mirroring `JsonConvert.ToString`. The Read path delegates to the built-in reader; both paths re-implement `JsonNumberHandling.AllowNamedFloatingPointLiterals` and `AllowReadingFromString` since custom converters bypass STJ's built-in number handling.
- `octo-construction-kit-engine/src/Runtime.Contracts/Serialization/RtSystemTextJsonSerializer.cs` — registers the three converters in the canonical options bundle. `SystemTextJsonOptions.Default` (the pipeline options) cascades from here.
- `octo-construction-kit-engine/src/Runtime.Engine/Blueprints/BlueprintMigrationExecutor.cs` — `ParseAttributeUpdates` now routes through `JsonScalar.ToClr` instead of a hand-rolled switch (which had the same int → long regression).
- `octo-mesh-adapter/src/MeshAdapter.Sdk/Nodes/Transform/MinMaxNode.cs` — comparison switch handles `int` in addition to `long`.

### Verification

- `Sdk.Common.PipelineParityTests.AttributeRoundTripClrTypeParityTests` — 42-case corpus, 32 cases at parity, 10 documented irreducibles.
- `Sdk.Common.Tests`, `Meshmakers.Octo.Runtime.Engine.Tests`, `Meshmakers.Octo.ConstructionKit.Engine.Tests`, `MeshAdapter.Sdk.Tests`, `EdaAdapterTests` — all unit suites green after assertion flips for tests that had encoded the buggy behaviour as expected.
- Production verification: re-run an EDA `handle-daten-crmsg.yaml` batch against staging and confirm `dataQuality` lands as `BsonInt32` and `quantity` lands as `BsonDouble` (not `BsonInt64`). The pre-existing `(quantity=0, BsonInt64)` and `(dataQuality=1, BsonInt64)` rows in `octogrid` are the regression evidence.
