# Octo SDK - Project Overview

## Purpose

The Octo SDK is a .NET 9.0 library providing client-side proxies and common utilities for communicating with OctoMesh services. It serves as the primary SDK for developers integrating with the OctoMesh data mesh platform.

## Key Capabilities

- **Service Communication**: Client-side proxies for OctoMesh service APIs supporting GraphQL, SignalR, and REST
- **ETL Pipeline Framework**: Flexible Extract-Transform-Load pipeline infrastructure for data processing
- **Source Generation**: Roslyn-based source generators for compile-time DTO generation
- **Pluggable Extensions**: Plug system for extending functionality (simulation, communication adapters, etc.)
- **Multi-targeting**: Supports .NET 9.0 and .NET Standard 2.0 for broad compatibility

## Repository Structure

```
octo-sdk/
├── src/                          # Source code
│   ├── Sdk.ServiceClient/        # Service client proxies (GraphQL, SignalR, REST)
│   ├── Sdk.Common/               # Common utilities and ETL pipeline framework
│   ├── Sdk.Common.Web/           # Web-specific utilities
│   ├── Communication.Contracts/  # DTOs and contracts
│   ├── Sdk.SourceGeneration/     # Roslyn source generators
│   ├── Sdk.Plug.Simulation/      # Simulation plug
│   ├── Sdk.CommunicationAdapter/ # Communication adapter plug
│   └── Sdk.SimulationNodes/      # ETL simulation nodes
├── tests/                        # Test projects
│   ├── Sdk.Common.Tests/         # Unit tests
│   └── Sdk.ServiceClient.SystemTests/ # Integration tests
├── samples/                      # Sample applications
│   ├── Sdk.Plugs.Sample/         # Example plug implementation
│   ├── Sdk.Socket.WebSample/     # WebSocket sample
│   └── Sdk.GraphQlCodeGenSample/ # GraphQL code generation sample
├── assets/                       # Package assets (icons, etc.)
└── devops-build/                 # Build scripts and configuration
```

## Main Components

### Core Libraries (NuGet Packages)

- **Sdk.ServiceClient**: Client proxies for bot services, GraphQL, SignalR, authentication
- **Sdk.Common**: Shared utilities, ETL pipeline infrastructure, extensions
- **Sdk.Common.Web**: Web-specific helpers
- **Communication.Contracts**: Data transfer objects
- **Sdk.SourceGeneration**: Compile-time source generators

### Plugs (Extension System)

- **Sdk.Plug.Simulation**: Testing and development simulation plug
- **Sdk.CommunicationAdapter**: Communication adapter using Bogus data generation
- **Sdk.SimulationNodes**: ETL pipeline nodes for simulation scenarios

## Related Repositories

This SDK is part of the larger OctoMesh ecosystem. The parent repository at `C:\dev\meshmakers\` contains the full microservices platform including identity services, report services, bot services, frontend applications, and more.
