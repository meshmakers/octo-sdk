# Code Style and Conventions

## Language Settings

### C# Language Features

- **Language Version**: `latestmajor` - Always use the latest major C# version
- **Nullable Reference Types**: Enabled - All projects enforce nullable annotations
- **Implicit Usings**: Enabled - Common namespaces imported automatically
- **Treat Warnings as Errors**: Enabled - All warnings must be resolved

## Naming Conventions

### Project Naming

Projects follow the pattern: `Meshmakers.Octo.Sdk.[Component]`

Examples:
- `Meshmakers.Octo.Sdk.ServiceClient`
- `Meshmakers.Octo.Sdk.Common`
- `Meshmakers.Octo.Sdk.Common.Web`

### Namespace Structure

- **RootNamespace**: Matches project name (e.g., `Meshmakers.Octo.Sdk.ServiceClient`)
- **Test Projects**: Simplified namespace (e.g., `Sdk.Common.Tests`)

### Custom Dictionary Terms

The following terms are recognized in ReSharper/Rider (from .DotSettings):
- Entra
- Meshmakers / meshmakers
- Octo / octo
- scaler

## Code Documentation

- **XML Documentation**: Enabled via `GenerateDocumentationFile` for all packable projects
- Public APIs should have XML documentation comments
- Use `<summary>`, `<param>`, `<returns>`, etc. appropriately

## Project Configuration Standards

### Common Properties

All projects include:
```xml
<PropertyGroup>
    <LangVersion>latestmajor</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ImplicitUsings>true</ImplicitUsings>
</PropertyGroup>
```

### Packable Projects

Core SDK libraries include:
```xml
<PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

### Test Projects

Test projects include:
```xml
<PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
</PropertyGroup>
```

## Configuration Management

### Build Configurations

Always specify one of three configurations:
1. **Debug**: Standard debugging
2. **DebugL**: Local development (REQUIRED for local work)
3. **Release**: Production builds

### Configuration-Specific Code

Use preprocessor directives when needed:
```csharp
#if DEBUG
    // Debug-specific code
#endif
```

## Testing Conventions

### Test Framework: xUnit

- Test classes should be public
- Test methods marked with `[Fact]` or `[Theory]`
- Use `[InlineData]` for parameterized tests

### Mocking

- **FakeItEasy**: Preferred mocking library
- Create fakes using: `A.Fake<IInterface>()`
- Configure behavior with: `A.CallTo(() => fake.Method()).Returns(value)`

### Test Data Generation

- **Bogus**: Use for generating realistic test data
- Create Faker instances for domain objects

### Test Organization

- Unit tests in `tests/[Project].Tests/`
- Integration/system tests in `tests/[Project].SystemTests/`
- Mirror source structure in test projects

## Dependency Injection

- Use constructor injection
- Prefer interfaces over concrete types
- Register services appropriately for the DI container

## Async/Await Patterns

- Use async/await for I/O operations
- Suffix async methods with `Async`
- Return `Task` or `Task<T>` from async methods
- Avoid `async void` except for event handlers

## Package References

### Version Management

- Don't hardcode Octo package versions
- Use `$(OctoVersion)` variable for internal packages
- Keep third-party packages up to date via Dependabot

### Multi-targeting

When targeting multiple frameworks:
```xml
<TargetFrameworks>net9.0;netstandard2.0</TargetFrameworks>
```

Use conditional compilation for framework-specific code:
```csharp
#if NET9_0
    // .NET 9.0 specific code
#elif NETSTANDARD2_0
    // .NET Standard 2.0 specific code
#endif
```

## Error Handling

- Use nullable reference types to prevent null reference exceptions
- Throw appropriate exception types
- Provide meaningful exception messages

## Warning Suppressions

Only suppress warnings when absolutely necessary:
```xml
<NoWarn>1701;1702;CS8002</NoWarn>
```

Common suppressions:
- CS8002: Referenced assembly targets different framework

## Git Practices

- Follow conventional commit messages
- Keep commits focused and atomic
- Reference work item IDs when applicable (e.g., AB#2703)
