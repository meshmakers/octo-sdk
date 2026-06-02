# Fully encapsulate System.Text.Json behind `IDataContext` + single-source the JSON primitives

**Status:** Draft for review
**Author:** Reimar Klammer (with Claude)
**Date:** 2026-05-21
**Branch:** `dev/newtonsoft-to-stj-pipeline` (octo-sdk, octo-mesh-adapter, octo-construction-kit-engine)
**Builds on:** `2026-05-06-newtonsoft-to-stj-pipeline-migration-design.md` (goal #4) and the three review rounds (`2026-05-08-…-r2`, `2026-05-19-…-cleanup`, `2026-05-20-rebase-…`).

---

## 1. Why

The Newtonsoft → System.Text.Json migration set **goal #4: "Hide JSON library specifics from pipeline-node authors. Public `IDataContext` exposes neither `JToken` nor `JsonNode`/`JsonElement`."** The shipped branch reached that goal only ~80%: it kept four deliberate boundary leaks (`EnumerateMatches → IEnumerable<JsonNode?>`, the `JsonSerializerOptions?` parameter on `Get`/`Set`, `INodeContext.CreateSubContext(JsonNode?)`, and generic `Get<JsonNode>`/`Set<JsonNode>` plumbing). Node authors who touched those leaks ended up reimplementing JSON glue by hand.

The reviews surfaced the concrete cost: the **same logic is reinvented across many nodes**, in slightly different and occasionally buggy variants (see §3). That fragmentation is exactly what goal #4 was meant to prevent.

This work **finishes goal #4**: STJ is removed from the node-author surface on both the read and write sides, and the JSON primitives that nodes were hand-rolling are consolidated into one canonical home each.

Three benefits drive it:

1. **Control of the exposed surface.** `IDataContext` (+ `INodeContext`, `DataKind`) becomes the *entire* vocabulary a node author has. The parity bugs the reviews found (strict `GetValue<T>`, the non-ASCII encoder divergence, compact-vs-indented stringification) were *only possible* because nodes could reach the raw types. Close the surface and that class of bug becomes unrepresentable.
2. **Testing.** With nodes depending only on `IDataContext`, a node test constructs an in-memory fake, feeds CLR data, runs the node, and asserts through `IDataContext` reads — no `JsonObject`/`JsonArray` scaffolding, no `JsonDocument` plumbing. Tests describe behavior, not JSON.
3. **No reinvention.** One `GetValue`, one `SelectMatches`, one scalar primitive → nodes cannot fork the logic.

## 2. Scope decision

**This lands inside the current migration PR** (decision recorded during brainstorming), as a full read + write encapsulation, protected by the test discipline in §7. It is a deliberate expansion of the migration's scope beyond strict parity, made consciously because it completes the migration's own stated goal rather than adding a new one.

### Non-goals

- Removing STJ from the *implementation*. STJ stays everywhere below `IDataContext` (the overlay, `JsonNodePath`, `LiteDbBsonConverter`, schema generators, the orchestrator, the debugger). We hide STJ from **node bodies**, not from the framework.
- New JSONPath dialect features.
- Touching the downstream adapter repos (`octo-adapter-eda`, `octo-plug-zenon`, `pipeline-editor`); they migrate later and benefit from the cleaner surface.
- A speculative general-purpose value builder. The write-side abstraction is built to fit the actual report-builder nodes (§6), nothing more (YAGNI).

## 3. The duplication this resolves (inventory)

**Cluster A — "unwrap a `JsonValue` to its Newtonsoft-equivalent CLR scalar."** The `bool → DateTime → long → double → string → ToJsonString()` ladder, hand-rolled in:
- `mesh-adapter` `CreateUpdateInfoNode.ExtractPrimitive` and `ApplyDataPointMappingsNode.UnwrapJsonNode` — **byte-identical**
- `mesh-adapter` `FieldFilterExtensions.GetComparisonValue` — same ladder **missing the `long` rung** (integers fall through to `double`)
- `mesh-adapter` `MinMaxNode` (inline, `DateTime/double/long` as `IComparable`)
- `mesh-adapter` `UpdateRtEntityIfNewerNode` (inline, `string/DateTime/DateTimeOffset`)
- `mesh-adapter` `AnomalyNodeHelpers.TryReadNumeric<T>`
- SDK cousins `DistinctNode` and `ConvertDataTypeNode` (over `JsonElement`)
- **Canonical authority already exists but is private:** `octo-construction-kit-engine` `RtAttributesConverter.MaterializeValue`.

**Cluster B — Newtonsoft-parity stringification.** `SDK JsonStringifyHelper.ToLegacyString` is the intended single stringifier but is `internal` to `Sdk.Common`, so mesh-adapter nodes fall back to raw `.ToJsonString()` (`AnomalyNodeHelpers`, `FieldFilterExtensions`, `ApplyDataPointMappingsNode`, `CreateUpdateInfoNode`, `MakeHttpRequestNode`).

**Cluster C — hand-rolled JSONPath navigation.** `SDK JoinNode.SelectRelativeNode` reimplements a weaker `Split('.')` walker that duplicates (and underperforms) `JsonNodePath.Select` — it silently fails on bracket/index keys.

**Cluster D — schema-gen string extraction.** `NodeSchemaRegistry` / `PipelineSchemaGenerator` repeat `is JsonValue v && v.TryGetValue<string>(out s)` ~7×. Build-time, low value; folded only opportunistically.

## 4. Architecture: the node-author surface

`IDataContext`, `INodeContext`, and `DataKind` are the only types a node `ProcessObjectAsync` may reference for data access. No `JsonNode`, `JsonElement`, `JsonValue`, `JsonObject`, `JsonArray`, or `JsonSerializerOptions`.

### 4.1 Reads — typed (hot paths, no boxing)

```csharp
T?    Get<T>(string path);                    // JsonSerializerOptions param removed (31 call sites pass only .Default)
bool  TryGet<T>(string path, out T? value);   // distinguishes missing vs present-but-null; tolerant coercion
IEnumerable<T?>? GetArray<T>(string path);
```

### 4.2 Reads — dynamic CLR value (the Cluster-A case)

```csharp
object? GetValue(string path, bool parseDateStrings = true);
```

Returns the natural CLR scalar (`bool` / `long` / `double` / `DateTime` / `string` / `null`) via the shared `JsonScalar` primitive (§5). `parseDateStrings` is the knob that preserves `DistinctNode`'s intentional no-date-parse behavior. For object/array kinds `GetValue` returns `null` — structured access goes through `Select`/`Get<T>` (the characterization tests confirm no current call site relies on the object→string fallback; if one does, it keeps an explicit stringify call).

### 4.3 Inspection (already STJ-free; unchanged)

```csharp
bool Exists(string path);  DataKind GetKind(string path);
int Length(string path);   IEnumerable<string> Keys(string path);
```

### 4.4 Navigation / multi-match — return `IDataContext`, never `JsonNode`

```csharp
IDataContext?              Select(string path);            // sub-context rooted at path, or null if absent
IEnumerable<IDataContext>  SelectMatches(string jsonPath); // replaces EnumerateMatches
// retained (already IDataContext-based): IterateArrayAsync, IterateObjectAsync,
// IterateMatchesAsync, UpdateMatchesAsync
```

- `SelectMatches` replaces `EnumerateMatches`. The returned contexts are **detached read views**: writing to one does **not** merge back into the source (that is `UpdateMatchesAsync`'s job). This matches the existing detached-`JsonNode` semantics, just typed.
- Returned sub/match contexts are **non-owning** — their `Dispose()` is a no-op (same as today's `DataContextChild`). A node never has to dispose what `Select`/`SelectMatches` hands back.

### 4.5 Writes — CLR in, no JSON types

```csharp
void Set<T>(string path, T? value);                                                   // options param removed
void Set<T>(string path, T? value, DocumentModes, ValueKinds, TargetValueWriteModes); // value is any CLR T / record / primitive
void Clear(string path);
void CopyTo(string sourcePath, string targetPath);
```

Report-builder nodes pass **typed records/POCOs** (`Set(path, new CoverageReport(...))`), not `JsonObject`. `Set<T>` already serializes any CLR `T` internally, so this needs no new write API for fixed-shape reports — and the report shape becomes typed, self-documenting, and testable. Genuinely dynamic shapes (e.g. CSV import → arbitrary columns) use a small builder scoped to exactly those nodes, or `Dictionary<string, object?>` where a builder would be overkill.

### 4.6 Removed from the surface

| Removed | Replacement | Consumers today |
|---|---|---|
| `EnumerateMatches → IEnumerable<JsonNode?>` | `SelectMatches → IEnumerable<IDataContext>` | `GetAssociationTargetsNode`, `FieldFilterExtensions`, `CreateUpdateInfoNode`, `MachineLearningAnomalyNode`, `StatisticalAnomalyNode`, `JoinNode` |
| `JsonSerializerOptions?` on `Get`/`Set` | none — context uses `SystemTextJsonOptions.Default` internally | 31 sites, **all pass `.Default`** |
| `INodeContext.CreateSubContext(JsonNode?)` | `CreateSubContext(IDataContext?)` (internal) | `ForNode`, `SwitchNode` (framework only) |
| `Get<JsonNode>` + manual unwrap | `GetValue` / `Select` / `Get<T>` | ~11 mesh-adapter sites |
| `JsonNodePath.Select` called from nodes | `Select` / match-context `GetValue` | `AnomalyNodeHelpers`, `MinMaxNode`, `GetRtEntitiesByWellKnownNameTypeNode` |

`WriteJsonTo(Stream)` / `SetFromJson(ReadOnlyMemory<byte>)` are retained (they traffic in `Stream`/`byte[]`, not STJ types) but moved to a clearly-internal/raw seam since no node consumes them.

## 5. Primitives and their homes (single source each)

```
octo-construction-kit-engine  (Runtime.Contracts/Serialization/)
  JsonScalar.ToClr(JsonElement|JsonValue, bool parseDateStrings)   ← the boxing LOGIC
  JsonScalar.TryToNumber<T>(JsonNode, out T)                       (replaces AnomalyNodeHelpers.TryReadNumeric)
        used by ──▶ RtAttributesConverter.MaterializeValue           (same layer)
        used by ──▶ IDataContext.GetValue / TryGet<T>                (octo-sdk, one layer up)

octo-sdk  (Sdk.Common/EtlDataPipeline/)
  IDataContext.GetValue / TryGet<T> / Select / SelectMatches      ← node-author SURFACE (calls JsonScalar)
  JsonNodePath                                                     (the only navigation impl; JoinNode routes through it)
  JsonStringifyHelper.ToLegacyString  → made `public`             (the only Newtonsoft-parity stringifier)
  [optional] value builder for the few dynamic-shape write nodes
```

- The scalar boxing **logic** must live in ck-engine because `RtAttributesConverter` needs it and ck-engine sits below the SDK. `IDataContext.GetValue` is a thin SDK surface over it. `RtAttributesConverter.MaterializeValue` keeps its own object/array/`RtRecord` recursion and calls `JsonScalar.ToClr` for the scalar arms → genuine single source for Cluster A across all three repos.
- `JsonScalar.ToClr` preserves the boxing rules `RtAttributesConverter` already documents: integers → `long`, reals → `double`, ISO-8601 strings → `DateTime` (when `parseDateStrings`), and the `long`/`double` boxing must stay an explicit `if`/return (not a ternary — a `long : double` conditional widens every integer to `double`).

## 6. The write side — typed records, not relocated `Dictionary`

The trap to avoid: rewriting `new JsonObject { ["x"] = 1 }` into `new Dictionary<string, object?> { ["x"] = 1 }` — that is the same untyped tree, uglier, and relocates the leak instead of removing it.

- **Fixed-shape reports** (the heavy builders: `ValidateDataPointCoverageNode`, `GenerateDataPointMappingsNode`, `BuildMappingTargetsNode`, `ToDiscordNode` payload, `MapToRecordArrayNode`, anomaly results) → define typed `record`s for the report shape and `Set(path, report)`. Strictly better than `JsonObject`.
- **Genuinely dynamic shapes** (`ImportFromCsvNode` → arbitrary columns) → a small purpose-built builder or `Dictionary<string, object?>`, scoped to those nodes only.
- **Framework / bridge code keeps STJ** (below the line): `LiteDbBsonConverter`, `NodeSchemaRegistry`, `PipelineSchemaGenerator`, the iteration nodes (`ForEachNode`/`ForNode`/`ObjectIteratorNode`), `JsonNodePath`, `HttpRequestService`. These are the implementation/serialization layer; STJ is correct there.

## 7. Zero-copy is a hard constraint (the original goal must not regress)

The migration's reason for existing was to eliminate per-iteration deep clones for deeply-nested `ForEach` over large data. This work must not regress that.

**Trace of copies under the new surface:**
- The perf-critical path (`ForEachNode`'s alias-based `IterateArrayAsync` + `DataContextChild`) is **untouched**: children share the same `JsonElement` base (struct copy), empty overlay, lift-on-write. The new methods are additions used by *other* nodes.
- `SelectMatches` **must** be implemented as thin `JsonElement`-backed read views (reusing `DataContextImpl(JsonElement)` with its lazy no-writes overlay), **not** `JsonNode.Parse` per match. Implemented that way it is *cheaper* than today's `EnumerateMatches`, which already does `JsonNode.Parse(element.GetRawText())` per match.
- `GetValue`/`Get<T>` read scalars off the `JsonElement` and are equal-or-better than today's per-read `JsonNode.Parse` in `Get<T>`.
- Write side: typed-record + `Set<T>` → one `SerializeToNode` tree bounded by *report* size — identical to today's `JsonObject` build.
- `GetValue` boxing is a tiny per-call alloc; hot numeric loops use `Get<T>`/`TryGet<T>` (unboxed).

**The rule:** `Select`/`SelectMatches`/match-contexts are `JsonElement` zero-copy views, never `JsonNode`-materialized copies. A naive `JsonNode.Parse`-per-match implementation would regress the very thing the migration was for, so this is enforced by the benchmark in §8, not by trust.

## 8. Test strategy — two mandatory guardrails, both before refactoring

1. **Behavior characterization (golden-master).** Before changing a line, pin the *current* output of every affected node/helper across a representative input matrix: int and real numbers, numeric strings, ISO and non-ISO date strings, booleans, `null`, nested object/array, and multi-match. Run → green baseline. Refactor onto the new surface → must stay green. Where behavior legitimately changes, the test changes **consciously** with a documented reason:
   - `FieldFilterExtensions` integer comparison value becomes `long` instead of `double` (fixes the missing-`long` rung).
   - `JoinNode` bracket/index join keys start resolving (fixes the silent no-match).
   Keep characterization tests that add real coverage (most affected nodes had none); delete pure scaffolding once the refactor is green.
2. **Allocation / peak-managed-heap benchmark.** The branch already has this harness (`docs/superpowers/plans/baseline-perf.txt` and the `test(perf)` ForEach peak-heap benchmarks). Capture the baseline now; the encapsulation must show **no peak-heap regression** on the deeply-nested-`ForEach`-over-large-data scenario. This benchmark is the proof that `Select`/`SelectMatches` stayed zero-copy and gates the merge.

## 9. Cross-repo sequencing

`octo-mesh-adapter` consumes `octo-sdk` via `../nuget` (`DebugL`, `999.0.0`); `octo-sdk` consumes `octo-construction-kit-engine` the same way. Work bottom-up, rebuilding NuGet after each layer.

1. **ck-engine:** add `JsonScalar` (`ToClr`, `TryToNumber<T>`); refactor `RtAttributesConverter.MaterializeValue` scalar arms onto it; characterization tests for `JsonScalar` + `RtAttributesConverter`. Build + pack.
2. **octo-sdk:** add `GetValue`/`TryGet<T>`/`Select`/`SelectMatches` to `IDataContext` (impl as `JsonElement` views); remove the `JsonSerializerOptions?` params; make `JsonStringifyHelper` public; route `JoinNode` through `JsonNodePath.Select`; migrate `DistinctNode`/`ConvertDataTypeNode` onto `JsonScalar` (preserving `DistinctNode`'s `parseDateStrings:false`). Characterization tests first; run the perf benchmark. Build + pack.
3. **octo-mesh-adapter:** migrate Cluster-A nodes onto `GetValue`/`SelectMatches`; delete `AnomalyNodeHelpers.TryReadNumeric` (→ `JsonScalar.TryToNumber`); rewrite the report builders onto typed records (write side); route stringification through public `JsonStringifyHelper`. Characterization tests first.
4. **Final:** `Invoke-BuildAll -configuration DebugL`; full test sweep on all three repos; perf benchmark shows no regression.

## 10. Risks & mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| `Select`/`SelectMatches` implemented as `JsonNode` copies → memory regression | medium | Hard design rule (§7) + peak-heap benchmark gate (§8). |
| Behavior drift in the many migrated nodes | medium | Characterization tests pinned **before** any refactor (§8); conscious, documented exceptions only. |
| Expanding an already-reviewed migration PR triggers another full review round | high | Accepted by the scope decision (§2); the two test guardrails make the diff verifiable rather than trust-based. |
| Write-side rewrite degenerates into `Dictionary<string,object?>` (relocated leak) | medium | §6 mandates typed records for fixed-shape reports; builder only where shape is truly dynamic. |
| `GetValue` object/array contract ambiguity | low | Returns `null` for non-scalars; structured access via `Select`/`Get<T>`; characterization tests confirm no current object→string-fallback dependency. |
| ck-engine is the lowest, most-shared layer; a `JsonScalar` bug is wide-blast | low | Pure function, exhaustively unit-tested; `RtAttributesConverter` parity tests already exercise the boxing rules. |

## 11. Decisions log (from brainstorming)

1. **Disposition:** fold the full encapsulation into the current migration PR.
2. **Breadth:** everything — fully hide STJ on **both** read and write sides.
3. **Primitive home:** ck-engine `Runtime.Contracts/Serialization` (`JsonScalar`), single source reused by `RtAttributesConverter` and the SDK surface.
4. **API shape:** small focused primitives under an `IDataContext` surface (`GetValue`/`TryGet<T>`/`Select`/`SelectMatches`), not one configurable god-converter and not bolted onto `JsonNodePath`.
5. **Write side:** typed records via `Set<T>`, not `Dictionary<string,object?>`.
6. **Discipline:** characterization tests + the existing peak-heap benchmark, both captured **before** refactoring; keep tests that add lasting coverage.

---

## Summary

Finish the migration's goal #4: `IDataContext` becomes the complete, STJ-free vocabulary for node authors (read **and** write). Consolidate the reinvented JSON glue into one home each — `JsonScalar` (ck-engine), `JsonNodePath` and public `JsonStringifyHelper` (sdk) — surfaced through `GetValue`/`TryGet<T>`/`Select`/`SelectMatches`. Hold the zero-copy line as a hard, benchmark-enforced constraint so the original memory goal is preserved, and pin every affected node's current behavior with characterization tests before touching it.
