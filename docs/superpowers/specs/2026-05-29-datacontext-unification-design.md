# DataContext Unification — Design Spec

- **Date:** 2026-05-29
- **Status:** Proposed (awaiting review)
- **Work item:** AB#3517 (STJ pipeline migration)
- **Predecessor:** `2026-05-06-newtonsoft-to-stj-pipeline-migration-design.md`

> ⚠️ **Divergence notes appended (2026-05-30) — see §12.** Parts of this spec (including the §11 as-built snapshot) were overtaken by later work: the `IReadSource.Evaluate` shape, the memory-gate isolation, and a subtree-direct-read optimization that was considered and dropped. The original text below is preserved as the point-in-time record.

## 1. Context & goal

The STJ migration replaced Newtonsoft `JToken` pipeline data with a path-only `IDataContext` backed by a **layered model**: a zero-copy `JsonElement` read base + a sparse `JsonNode` overlay that lifts on write; iteration children are built zero-copy from aliases + parent-fallback instead of per-item `DeepClone`. That model achieved its purpose (ForEach benchmark 924 MB → 82 MB).

**Goal of this change:** a *maintainable* implementation of that *performant, low-memory* model. Product-owner driver, verbatim: *"a maintainable implementation of a performant and low-memory profile"* — reduce memory on **big documents**, with correctness & maintainability always paramount in this hot path.

## 2. Problem

1. **Duplicated implementations.** Every `IDataContext` member is hand-written **twice** — once in the root `DataContextImpl`, once in the nested `DataContextChild` (`src/Sdk.Common/EtlDataPipeline/DataContext.cs`). This duplication caused a production memory regression: the parent's `SelectMatches` had an `_overlay.HasWrites` fast path the child lacked, so the child re-serialized the whole alias-augmented document ~4× per call. Inside `ForEach`, `CreateUpdateInfoNode` calls it once per attribute `valuePath`, giving `O(messages × valuePaths × docSize)` LOH churn (~4.5 GB LOH/run on the EDA pipeline). **The instance is fixed (commit `1061e47`); the *class* of bug — "an optimization added to one implementation but not the other" — remains.**
2. **Dual walkers.** Reads use two JSONPath walkers — `JsonPathEvaluator` (over `JsonElement`) and `JsonNodePath` (over `JsonNode`) — kept equivalent only by `WalkerParityTests`. A standing maintenance burden and divergence risk.

## 3. Decision

Adopt the **"all-three"** design:

- **One** `DataContextImpl`, parameterized over an `IReadSource` seam (`ElementSource` | `LayeredSource`). Every member implemented exactly once.
- **One** generic, struct-constrained JSONPath walker over `IJsonView<TSelf>`, replacing **both** `JsonPathEvaluator` and `JsonNodePath`. Retires the parity-test burden structurally.
- **Preserve the dual representation** — immutable `JsonElement` base for cheap big-doc reads + mutable `JsonNode` overlay for writes. This is load-bearing for the memory goal.

**Rejected:** collapsing to a single `JsonNode` DOM. A big read-mostly document as a `JsonNode` tree is `O(size)` managed heap + GC; as a `JsonElement` view it is a struct over one pooled buffer (~zero managed heap). Collapsing would regress the migration's entire purpose.

**Evidence the single walker is viable** (throwaway spike, 2026-05-29): a generic `Walk<TView> where TView : struct, IJsonView<TView>` over `JsonElement` allocated **0.69×** the native `JsonPathEvaluator` on the worst case (`$..` recursive descent, 12 000 matches, *identical* match set) — no boxing. The `struct` constraint makes interface calls *constrained* (JIT-monomorphized, non-boxing). The `JsonNode` side measured ~1.28× the hand-tuned `JsonNodePath` — acceptable, because that path handles small per-item/overlay data, not the big base document.

## 4. Architecture

### 4.1 The unified walker

