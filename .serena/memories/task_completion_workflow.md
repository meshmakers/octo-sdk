# Task Completion Workflow

## When a Task is Completed

After making code changes, follow these steps to ensure quality and completeness:

## 1. Build the Solution

**CRITICAL**: Always use the `DebugL` configuration for local development.

```bash
dotnet build Octo.Sdk.sln --configuration DebugL
```

**Expected Result**: Build should succeed with zero errors and zero warnings (TreatWarningsAsErrors is enabled).

**If Build Fails**:
- Fix all compilation errors
- Fix all warnings (they are treated as errors)
- Ensure nullable reference type annotations are correct
- Rebuild until successful

## 2. Run Tests

Run all tests to ensure no regressions:

```bash
dotnet test Octo.Sdk.sln --configuration DebugL
```

**Expected Result**: All tests should pass.

**If Tests Fail**:
- Investigate the failing test(s)
- Fix the issue in the source code or update the test if behavior changed intentionally
- Re-run tests until all pass

### Run Specific Test Subset (Optional)

If you only changed code in a specific component:

```bash
# For Sdk.Common changes
dotnet test tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj --configuration DebugL

# For Sdk.ServiceClient changes
dotnet test tests/Sdk.ServiceClient.SystemTests/Sdk.ServiceClient.SystemTests.csproj --configuration DebugL
```

## 3. Code Quality Checks

### Verify Nullable Reference Types

Ensure:
- No nullable warnings
- Proper use of `?` for nullable types
- Appropriate null checks where needed

### Check XML Documentation

For public APIs:
- All public classes should have `<summary>` documentation
- All public methods should have `<summary>`, `<param>`, and `<returns>` tags
- Documentation should be clear and accurate

### Review Code Style

- Naming conventions followed
- Async methods suffixed with `Async`
- Proper error handling
- No commented-out code left behind

## 4. Git Operations (If Creating a Commit)

### Stage Changes

```bash
# Review what changed
git status
git diff

# Stage specific files
git add path/to/file.cs

# Or stage all changes
git add .
```

### Commit with Meaningful Message

```bash
# Follow the commit message pattern (reference work item if applicable)
git commit -m "AB#XXXX: Brief description of changes"
```

**Commit Message Guidelines**:
- Reference Azure Boards work item ID (AB#XXXX) if applicable
- Use present tense ("Add feature" not "Added feature")
- Be concise but descriptive
- Examples from repo history:
  - "AB#2703 New: Tenants can be restored with other than original database name"
  - "AB#2706: New: Added hash node to create MD4, SHA hashes"

### Push Changes (If Appropriate)

```bash
git push origin branch-name
```

## 5. Package Creation (If Needed)

If you've made changes that need to be consumed by other projects:

```bash
# Pack packages locally
dotnet pack Octo.Sdk.sln --configuration DebugL --output ../nuget
```

This creates NuGet packages with version 999.0.0 in the `../nuget` directory for local consumption.

## 6. Documentation Updates (If Needed)

If you've:
- Added new public APIs
- Changed existing behavior
- Added new components
- Modified build/test procedures

Consider updating:
- `CLAUDE.md` - For development guidance
- XML documentation comments in code
- Sample projects (if relevant)

## Complete Checklist

Before considering a task complete:

- [ ] Code compiles without errors or warnings
- [ ] All tests pass
- [ ] Nullable reference types properly annotated
- [ ] Public APIs have XML documentation
- [ ] Code follows project conventions
- [ ] No unnecessary commented code
- [ ] Changes are committed with meaningful message (if appropriate)
- [ ] Packages are packed locally (if needed by other projects)
- [ ] Documentation updated (if needed)

## Quick Reference - Typical Workflow

```bash
# 1. Build
dotnet build Octo.Sdk.sln --configuration DebugL

# 2. Test
dotnet test Octo.Sdk.sln --configuration DebugL

# 3. Review changes
git status
git diff

# 4. Commit (if appropriate)
git add .
git commit -m "AB#XXXX: Description"

# 5. Pack (if needed)
dotnet pack Octo.Sdk.sln --configuration DebugL --output ../nuget
```

## Handling Failures

### Build Failures

1. Read the error messages carefully
2. Fix compilation errors first, then warnings
3. Check for:
   - Missing using statements
   - Type mismatches
   - Nullable reference type issues
   - Missing dependencies

### Test Failures

1. Identify which test(s) failed
2. Run the specific failing test for faster iteration:
   ```bash
   dotnet test --filter "FullyQualifiedName~TestClass.TestMethod" --configuration DebugL
   ```
3. Debug the test if needed
4. Fix the issue
5. Re-run all tests to ensure no regressions

### NuGet Version Conflicts

If you encounter version conflicts:
1. Ensure you're using `DebugL` configuration
2. Clear NuGet caches: `dotnet nuget locals all --clear`
3. Restore packages: `dotnet restore Octo.Sdk.sln`
4. Rebuild: `dotnet build Octo.Sdk.sln --configuration DebugL`

## Integration with Other OctoMesh Projects

If changes affect other services in the parent OctoMesh repository:
1. Pack the SDK packages locally
2. Navigate to the dependent service
3. Build the dependent service with DebugL configuration
4. Test the integration
5. Ensure all affected services still work correctly
