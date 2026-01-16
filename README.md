# Octo SDK

The **Octo SDK** is a .NET framework for building distributed mesh services with real-time communication, ETL data pipelines, and adaptive services. It provides client libraries for REST/GraphQL communication, an adapter framework for building data transformation services, and source generators for strongly-typed DTOs.

## Table of Contents

- [Getting Started](#getting-started)
- [Architecture Overview](#architecture-overview)
- [Assembly Reference](#assembly-reference)
- [Building Custom Adapters](#building-custom-adapters)
- [ETL Pipeline System](#etl-pipeline-system)
- [Source Generation](#source-generation)
- [Build and Test](#build-and-test)
- [Configuration](#configuration)
- [License](#license)

## Getting Started

### Prerequisites

- .NET 10.0 SDK (or .NET Standard 2.0 compatible runtime)
- Access to Octo Mesh services

### Installation

The SDK packages are available on NuGet:

```bash
dotnet add package Meshmakers.Octo.Sdk.ServiceClient
dotnet add package Meshmakers.Octo.Sdk.Common
dotnet add package Meshmakers.Octo.Communication.Contracts
```

### Quick Start: Creating an Adapter

```csharp
using Meshmakers.Octo.Sdk.Common.Adapters;
using Microsoft.Extensions.DependencyInjection;

var builder = new AdapterBuilder();

builder.Run(args, (_, services) =>
{
    services.AddSingleton<IAdapterService, MyAdapterService>();
    services.AddDataPipeline();
});
```

```csharp
public class MyAdapterService : IAdapterService
{
    public Task<bool> StartupAsync(AdapterStartup adapterStartup,
        List<DeploymentUpdateErrorMessageDto> errorMessages,
        CancellationToken stoppingToken)
    {
        // Initialize your adapter
        return Task.FromResult(true);
    }

    public Task ShutdownAsync(AdapterShutdown adapterShutdown,
        CancellationToken stoppingToken)
    {
        // Cleanup resources
        return Task.CompletedTask;
    }
}
```

## Architecture Overview

The Octo SDK follows a layered architecture designed for building distributed adapters that communicate with Octo Mesh services:

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Your Adapter                                │
├─────────────────────────────────────────────────────────────────────┤
│  IAdapterService Implementation                                     │
│         │                                                           │
│         ▼                                                           │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              Sdk.Common (Adapter Framework)                 │    │
│  │  ┌─────────────────┐  ┌──────────────────────────────────┐  │    │
│  │  │ AdapterBuilder  │  │     EtlDataOrchestrator          │  │    │
│  │  │ (Startup/DI)    │  │  (Pipeline Execution Engine)     │  │    │
│  │  └─────────────────┘  └──────────────────────────────────┘  │    │
│  │  ┌─────────────────┐  ┌──────────────────────────────────┐  │    │
│  │  │ PipelineRegistry│  │      IPipelineNode Chain         │  │    │
│  │  │    Service      │  │   (Transform/Filter/Output)      │  │    │
│  │  └─────────────────┘  └──────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────┘    │
│         │                                                           │
│         ▼                                                           │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │           Sdk.ServiceClient (Communication Layer)           │    │
│  │  ┌─────────────────┐  ┌──────────────────────────────────┐  │    │
│  │  │  ServiceClient  │  │      SignalRClient<T>            │  │    │
│  │  │  (REST/GraphQL) │  │  (Real-time Communication)       │  │    │
│  │  └─────────────────┘  └──────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────┘    │
│         │                                                           │
│         ▼                                                           │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │       Communication.Contracts (Shared DTOs & Interfaces)    │    │
│  │  Hub Interfaces │ Data Transfer Objects │ Entity Models     │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌───────────────────┐
                    │  Octo Mesh Cloud  │
                    │    Services       │
                    └───────────────────┘
```

### Communication Patterns

**REST/GraphQL Communication**
- `ServiceClient` base class provides REST communication via RestSharp
- GraphQL queries use `GraphQL.Client` with System.Text.Json serialization
- All domain clients inherit access token management

**Real-time SignalR Communication**
- `SignalRClient<TOptions>` enables bidirectional communication
- Hub interfaces define server-side (`IAdapterHub`) and client-side (`IAdapterHubCallbacks`) contracts
- Adapter lifecycle: Register → Receive Configuration → Process Data → Report Results

## Assembly Reference

### Sdk.ServiceClient

**NuGet Package:** `Meshmakers.Octo.Sdk.ServiceClient`

Client-side proxies for communicating with Octo Mesh services.

| Component                 | Description                                                    |
|---------------------------|----------------------------------------------------------------|
| `ServiceClient`           | Base REST client with authentication and response handling     |
| `SignalRClient<TOptions>` | Real-time bidirectional communication via ASP.NET Core SignalR |
| `IdentityServicesClient`  | Authentication and identity management                         |
| `TenantClient`            | Multi-tenant data operations via GraphQL                       |
| `AdapterHubClient`        | Adapter registration and configuration management              |
| `BotServicesClient`       | Bot-related operations                                         |
| `ReportingServicesClient` | Report generation                                              |
| `AuthenticatorClient`     | OAuth2/OIDC authentication flows                               |

**Key Interfaces:**
- `IServiceClient` - Base interface for all service clients
- `IServiceClientAccessToken` - Access token provider interface
- `ISignalRClient<TOptions>` - SignalR client contract
- `IAdapterHubClient` - Adapter hub communication

### Sdk.Common

**NuGet Package:** `Meshmakers.Octo.Sdk.Common`

Core adapter framework with ETL pipeline orchestration.

| Component                 | Description                                           |
|---------------------------|-------------------------------------------------------|
| `AdapterBuilder`          | Bootstraps adapter applications with DI configuration |
| `IAdapterService`         | Main interface to implement for custom adapters       |
| `EtlDataOrchestrator`     | Executes data transformation pipelines                |
| `IPipelineNode`           | Interface for pipeline transformation nodes           |
| `IDataContext`            | Mutable JSON-based data context (uses JToken)         |
| `PipelineRegistryService` | Manages pipeline instances per tenant                 |

**Key Subsystems:**
- **Adapters:** Lifecycle management for adapter services
- **ETL Pipeline:** Configurable data transformation with node composition
- **Configuration:** YAML/JSON pipeline configuration with type discovery
- **Buffering:** Edge data buffering with LiteDB persistence

### Communication.Contracts

**NuGet Package:** `Meshmakers.Octo.Communication.Contracts`

Shared data transfer objects and hub interface contracts.

| Component                        | Description                                    |
|----------------------------------|------------------------------------------------|
| `IAdapterHub`                    | Server-side SignalR hub interface for adapters |
| `IAdapterHubCallbacks`           | Client-side callback interface for adapters    |
| `IPoolHub` / `IPoolHubCallbacks` | Worker pool hub interfaces                     |
| `GraphQlDto`                     | Base class for GraphQL-compatible DTOs         |
| `RtEntityDto`                    | Runtime entity data transfer object            |
| `RtEntityId`                     | Strongly-typed entity identifier               |

**DTO Categories:**
- Entity models (associations, aggregations)
- Query/Mutation DTOs
- Pipeline data messages (`PipelineData`, `PipelineDataSent`, `PipelineDataReceived`)
- Deployment and configuration DTOs

### Sdk.Common.Web

**NuGet Package:** `Meshmakers.Octo.Sdk.Common.Web`

ASP.NET Core integration for building web-based adapters (sockets and plugs).

Use this package when your adapter needs to expose HTTP endpoints or integrate with ASP.NET Core middleware.

### Sdk.CommunicationAdapter

Internal adapter hosting infrastructure built on .NET Generic Host.

| Feature              | Description                                                   |
|----------------------|---------------------------------------------------------------|
| Dependency Injection | Full Microsoft.Extensions.DependencyInjection support         |
| Event Hub            | Integration with `DistributionEventHub` for pub/sub messaging |
| NLog Integration     | Structured logging configuration                              |
| Configuration        | Environment variables with `OCTO_` prefix                     |

### Sdk.SourceGeneration

**NuGet Package:** `Meshmakers.Octo.Sdk.SourceGeneration`

C# incremental source generator for Construction Kit models.

**Input Files:**
- `ck-{ModelName}.yaml` - Construction Kit model definition
- `ck-{ModelName}.cache.json` - Compiled model metadata

**Generated Output:**
```
DataTransferObjects/{ModelId}/v{Version}/
├── Rt{TypeName}QueryDto.g.cs      # Query DTOs for GraphQL reads
├── Rt{TypeName}MutationDto.g.cs   # Mutation DTOs for writes
├── Rt{RecordName}QueryDto.g.cs    # Record query DTOs
├── Rt{RecordName}MutationDto.g.cs # Record mutation DTOs
└── {EnumName}.g.cs                # Enum classes
```

**Features:**
- Type inheritance support (`DerivedFromCkTypeId`)
- Nullable semantics for GraphQL compatibility
- XML documentation from Construction Kit descriptions
- Association and navigation property generation

### Sdk.SimulationNodes

**NuGet Package:** `Meshmakers.Octo.Sdk.SimulationNodes`

Pipeline nodes for generating synthetic/simulated data using the Bogus library.

| Component           | Description                                                     |
|---------------------|-----------------------------------------------------------------|
| `SimulationNode`    | Pipeline node that generates values using configured simulators |
| `SimulationService` | Factory for creating locale-specific simulators                 |
| `IValueGenerator`   | Interface for custom value generators                           |
| `MathGenerators`    | Numeric value generation                                        |
| `TextGenerators`    | Text value generation                                           |

### Sdk.Plug.Simulation

Executable adapter for data simulation. Combines `Sdk.Common`, `Sdk.CommunicationAdapter`, and `Sdk.SimulationNodes` into a deployable service.

## Building Custom Adapters

### Implementing IAdapterService

Every adapter must implement the `IAdapterService` interface:

```csharp
public interface IAdapterService
{
    Task<bool> StartupAsync(
        AdapterStartup adapterStartup,
        List<DeploymentUpdateErrorMessageDto> errorMessages,
        CancellationToken stoppingToken);

    Task ShutdownAsync(
        AdapterShutdown adapterShutdown,
        CancellationToken stoppingToken);
}
```

### Adapter Lifecycle

1. **Registration** - Adapter connects to hub and registers itself
2. **Configuration** - Receives deployment configuration from the controller
3. **Startup** - `StartupAsync` is called with configuration
4. **Operation** - Process data, execute pipelines, communicate results
5. **Update** - Pre-update notifications enable graceful configuration changes
6. **Shutdown** - `ShutdownAsync` is called for cleanup

### Using the AdapterBuilder

```csharp
var builder = new AdapterBuilder();

builder.Run(args, (hostBuilder, services) =>
{
    // Register your adapter service
    services.AddSingleton<IAdapterService, MyAdapterService>();

    // Add ETL pipeline support
    services.AddDataPipeline();

    // Register custom pipeline nodes
    services.AddTransient<IPipelineNode, MyCustomNode>();

    // Add additional services
    services.AddSingleton<IMyBusinessService, MyBusinessService>();
});
```

## ETL Pipeline System

The SDK includes a powerful ETL (Extract-Transform-Load) pipeline system.

### Pipeline Architecture

```
Trigger → Node₁ → Node₂ → ... → Nodeₙ → Output
            ↓       ↓             ↓
         Context flows through the pipeline
```

### Implementing Pipeline Nodes

```csharp
[NodeName("my-transform")]
public class MyTransformNode : IPipelineNode
{
    public async Task ExecuteAsync(
        INodeContext nodeContext,
        Func<INodeContext, Task> next,
        CancellationToken cancellationToken)
    {
        // Access the data context
        var data = nodeContext.DataContext;

        // Transform data
        data["result"] = TransformData(data["input"]);

        // Continue to next node
        await next(nodeContext);
    }
}
```

### Pipeline Configuration

Pipelines are configured via YAML or JSON:

```yaml
nodes:
  - nodeName: http-trigger
    configuration:
      path: /api/data
  - nodeName: my-transform
    configuration:
      mappings:
        input: "$.source.value"
        output: "$.target.result"
  - nodeName: graphql-output
    configuration:
      mutation: CreateEntity
```

### Data Context

The `IDataContext` provides a mutable JSON-based context using `JToken`:

```csharp
// Read values
var value = context.DataContext["path"]["to"]["value"];

// Write values
context.DataContext["output"] = JToken.FromObject(myResult);

// Access trigger information
var triggerData = context.TriggerContext;
```

## Source Generation

### Setup

1. Add the source generator package:
```xml
<PackageReference Include="Meshmakers.Octo.Sdk.SourceGeneration" Version="x.x.x" />
```

2. Add Construction Kit model files to your project:
```xml
<AdditionalFiles Include="ck-MyModel.yaml" />
<AdditionalFiles Include="ck-MyModel.cache.json" />
```

3. Use generated DTOs:
```csharp
using MyProject.DataTransferObjects.MyModel.v1;

var query = new GraphQLRequest
{
    Query = "query { rtEntities { items { rtId rtWellKnownName } } }",
};

var result = await client.SendQueryAsync<RtMyEntityQueryDto>(query);
```

### Generated DTO Structure

Query DTOs are read-only and designed for GraphQL queries:
```csharp
public class RtMyEntityQueryDto : GraphQlDto
{
    public string? RtId { get; set; }
    public string? RtWellKnownName { get; set; }
    public string? MyProperty { get; set; }
}
```

Mutation DTOs include validation and are used for create/update operations:
```csharp
public class RtMyEntityMutationDto : GraphQlDto
{
    public string? RtId { get; set; }
    public string? RtWellKnownName { get; set; }
    public string? MyProperty { get; set; }
}
```

## Build and Test

### Building the Solution

```bash
# Build all projects
dotnet build Octo.Sdk.sln

# Build in Release mode
dotnet build Octo.Sdk.sln -c Release

# Build with local NuGet packages (from ../nuget directory)
dotnet build Octo.Sdk.sln -c DebugL
```

### Running Tests

```bash
# Run all tests
dotnet test Octo.Sdk.sln

# Run unit tests only
dotnet test tests/Sdk.Common.Tests/Sdk.Common.Tests.csproj

# Run a specific test
dotnet test --filter "FullyQualifiedName~MyTestClass.MyTestMethod"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Build Configurations

| Configuration | Description                                                                 |
|---------------|-----------------------------------------------------------------------------|
| `Debug`       | Standard debug build                                                        |
| `Release`     | Optimized release build                                                     |
| `DebugL`      | Local development with version `999.0.0`, uses `../nuget` as package source |

## Configuration

### Environment Variables

Adapters use environment variables with the `OCTO_` prefix:

| Variable                    | Description                       |
|-----------------------------|-----------------------------------|
| `OCTO_Adapter__TenantId`    | Tenant identifier                 |
| `OCTO_Adapter__AdapterRtId` | Adapter runtime identifier        |
| `OCTO_EdgeDataBuffer__*`    | Edge data buffering configuration |

### AppSettings Structure

```json
{
  "Adapter": {
    "TenantId": "my-tenant",
    "AdapterRtId": "adapter-001"
  },
  "EdgeDataBuffer": {
    "MaxBufferSize": 10000,
    "FlushInterval": "00:00:30"
  }
}
```

### NLog Configuration

Adapters use NLog for logging. Configure via `nlog.config`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog>
  <targets>
    <target name="console" xsi:type="Console" />
  </targets>
  <rules>
    <logger name="*" minlevel="Info" writeTo="console" />
  </rules>
</nlog>
```

## Samples

The `samples/` directory contains example projects:

| Sample                     | Description                               |
|----------------------------|-------------------------------------------|
| `Sdk.Plugs.Sample`         | Basic adapter implementation example      |
| `Sdk.Socket.WebSample`     | Web-based adapter with ASP.NET Core       |
| `Sdk.GraphQlCodeGenSample` | GraphQL client with source-generated DTOs |

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Repository:** https://github.com/meshmakers/octo-sdk

**Website:** https://www.meshmakers.io