- **`IJsonView<TSelf> where TSelf : struct, IJsonView<TSelf>`** — minimal, F-bounded node abstraction. Operations the walker + reads need: `ValueKind Kind`, `bool TryGetProperty(string, out TSelf)`, `bool TryGetIndex(int, out TSelf)`, `IEnumerable<TSelf> EnumerateChildren()` (array elements / object property-values), object key enumeration for canonical paths, `string GetRawText()` (match materialization), and scalar accessors for filter predicates.
- **Two `readonly struct` views:** `ElementView` (wraps `JsonElement`; zero-copy) and `NodeView` (wraps `JsonNode`).
- **`JsonPathWalker`** — one generic implementation of the full dialect: root, property (incl. the RtCkId `SemanticVersionedFullName`/`FullName` shim), index, wildcard, recursive descent, filter, bracket-property. It **threads the canonical path** through the walk and yields `(match, canonicalPath)` — the write-back capability `JsonNodePath` lacked.
- Shared front-end `JsonPathParser` + `PathSegment` AST (already shared) feeds the walker.
- The walker is invoked with the **concrete struct view inside each `IReadSource`** (`Walk<ElementView>` / `Walk<NodeView>` — monomorphized, no boxing). Per-match materialization (`GetRawText()` → wrap) happens at the seam boundary — the same cost as today.

### 4.2 The read-source seam

```csharp
internal interface IReadSource
{
    bool HasWrites { get; }                                  // overlay fast-path discriminator
    bool IsTombstoned(string canonicalPath);
    bool TryResolve(string path, out ResolvedRead read);     // single-path read (Get/GetValue/GetKind/Exists), no materialization
    IEnumerable<MatchRef> Evaluate(JsonPathExpression expr);  // multi-match: (canonicalPath, rawJson) via the unified walker
    void SeedAncestorsForWrite(string path, DataOverlay overlay); // §5.1 seeding; no-op on ElementSource
}
```

- **`ElementSource`** — wraps `JsonElement _base` (+ owned `JsonDocument`). No-writes reads walk `_base` via `ElementView` (zero-copy). When `HasWrites`, reads the overlay's lifted node via `NodeView`. No aliases. `SeedAncestorsForWrite` is a **no-op** (the root overlay lifts a full base copy, so siblings already exist).
- **`LayeredSource`** — wraps `_aliases` + parent-fallback (`IDataContextFallbackSource`) + its own overlay over the shared empty base. Reads compose tombstone → overlay → alias → parent. Multi-match builds the **alias-pruned eval root once** (the shipped fix) and walks via `NodeView`. `SeedAncestorsForWrite` is the **real** ancestor-seeding.

`MatchRef`/`ResolvedRead` are small boundary types carrying raw JSON / kind + canonical path. The per-step walk stays monomorphized-generic (no boxing); only the boundary materializes (unchanged cost).

### 4.3 One `DataContextImpl`

Holds `_overlay` (mutation; **`DataOverlay` is unchanged**) + `_source : IReadSource` (reads) + `_ownedDocument`. Every member is implemented once: reads route through `_source`, writes through `_overlay` (+ `_source.SeedAncestorsForWrite` before non-root/non-Replace writes). Construction: element ctor → `ElementSource`; a single `CreateChild(parent, aliases)` factory → `LayeredSource`. **`DataContextChild` is deleted.**

The one former behavioural divergence — child-only `SeedAncestorsFromParent` — becomes the seam's `SeedAncestorsForWrite` (real on `LayeredSource`, no-op on `ElementSource`), so the §5.1 invariant is a property of the read-source, not a hand-copied branch. `CopyTo`/`SetFromJson` keep their raw-overlay-write path (they intentionally do **not** seed).

## 5. Invariant preservation

The refactor must preserve every load-bearing invariant. Key ones and how:

