# Tech Stack and Architecture

## Target Framework

- **Primary Framework**: .NET 9.0 (net9.0)
- **Multi-targeting**: Some projects also target .NET Standard 2.0 (netstandard2.0) for broader compatibility
- **Language**: C# with `latestmajor` language version
- **Build Tool**: .NET CLI (dotnet) and MSBuild

## Key Technologies

### Service Communication

- **GraphQL**: GraphQL.Client 6.1.0 with SystemTextJson serialization
- **SignalR**: Microsoft.AspNetCore.SignalR.Client 9.0.9 for real-time communication
- **REST API**: RestSharp 112.1.0 for HTTP requests
- **OAuth2/OIDC**: IdentityModel 7.0.0 for authentication

### Testing

- **Test Framework**: xUnit 2.9.3
- **Mocking**: FakeItEasy 8.3.0
- **Test Data**: Bogus 35.6.3 for generating realistic test data
- **Code Coverage**: Coverlet.collector 6.0.4
- **Test SDK**: Microsoft.NET.Test.Sdk 17.14.1

### Source Generation

- **Roslyn**: Source generators and analyzers for compile-time code generation
- **Dependency**: Meshmakers.Octo.ConstructionKit.Engine for generation logic

### Data Storage

- **LiteDB**: Embedded NoSQL database for scenarios requiring local persistence

### Other Key Dependencies

- **Microsoft.Extensions.Caching.Memory**: Memory caching support
- **Microsoft.Extensions.Logging.Abstractions**: Logging infrastructure

## Build Configurations

### Debug
- Standard debug build with full symbols
- Debug constants: DEBUG;TRACE
- No optimization

### DebugL (Local Development)
- **Critical**: Always use this for local development
- Version: 999.0.0 (all packages)
- NuGet source: `../nuget` (local feed)
- Debug symbols: full
- No optimization
- Prevents version conflicts with dependencies

### Release
- Production build
- Optimizations enabled
- Package generation enabled
- Version: 0.1.* (private server) or 3.2.* (public)

## Package Management

### NuGet Configuration

Package sources are determined by configuration:
- **Local Development (DebugL)**: `../nuget` folder + nuget.org
- **Private Server**: Custom NuGet server at configured `OctoNugetPrivateServer`
- **Public**: nuget.org (https://api.nuget.org/v3/index.json)

### Versioning Strategy

- **Local (DebugL)**: 999.0.0
- **Private**: 0.1.* (when OctoNugetPrivateServer is set)
- **Public**: 3.2.* (default)

Versions are controlled via the `$(OctoVersion)` MSBuild variable.

## Architectural Patterns

### ETL Data Pipeline

Located in `Sdk.Common/EtlDataPipeline`, provides:
- **Node Categories**: Extracts, Transforms, Loads, Triggers, Control, Buffering
- **Orchestration**: IEtlDataOrchestrator for pipeline execution
- **Context**: IDataContext and ITriggerContext for execution state
- **Configuration**: Declarative pipeline configuration
- **Debugging**: Built-in pipeline debugger infrastructure

### Service Client Architecture

`Sdk.ServiceClient` provides typed proxies for:
- **BotServices**: Job management, tenant operations, tenant comparison
- **GraphQL**: Type-safe queries and mutations
- **SignalR**: Real-time messaging and notifications
- **Authentication**: OAuth2/OIDC flows

### Plug System

Extension mechanism for adding functionality:
- Simulation capabilities
- Custom communication adapters
- Additional ETL nodes
- Domain-specific functionality

### Source Generation

Roslyn-based generators create:
- DTOs at compile time
- Reduces boilerplate
- Improves type safety
- Packaged as analyzer in `analyzers/dotnet/cs`

## Common MSBuild Properties

From `Directory.Build.props`:
- `LangVersion`: latestmajor
- `Nullable`: enabled
- `TreatWarningsAsErrors`: true
- `ImplicitUsings`: true
- `GenerateDocumentationFile`: true (for packable projects)
- `GeneratePackageOnBuild`: true (for core libraries)

## Windows Development

This project is developed on Windows. Key considerations:
- Path separators: backslash (`\`)
- PowerShell or CMD for shell commands
- Visual Studio or Rider as IDEs
- Git Bash available for Unix-style commands
