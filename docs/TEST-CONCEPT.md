# Octo SDK Test Concept

This document defines the testing strategy for the Octo SDK, covering unit tests, integration tests, and system tests.

## Table of Contents

- [Test Philosophy](#test-philosophy)
- [Test Categories](#test-categories)
- [Test Project Structure](#test-project-structure)
- [Unit Tests](#unit-tests)
- [Integration Tests](#integration-tests)
- [System Tests](#system-tests)
- [Test Infrastructure](#test-infrastructure)
- [Naming Conventions](#naming-conventions)
- [Test Data Management](#test-data-management)
- [Mocking Strategy](#mocking-strategy)
- [Coverage Goals](#coverage-goals)
- [CI/CD Integration](#cicd-integration)

---

## Test Philosophy

The Octo SDK testing strategy follows these principles:

1. **Test Pyramid**: More unit tests, fewer integration tests, even fewer system tests
2. **Fast Feedback**: Unit tests must execute quickly (< 100ms per test)
3. **Isolation**: Tests should not depend on each other or external state
4. **Determinism**: Tests must produce consistent results across environments
5. **Readability**: Tests serve as documentation for expected behavior

---

## Test Categories

### Unit Tests

**Purpose**: Test individual components in complete isolation.

| Aspect          | Description                                                |
|-----------------|------------------------------------------------------------|
| Scope           | Single class or method                                     |
| Dependencies    | All external dependencies are mocked                       |
| Execution Time  | < 100ms per test                                           |
| External Access | None (no network, database, file system)                   |
| Test Project    | `Sdk.Common.Tests`, `Sdk.ServiceClient.Tests` (to create)  |

### Integration Tests

**Purpose**: Test interaction between multiple components within the SDK.

| Aspect          | Description                                                       |
|-----------------|-------------------------------------------------------------------|
| Scope           | Multiple classes working together                                 |
| Dependencies    | Real implementations within SDK, mocked external services         |
| Execution Time  | < 1s per test                                                     |
| External Access | None (mock external APIs, use in-memory databases)                |
| Test Project    | `Sdk.Common.IntegrationTests`, `Sdk.ServiceClient.IntegrationTests` |

### System Tests

**Purpose**: Test complete workflows against real Octo Mesh services.

| Aspect          | Description                                             |
|-----------------|---------------------------------------------------------|
| Scope           | End-to-end scenarios                                    |
| Dependencies    | Real Octo Mesh services                                 |
| Execution Time  | Variable (depends on service response)                  |
| External Access | Full access to configured test environment              |
| Test Project    | `Sdk.ServiceClient.SystemTests`                         |

---

## Test Project Structure

```
tests/
├── Sdk.Common.Tests/                        # Unit tests for Sdk.Common
│   ├── Fixtures/                            # Test fixtures and base classes
│   │   ├── ServiceCollectionFixture.cs
│   │   ├── DataPipelineFixture.cs
│   │   └── NodeFixture.cs
│   ├── TestData/                            # Test data generators and DTOs
│   │   ├── Dto/
│   │   │   ├── Customer.cs
│   │   │   ├── Order.cs
│   │   │   └── Generator.cs
│   │   ├── TestNode.cs                      # Test pipeline nodes
│   │   └── TestPipelineConfigurations.cs
│   ├── EtlDataPipeline/                     # ETL pipeline tests
│   │   ├── DataContextTests.cs
│   │   ├── EtlDataOrchestratorTests.cs
│   │   ├── Configuration/
│   │   │   └── Serializer/
│   │   └── Nodes/
│   │       ├── Control/
│   │       ├── Transforms/
│   │       ├── Extracts/
│   │       └── Load/
│   ├── Adapters/                            # Adapter tests (to create)
│   └── Services/                            # Service tests
│       ├── PipelineRegistryServiceTests.cs
│       └── PipelineRegistrationTests.cs
│
├── Sdk.ServiceClient.Tests/                 # Unit tests for Sdk.ServiceClient (to create)
│   ├── Fixtures/
│   ├── Authentication/
│   ├── Clients/
│   └── SignalR/
│
├── Sdk.Common.IntegrationTests/             # Integration tests (to create)
│   ├── Fixtures/
│   ├── EtlDataPipeline/
│   └── Adapters/
│
├── Sdk.ServiceClient.IntegrationTests/      # Integration tests (to create)
│   ├── Fixtures/
│   └── Clients/
│
├── Sdk.ServiceClient.SystemTests/           # System tests (existing)
│   ├── Fixtures/
│   │   ├── ServiceCollectionFixture.cs
│   │   └── Logging.cs
│   ├── Models/
│   └── TenantClientTests.cs
│
└── Sdk.SourceGeneration.Tests/              # Source generator tests (to create)
    ├── GeneratorTests.cs
    └── Snapshots/
```

---

## Unit Tests

### What to Test

| Component               | Test Focus                                                    |
|-------------------------|---------------------------------------------------------------|
| Pipeline Nodes          | Input/output transformation, edge cases, error handling       |
| Data Context            | Path operations, value setting/getting, JSON manipulation     |
| Configuration Serializer| YAML/JSON parsing, node configuration deserialization         |
| Service Clients         | Request building, response parsing, error handling            |
| DTOs                    | Serialization/deserialization, validation                     |
| Adapters                | Lifecycle methods, configuration handling                     |

### Unit Test Pattern for Pipeline Nodes

```csharp
public class MyNodeTests : IClassFixture<NodeFixture>
{
    private readonly NodeFixture _fixture;

    public MyNodeTests(NodeFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProcessObjectAsync_ValidInput_TransformsCorrectly()
    {
        // Arrange
        var configuration = new MyNodeConfiguration
        {
            Path = "$.input",
            TargetPath = "$.output"
        };
        var (dataContext, nodeContext) = PrepareTest(configuration);
        var next = A.Fake<NodeDelegate>();
        var sut = new MyNode(next);

        // Act
        await sut.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => next.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(expectedValue, dataContext.GetValueByPath("$.output"));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullInput_ThrowsArgumentException()
    {
        // Arrange
        var configuration = new MyNodeConfiguration { Path = "$.missing" };
        var (dataContext, nodeContext) = PrepareTest(configuration);
        var next = A.Fake<NodeDelegate>();
        var sut = new MyNode(next);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ProcessObjectAsync(dataContext, nodeContext));
    }

    private (DataContext, INodeContext) PrepareTest(MyNodeConfiguration config)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new { input = "test" })
        };
        var rootContext = NodeContext.CreateRootNodeContext(
            _fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootContext.RegisterChildNode("MyNode", 0, config, dataContext);
        return (dataContext, nodeContext);
    }
}
```

### Unit Test Pattern for Service Clients

```csharp
public class MyServiceClientTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly Mock<IServiceClientAccessToken> _accessToken;

    public MyServiceClientTests()
    {
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _accessToken = new Mock<IServiceClientAccessToken>();
    }

    [Fact]
    public async Task SendQueryAsync_ValidQuery_ReturnsExpectedResult()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.When("*").Respond("application/json",
            "{\"data\":{\"items\":[{\"id\":\"1\"}]}}");

        var client = CreateClient(mockHandler);
        var query = new GraphQLRequest { Query = "{ items { id } }" };

        // Act
        var result = await client.SendQueryAsync<TestDto>(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SendQueryAsync_Unauthorized_ThrowsUnauthorizedException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.When("*").Respond(HttpStatusCode.Unauthorized);

        var client = CreateClient(mockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedServiceAccessException>(
            () => client.SendQueryAsync<TestDto>(new GraphQLRequest()));
    }
}
```

---

## Integration Tests

### What to Test

| Scenario                            | Description                                              |
|-------------------------------------|----------------------------------------------------------|
| Pipeline End-to-End                 | Complete pipeline execution with multiple nodes          |
| Configuration Loading               | YAML/JSON config to executable pipeline                  |
| Adapter Lifecycle                   | Start → Configure → Execute → Shutdown                   |
| Service Client Chain                | Authentication → Request → Response handling             |
| SignalR Hub Communication           | Connect → Subscribe → Receive → Disconnect               |

### Integration Test Pattern

```csharp
public class PipelineIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public PipelineIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecutePipeline_ComplexTransformation_ProcessesCorrectly()
    {
        // Arrange
        var pipelineConfig = @"
nodes:
  - nodeName: extract-data
    configuration:
      source: $.input
  - nodeName: transform
    configuration:
      mappings:
        - source: $.name
          target: $.fullName
  - nodeName: load
    configuration:
      target: $.output
";
        var orchestrator = _fixture.CreateOrchestrator();
        var context = _fixture.CreateContext();
        var inputData = new { input = new { name = "Test" } };

        // Act
        var result = await orchestrator.ExecutePipelineAsync(
            pipelineConfig, context, inputData: inputData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.SelectToken("$.output.fullName")?.Value<string>());
    }
}
```

### Integration Test Fixture

```csharp
public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public IntegrationTestFixture()
    {
        var services = new ServiceCollection();

        // Register real implementations
        services.AddDataPipeline()
            .RegisterAllBuiltInNodes();

        services.AddLogging(builder => builder.AddConsole());

        ServiceProvider = services.BuildServiceProvider();
    }

    public IEtlDataOrchestrator CreateOrchestrator()
    {
        return new EtlDataOrchestrator(
            ServiceProvider,
            ServiceProvider.GetRequiredService<INodeLookupService>());
    }

    public IEtlContext CreateContext()
    {
        return new DefaultEtlContext(
            "integration-test",
            OctoObjectId.GenerateNewId(),
            Guid.NewGuid(),
            new RtEntityId("Test/Pipeline", OctoObjectId.GenerateNewId()),
            DateTime.UtcNow,
            null,
            new GlobalConfiguration(new List<ConfigurationDto>()),
            new Dictionary<string, object?>());
    }

    public void Dispose()
    {
        (ServiceProvider as IDisposable)?.Dispose();
    }
}
```

---

## System Tests

### Prerequisites

System tests require access to a running Octo Mesh environment. Configure via:

- Environment variables
- `appsettings.json` or `secrets.json`
- Test-specific configuration files

### Configuration

```json
{
  "TestEnvironment": {
    "AuthorityUrl": "https://identity.test.octomesh.io",
    "AssetServiceUrl": "https://assets.test.octomesh.io",
    "TenantId": "test-tenant",
    "ClientId": "test-client",
    "ClientSecret": "..."
  }
}
```

### System Test Pattern

```csharp
[Collection("SystemTests")]
public class TenantClientSystemTests : IClassFixture<SystemTestFixture>
{
    private readonly SystemTestFixture _fixture;

    public TenantClientSystemTests(SystemTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "SystemTest")]
    public async Task QueryEntities_ExistingTenant_ReturnsEntities()
    {
        // Arrange
        var client = _fixture.CreateTenantClient();
        var query = new GraphQLRequest
        {
            Query = "query { rtEntities { items { rtId } } }"
        };

        // Act
        var result = await client.SendQueryAsync<RtEntityDto>(query);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    [Trait("Category", "SystemTest")]
    public async Task CreateAndDeleteEntity_FullLifecycle_Succeeds()
    {
        // Arrange
        var client = _fixture.CreateTenantClient();
        var entityToCreate = new RtEntityMutationDto
        {
            RtWellKnownName = $"test-entity-{Guid.NewGuid()}"
        };

        // Act - Create
        var created = await client.CreateEntityAsync(entityToCreate);

        // Assert - Created
        Assert.NotNull(created?.RtId);

        // Act - Delete
        await client.DeleteEntityAsync(created.RtId);

        // Assert - Deleted
        var found = await client.GetEntityByIdAsync(created.RtId);
        Assert.Null(found);
    }
}
```

---

## Test Infrastructure

### Fixtures

| Fixture                  | Purpose                                          |
|--------------------------|--------------------------------------------------|
| `ServiceCollectionFixture` | Base fixture with DI container setup           |
| `NodeFixture`            | Setup for pipeline node tests                    |
| `DataPipelineFixture`    | Full pipeline infrastructure with node registry  |
| `IntegrationTestFixture` | Complete SDK setup for integration tests         |
| `SystemTestFixture`      | External service connections for system tests    |

### Test Utilities

```csharp
// Test data generator using Bogus
public static class Generator
{
    private static readonly Faker Faker = new();

    public static Customer GenerateCustomer()
    {
        return new Customer
        {
            Id = Faker.Random.Guid(),
            Name = Faker.Name.FullName(),
            Email = Faker.Internet.Email(),
            Address = GenerateAddress()
        };
    }

    public static Order GenerateOrder(int itemCount = 3)
    {
        return new Order
        {
            OrderId = Faker.Random.Guid(),
            CustomerId = Faker.Random.Guid(),
            Items = Enumerable.Range(0, itemCount)
                .Select(_ => GenerateOrderItem())
                .ToList(),
            CreatedAt = Faker.Date.Recent()
        };
    }
}
```

### Custom Test Nodes

```csharp
// Test node for asserting pipeline behavior
[NodeName("TestOutput", 1)]
internal record TestOutputNodeConfiguration : TargetPathNodeConfiguration
{
    public int OutputValue { get; set; }
}

[NodeConfiguration(typeof(TestOutputNodeConfiguration))]
internal class TestOutputNode(NodeDelegate next) : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var config = nodeContext.GetNodeConfiguration<TestOutputNodeConfiguration>();
        dataContext.SetValueByPath(config.TargetPath, config.DocumentMode,
            config.TargetValueKind, config.TargetValueWriteMode, config.OutputValue);
        await next(dataContext, nodeContext);
    }
}
```

---

## Naming Conventions

### Test Class Names

```
{ClassUnderTest}Tests
{Feature}IntegrationTests
{Feature}SystemTests
```

Examples:
- `MapNodeTests`
- `DataContextTests`
- `PipelineExecutionIntegrationTests`
- `TenantClientSystemTests`

### Test Method Names

```
{MethodUnderTest}_{Scenario}_{ExpectedResult}
```

Examples:
- `ProcessObjectAsync_ValidInput_TransformsCorrectly`
- `ProcessObjectAsync_NullInput_ThrowsArgumentException`
- `SendQueryAsync_Unauthorized_ThrowsUnauthorizedException`
- `ExecutePipeline_ComplexTransformation_ProcessesCorrectly`

### Test Categories (Traits)

```csharp
[Trait("Category", "Unit")]
[Trait("Category", "Integration")]
[Trait("Category", "SystemTest")]
[Trait("Component", "ETL")]
[Trait("Component", "ServiceClient")]
```

---

## Test Data Management

### Principles

1. **Generate fresh data** for each test using Bogus
2. **Avoid shared mutable state** between tests
3. **Use meaningful test data** that represents real scenarios
4. **Clean up test data** in system tests (try/finally or IAsyncLifetime)

### Test Data Classes

```csharp
namespace Sdk.Common.Tests.TestData.Dto;

public record Customer
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Address? Address { get; init; }
}

public record Order
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public List<OrderItem> Items { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}
```

---

## Mocking Strategy

### Tools

| Tool        | Use Case                                      |
|-------------|-----------------------------------------------|
| FakeItEasy  | General mocking (primary choice)              |
| Moq         | Alternative for complex setups                |
| NSubstitute | Alternative if team prefers                   |

### What to Mock

| Mock                | When                                              |
|---------------------|---------------------------------------------------|
| External APIs       | Always in unit and integration tests              |
| Database            | Unit tests (use in-memory for integration)        |
| File System         | Unit tests (use in-memory for integration)        |
| Time/DateTime       | When determinism required                         |
| Random/Guid         | When determinism required                         |
| Logger              | Unit tests (verify logging calls if needed)       |

### What NOT to Mock

| Component           | Reason                                            |
|---------------------|---------------------------------------------------|
| DTOs                | Simple data containers                            |
| Configuration POCOs | Simple property containers                        |
| Extension methods   | Cannot be mocked, test through behavior           |
| The SUT itself      | Testing behavior, not mocks                       |

### Mocking Examples

```csharp
// FakeItEasy
var logger = A.Fake<IPipelineLogger>();
var nodeDelegate = A.Fake<NodeDelegate>();

A.CallTo(() => nodeDelegate.Invoke(A<IDataContext>._, A<INodeContext>._))
    .Returns(Task.CompletedTask);

// Verify calls
A.CallTo(() => logger.Log(A<LogLevel>._, A<string>._))
    .MustHaveHappened();
```

---

## Coverage Goals

### Target Coverage by Component

| Component            | Line Coverage | Branch Coverage |
|----------------------|---------------|-----------------|
| Pipeline Nodes       | ≥ 90%         | ≥ 85%           |
| Data Context         | ≥ 95%         | ≥ 90%           |
| Configuration        | ≥ 85%         | ≥ 80%           |
| Service Clients      | ≥ 80%         | ≥ 75%           |
| DTOs (generated)     | N/A           | N/A             |

### Coverage Collection

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate report (requires reportgenerator tool)
reportgenerator -reports:**/coverage.cobertura.xml \
    -targetdir:coverage-report \
    -reporttypes:Html
```

---

## CI/CD Integration

### Test Execution Strategy

```yaml
# Example GitHub Actions workflow
test:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Unit Tests
      run: dotnet test --no-build --filter "Category=Unit"

    - name: Run Integration Tests
      run: dotnet test --no-build --filter "Category=Integration"

    # System tests only on specific branches/triggers
    - name: Run System Tests
      if: github.ref == 'refs/heads/main'
      run: dotnet test --no-build --filter "Category=SystemTest"
      env:
        TEST_AUTHORITY_URL: ${{ secrets.TEST_AUTHORITY_URL }}
        TEST_TENANT_ID: ${{ secrets.TEST_TENANT_ID }}
        TEST_CLIENT_ID: ${{ secrets.TEST_CLIENT_ID }}
        TEST_CLIENT_SECRET: ${{ secrets.TEST_CLIENT_SECRET }}
```

### Test Execution Commands

```bash
# Run all tests
dotnet test Octo.Sdk.sln

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run only system tests
dotnet test --filter "Category=SystemTest"

# Run tests for a specific component
dotnet test --filter "Component=ETL"

# Run a specific test class
dotnet test --filter "FullyQualifiedName~MapNodeTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~MapNodeTests.ProcessObjectAsync_Simple_OK"
```

---

## Appendix: Test Project Templates

### Unit Test Project (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bogus" Version="35.6.5" />
        <PackageReference Include="FakeItEasy" Version="9.0.0" />
        <PackageReference Include="MartinCostello.Logging.XUnit.v3" Version="0.7.0" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
        <PackageReference Include="xunit.v3" Version="3.2.2" />
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
        <PackageReference Include="coverlet.collector" Version="6.0.4" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\{ProjectUnderTest}\{ProjectUnderTest}.csproj" />
    </ItemGroup>
</Project>
```

### GlobalUsings.cs

```csharp
global using Xunit;
global using Xunit.Abstractions;
global using FakeItEasy;
global using FluentAssertions;
```