| Invariant | How preserved |
|---|---|
| §5.1 ancestor seeding (child-only, all write modes) | `IReadSource.SeedAncestorsForWrite`: real on `LayeredSource`, no-op on `ElementSource`; called once in the unified `Set` before the write-mode switch |
| Explicit-null vs undefined | Unchanged `DataOverlay` (`TryRead` true+null ⇒ Null); null⇒`Null` checks written once |
| Tombstone shadowing | Unchanged overlay tombstones + `IsPathTombstoned` chain; `LayeredSource` keeps the short-circuits |
| `_overlay.HasWrites` fast path | Surfaced as `IReadSource.HasWrites`; **child gains the fast path it lacked** |
| Aliases top-level-only (`$.name`) | Owned by `LayeredSource`; pruning + single-segment check ported verbatim |
| Detached match/sub-context owns its `JsonDocument`, survives parent dispose | Every match re-parses raw text into a fresh owned `DataContextImpl` |
| Replace vs Extend | Identical `Set` step; `SeedAncestorsForWrite` guarded off for Replace/root |
| Append / Prepend / Merge | Shared `*Core` helpers unchanged; called from the single `Set` |
| L3 nested-iteration parent-fallback | `CreateChild(this, …)` passes the unified instance as `IDataContextFallbackSource`; uniform because root & child are one type |
| Write-back via `Set` (child seeds; root raw) | Unified `UpdateMatchesAsync` always `Set(canonicalPath, …)`; `SeedAncestorsForWrite` no-op on `ElementSource` reproduces the root's raw-write behaviour |
| Match paths captured before mutation | Collect `(canonicalPath, rawJson)` before the write-back loop |
| RtCkId `SemanticVersionedFullName`/`FullName` shim across all readers | Implemented once in the unified walker + overlay + alias reader |
| Iteration item always deep-cloned before seeding | Unchanged at the single `CreateChild`+seed site |

Full invariant catalogue is enumerated in the implementation plan.

## 6. Test strategy

- **Characterization-first.** Before refactoring, ADD tests pinning under-covered **child** behaviours: Replace; append/prepend/merge on arrays living only in the parent-fallback; nested-iteration fallback (grandchild through middle child); `UpdateMatches` write-back without the manual `$`-seed workaround; child `Select` survival after parent dispose; child read parity for `GetValue`/`TryGet`/`GetKind`/`Length`/`Keys`. Plus a `GetValue` fast-path-vs-slow-path parity test (`JsonScalar.ToClr(JsonElement)` ≡ `ToClr(JsonValue)`).
- **Walker correctness.** The `WalkerParityTests` corpus migrates to the unified walker as **cross-view** parity (`ElementView` vs `NodeView` must yield identical matches) during the transition; once both old walkers are deleted there is one walker, so parity is structural. Expand the corpus for canonical-path emission.
- **Memory gates (load-bearing; run after every step):** the `ForEachMemoryBenchmark` 300 MB ceiling; the existing `DataContextChildSelectMatches` allocation gate; and a **new big-doc read allocation gate** — a read-only `SelectMatches`/`GetValue`/`Iterate` on a root must not scale with document size (zero-copy proof). Use `GC.GetAllocatedBytesForCurrentThread()` (process-wide counters are polluted by parallel test execution).
- **Newtonsoft-oracle `PipelineParityTests`** stay green throughout.
- **End-to-end verification.** Re-profile the EDA pipeline (`receive-eda-messages-manual`, octogrid): total allocation and LOH must stay **≤** the post-fix level (4731 → 1287 MB / 4522 → 284 MB), and `SelectMatches` must be absent from the hot allocation stacks.

## 7. Implementation phasing

Granular red→green steps live in the implementation plan; the shape:

