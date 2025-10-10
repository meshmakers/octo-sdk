# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

The Octo SDK is a .NET 9.0 library providing client-side proxies and common utilities for communicating with OctoMesh services. This repository contains SDK components, plugs, samples, and tests organized as a single Visual Studio solution.

## Development Commands

### Build and Test

```bash
# Build the entire solution (IMPORTANT: Always use DebugL configuration for local development)
dotnet build Octo.Sdk.sln --configuration DebugL

# Build for release
dotnet build Octo.Sdk.sln --configuration Release

# Run all tests
dotnet test Octo.Sdk.sln --configuration DebugL

# Run tests for a specific project
dotnet test tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj --configuration DebugL

# Run a single test by name
dotnet test --filter "FullyQualifiedName~Sdk.Common.Tests.ClassName.TestMethodName" --configuration DebugL
```

### NuGet Package Management

```bash
# Pack packages locally (uses version 999.0.0)
dotnet pack Octo.Sdk.sln --configuration DebugL

# Pack for release
dotnet pack Octo.Sdk.sln --configuration Release
```

## Architecture Overview

### Project Structure

The solution contains the following main components:

**Core Libraries:**
- **Sdk.ServiceClient**: Client-side proxies for OctoMesh service APIs (GraphQL, SignalR, REST)
- **Sdk.Common**: Shared utilities including ETL pipeline infrastructure and extensions
- **Sdk.Common.Web**: Web-specific utilities and helpers
- **Communication.Contracts**: Data transfer objects and contracts for communication services
- **Sdk.SourceGeneration**: Roslyn source generators for DTO generation

**Plugs (Extensions):**
- **Sdk.Plug.Simulation**: Simulation plug for testing and development
- **Sdk.CommunicationAdapter**: Communication adapter plug using Bogus for data generation
- **Sdk.SimulationNodes**: ETL pipeline nodes for simulation scenarios

**Samples:**
- **Sdk.Plugs.Sample**: Example plug implementation
- **Sdk.Socket.WebSample**: WebSocket communication sample
- **Sdk.GraphQlCodeGenSample**: GraphQL code generation example

**Tests:**
- **Sdk.Common.Tests**: Unit tests for common SDK functionality (uses xUnit, FakeItEasy, Bogus)
- **Sdk.ServiceClient.SystemTests**: System/integration tests for service client

### Build Configurations

- **Debug**: Standard debug build with symbols
- **DebugL**: Local development mode with version 999.0.0 and local NuGet feed (`../nuget`)
- **Release**: Production build for package distribution

**IMPORTANT**: Always use `DebugL` configuration for local development to avoid NuGet version conflicts.

### Key Technologies and Dependencies

- **Target Framework**: .NET 9.0 (with netstandard2.0 support for some projects)
- **Language**: C# (latestmajor), nullable reference types enabled, treat warnings as errors
- **Service Communication**:
  - GraphQL (GraphQL.Client)
  - SignalR (Microsoft.AspNetCore.SignalR.Client)
  - REST (RestSharp)
  - OAuth2 (IdentityModel)
- **Testing**: xUnit, FakeItEasy (mocking), Bogus (test data generation), Coverlet (coverage)
- **Source Generation**: Roslyn analyzers and source generators
- **Persistence**: LiteDB for embedded storage scenarios

### NuGet Configuration

The repository uses multiple package sources based on configuration:

- **Private packages** (when `OctoNugetPrivateServer` is set): Custom NuGet server with version `0.1.*`
- **Public packages** (default): nuget.org with version `3.2.*`
- **Local development** (DebugL): `../nuget` folder with version `999.0.0`

Package references use `$(OctoVersion)` variable for version management.

## Key Architectural Patterns

### ETL Data Pipeline

The `Sdk.Common/EtlDataPipeline` namespace contains a flexible ETL (Extract, Transform, Load) pipeline framework:

- **Nodes**: Organized into Extracts, Transforms, Loads, Triggers, Control, and Buffering categories
- **Orchestration**: `IEtlDataOrchestrator` manages pipeline execution
- **Context**: `IDataContext` and `ITriggerContext` provide execution context
- **Configuration**: Pipeline configuration through `Configuration` namespace
- **Debugging**: Pipeline debugger infrastructure in `Debugger` namespace

### Service Client Architecture

`Sdk.ServiceClient` provides typed client proxies for OctoMesh services:

- **BotServices**: Bot automation, job management, tenant backup/restore, tenant comparison
- **GraphQL Integration**: Type-safe GraphQL queries and mutations
- **SignalR Support**: Real-time communication channels
- **Authentication**: OAuth2/OIDC integration through IdentityModel

### Source Generation

`Sdk.SourceGeneration` is a Roslyn-based source generator that creates DTOs at compile time:

- Packaged as an analyzer (targets `analyzers/dotnet/cs`)
- Uses `EnforceExtendedAnalyzerRules` for analyzer development
- Depends on `Meshmakers.Octo.ConstructionKit.Engine` for code generation logic

## Common Development Patterns

1. **Multi-targeting**: Some projects target both `net9.0` and `netstandard2.0` for compatibility
2. **ImplicitUsings**: Enabled globally for reduced boilerplate
3. **Documentation**: XML documentation generation enabled (`GenerateDocumentationFile`)
4. **InternalsVisibleTo**: Used in tests to expose internals (e.g., for mocking with `DynamicProxyGenAssembly2`)
5. **Package Generation**: Core libraries auto-generate NuGet packages on build (`GeneratePackageOnBuild`)
