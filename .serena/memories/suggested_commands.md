# Suggested Commands

## CRITICAL: Always Use DebugL Configuration

⚠️ **IMPORTANT**: For local development, ALWAYS use the `DebugL` configuration to avoid NuGet version conflicts.

## Build Commands

### Build Entire Solution

```bash
# Local development build (REQUIRED for local work)
dotnet build Octo.Sdk.sln --configuration DebugL

# Release build
dotnet build Octo.Sdk.sln --configuration Release
```

### Build Specific Project

```bash
# Build a specific project (local development)
dotnet build src/Sdk.ServiceClient/Sdk.ServiceClient.csproj --configuration DebugL

# Build a specific project (release)
dotnet build src/Sdk.ServiceClient/Sdk.ServiceClient.csproj --configuration Release
```

### Clean Solution

```bash
dotnet clean Octo.Sdk.sln --configuration DebugL
```

## Test Commands

### Run All Tests

```bash
# Run all tests in the solution
dotnet test Octo.Sdk.sln --configuration DebugL

# Run with verbose output
dotnet test Octo.Sdk.sln --configuration DebugL --verbosity normal
```

### Run Tests for Specific Project

```bash
# Unit tests
dotnet test tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj --configuration DebugL

# System/integration tests
dotnet test tests/Sdk.ServiceClient.SystemTests/Sdk.ServiceClient.SystemTests.csproj --configuration DebugL
```

### Run Single Test

```bash
# Run a specific test by fully qualified name
dotnet test --filter "FullyQualifiedName~Sdk.Common.Tests.ClassName.TestMethodName" --configuration DebugL

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~Sdk.Common.Tests.ClassName" --configuration DebugL

# Run tests containing a name pattern
dotnet test --filter "Name~PatternToMatch" --configuration DebugL
```

### Test with Code Coverage

```bash
# Run tests and collect coverage
dotnet test Octo.Sdk.sln --configuration DebugL --collect:"XPlat Code Coverage"
```

## Package Management

### Pack Packages Locally

```bash
# Pack all projects (uses version 999.0.0)
dotnet pack Octo.Sdk.sln --configuration DebugL

# Pack to specific output directory
dotnet pack Octo.Sdk.sln --configuration DebugL --output ../nuget
```

### Pack for Release

```bash
# Pack for release (uses 0.1.* or 3.2.* version)
dotnet pack Octo.Sdk.sln --configuration Release

# Pack to specific output directory
dotnet pack Octo.Sdk.sln --configuration Release --output ./artifacts
```

### Restore Packages

```bash
# Restore NuGet packages
dotnet restore Octo.Sdk.sln

# Restore with specific configuration
dotnet restore Octo.Sdk.sln --configuration DebugL
```

## Windows System Commands

Since this project is developed on Windows, here are common system commands:

### Directory Navigation

```powershell
# List directory contents
dir
ls  # if using PowerShell or Git Bash

# Change directory
cd src\Sdk.ServiceClient

# Go to parent directory
cd ..

# Show current directory
pwd
```

### File Operations

```powershell
# View file contents
type CLAUDE.md
cat CLAUDE.md  # if using Git Bash

# Search for text in files
findstr /s /i "SearchTerm" *.cs

# Find files by pattern
dir /s /b *.csproj
```

### Git Commands

```bash
# Check status
git status

# View recent commits
git log --oneline -10

# Create branch
git checkout -b feature/new-feature

# Stage and commit changes
git add .
git commit -m "AB#2703: Description of changes"

# Push changes
git push origin branch-name

# View diff
git diff
git diff --staged
```

## Project-Specific Commands

### Examine Solution Structure

```bash
# List all projects in solution
dotnet sln Octo.Sdk.sln list

# View project dependencies
dotnet list src/Sdk.ServiceClient/Sdk.ServiceClient.csproj package
```

### Add Package Reference

```bash
# Add NuGet package to a project
dotnet add src/Sdk.ServiceClient/Sdk.ServiceClient.csproj package PackageName
```

### Add Project Reference

```bash
# Add project reference
dotnet add src/Sdk.ServiceClient/Sdk.ServiceClient.csproj reference src/Sdk.Common/Sdk.Common.csproj
```

## Typical Development Workflow

```bash
# 1. Build the solution (local development)
dotnet build Octo.Sdk.sln --configuration DebugL

# 2. Run tests to ensure everything works
dotnet test Octo.Sdk.sln --configuration DebugL

# 3. Make code changes...

# 4. Build again
dotnet build Octo.Sdk.sln --configuration DebugL

# 5. Run tests
dotnet test Octo.Sdk.sln --configuration DebugL

# 6. Pack packages if needed
dotnet pack Octo.Sdk.sln --configuration DebugL --output ../nuget
```

## Troubleshooting Commands

### Clear NuGet Cache

```bash
# Clear all caches
dotnet nuget locals all --clear

# Clear specific cache
dotnet nuget locals global-packages --clear
```

### Build with Detailed Output

```bash
# Verbose build output
dotnet build Octo.Sdk.sln --configuration DebugL --verbosity detailed

# Diagnostic output for troubleshooting
dotnet build Octo.Sdk.sln --configuration DebugL --verbosity diagnostic
```

### Check .NET Version

```bash
# Show installed .NET SDKs
dotnet --list-sdks

# Show installed runtimes
dotnet --list-runtimes

# Show .NET version
dotnet --version
```

## IDE Integration

### Visual Studio

- Open `Octo.Sdk.sln` in Visual Studio
- Set build configuration to `DebugL` in the toolbar
- Use Test Explorer to run tests
- Use NuGet Package Manager to manage dependencies

### JetBrains Rider

- Open `Octo.Sdk.sln` in Rider
- Set build configuration to `DebugL` 
- Use Unit Tests window to run tests
- Use NuGet tool window to manage packages