- **Phase 0 — Complete the safety net.** Characterization tests above. All green, no production change.
- **Phase 1 — `IReadSource` + `ElementSource`; route the root** (child untouched). Green.
- **Phase 2 — `LayeredSource`; route the child reads** (child still its own type). Green.
- **Phase 3 — Unified `IJsonView<TSelf>` walker + `ElementView`/`NodeView`.** Route both sources' multi-match through it behind the cross-view corpus; keep the old walkers until that corpus is green; then delete `JsonPathEvaluator`/`JsonNodePath`. Green.
- **Phase 4 — Seam writes.** `SeedAncestorsForWrite`; unify `Set`; unify `UpdateMatches` write-back (canonical paths from the unified walker). Green.
- **Phase 5 — Collapse `DataContextChild` into `DataContextImpl`** over `LayeredSource`; single `CreateChild` factory; delete duplicated members. Green + memory gates + cross-repo `octo-mesh-adapter` build.
- **Phase 6 — Verify & review.** Re-profile EDA pipeline; adversarial review (parity, ownership/dispose, ForEach concurrency, memory).

Every step: full `Sdk.Common.Tests` + `PipelineParityTests` + memory gates green; individually revertible; never a long red window.

## 8. Risks & kill-switches

- **Walker boxing/perf** — spike proved the element path is *leaner*; gate = the big-doc read allocation test after every step; any regression ⇒ revert that step.
- **`JsonNode`-path ~28% overhead** — acceptable; if it surfaces in the EDA re-profile, the seam permits keeping `JsonNodePath` for `LayeredSource` only (fallback) without reintroducing the parity burden on the element path.
- **Ownership/dispose** — `ElementSource` owns the document; `LayeredSource` owns nothing; detached matches re-parse into fresh owned docs. Survival tests guard; kill-switch = revert Phase 5 to the two-type state.
- **ForEach concurrency** — `CreateChild` builds an independent `LayeredSource` + fresh overlay per item; `ForEachNodeTests` + `IterationParityTests` guard.
- **Canonical-path emission (new in the unified walker)** — covered by `UpdateMatches` characterization tests *before* the switch.

## 9. Out of scope

- `yield`-based per-node enumerator allocation (affects both walkers equally today; a future struct-enumerator/visitor optimization).
- Any node refactor beyond what consuming the unified `IDataContext` requires.

## 10. Build / run

`dotnet test tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj -c DebugL` and `tests/Sdk.Common.PipelineParityTests/...` (both xunit.v3; `dotnet test` works). Cross-repo propagation to `octo-mesh-adapter` via `Invoke-BuildAll` (single-repo build does not refresh the `999.0.0` global NuGet cache for downstream consumers).

## 11. As-built notes

Implemented on `dev/reimar/stj-pipeline-migration` (squashed commit). The shipped design matches §3–§5; minor deltas from the §4.2 sketch and the final outcome:

- **`IReadSource` (final members):** `bool PathExists(string)`, `JsonNode? TryGetNode(string)`, `DataKind GetKind(string)`, `object? GetValue(string, bool parseDateStrings)`, `IEnumerable<(string CanonicalPath, string RawJson)> Evaluate(string)`, `void SeedAncestorsForWrite(string)`, `bool IsPathTombstoned(string)`. The §4.2 sketch's `HasWrites`/`TryResolve` were dropped — `HasWrites` is encapsulated inside each source's fast/slow split rather than crossing the seam, and `TryResolve` was realised as the concrete `GetKind`/`GetValue`/`TryGetNode`/`PathExists` reads (preserving the `JsonElement` zero-copy fast path inside `ElementSource`).
- **Walker:** `JsonPathEvaluator` was deleted; `JsonNodePath` was trimmed to its write/normalize API (`Set`/`Remove`/`NormalizePathOrRelative`/`ParseDottedSegments`). The one `JsonPathWalker.Select<TView>` is IL-verified non-boxing and is Newtonsoft-oracle-guarded (`ReadParityTests`) plus 30 cross-view + golden canonical-path tests. A shared `DataKindMapper` deduplicates the `JsonElement`/`JsonNode` → `DataKind` mapping.
- **Result (same pipeline, debugging on) vs the Newtonsoft baseline:** total alloc/run **2412 → 1384 MB (−43%)**, LOH **1106 → 278 MB (−75%)**, peak working set **1721 → 986 MB (−43%)** — below the baseline the migration set out to beat (vs the pre-fix STJ state: 4731 → 1384 MB alloc, 4522 → 278 MB LOH). Net ≈ −440 LOC; 750 `Sdk.Common` + 171 `PipelineParity` tests green.
- **Operational:** the profiling pipeline runs with `IsDebuggingEnabled=true`; the remaining ~1.4 GB/run is the PipelineDebugger's SignalR snapshot transmission (gated), not the data path — production should run debugging off.
- The granular TDD implementation plan that accompanied this spec was removed after completion (execution artifact; superseded by the code + this section).

## 12. Divergence notes (2026-05-30)

Point-in-time record: the items below changed *after* this spec (and after the §11 as-built snapshot) was written. Original text above is preserved.

- **`IReadSource.Evaluate` shape (supersedes §11).** No longer `IEnumerable<(string CanonicalPath, string RawJson)>` — it now returns `IEnumerable<DetachedMatch>`, each carrying an owned `JsonElement` (`JsonElement.Clone`, element fast path) or an orphan `JsonNode` (`JsonNode.DeepClone`, node/lifted path) plus its canonical path, never a UTF-16 JSON string (`IReadSource.cs`, `DetachedMatch.cs`, `JsonDetach.cs`). `ElementSource.Evaluate` walks the lifted overlay node directly via `NodeView` instead of snapshotting the whole document with `ToJsonString` (that whole-document serialize was the single biggest read-heavy-pipeline allocator). The per-match serialize→reparse round-trip the `(string RawJson)` shape implied is gone.
- **`JsonNodePath` (refines §11).** §11 says it was "trimmed to its write/normalize API." Public `Select`/`SelectAll` read wrappers were later re-added (thin delegations to `JsonPathWalker.Select` over `NodeView`) for adapter call sites that hold a raw `JsonNode` — so it keeps the write/normalize helpers **plus** those read wrappers.
- **Memory gates (refines §6).** Beyond `GetAllocatedBytesForCurrentThread()`, the gates are isolated in a `[CollectionDefinition("AllocationGates", DisableParallelization = true)]` collection (`AllocationGatesCollection.cs`, commit `aa7cec3`) and their relative ratios were replaced with frozen absolute ceilings (process-wide GC counters flaked under xUnit parallel execution). The Newtonsoft-oracle net also grew: `OperationParityTests` + `OverlayWriteThenReadParityTests` + `EncodingParityTests` (commits `8e05997`, `1338934`).
- **Subtree-scoped element-direct reads after an unrelated write — CONSIDERED and DROPPED.** Serving such reads from the base after a write to an unrelated path was measured a no-op on .NET 10: post-lift `JsonNode.Deserialize<T>` allocates identically to element-direct (GC ratio ~1.00 for `int[]` and string-heavy `record[]`); the residual is the inherent typed-result materialization, not a node-vs-element overhead. No production seam change was made. (The shipped zero-copy typed `Get<T>`/`GetArray<T>` off the base — commit `d232236` — applies only while the root overlay is unmutated, via `IReadSource.TryGetElement`.)
- **`WriteJsonTo` encoder fixed (commit `1338934`).** Now routes its `Utf8JsonWriter` through `SystemTextJsonOptions.Default.Encoder` (`UnsafeRelaxedJsonEscaping`). The previous bare `Utf8JsonWriter` used STJ's default encoder, which `\uXXXX`-escapes non-ASCII (umlauts/ß) and HTML (`< > &`), diverging from Newtonsoft byte-parity and breaking hash/HMAC over serialized output (e.g. mesh-adapter `CheckDuplicateNode` / `ApplyDataPointMappingsNode`).
