using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData;
using Sdk.Common.Tests.TestData.Dto;
using Xunit.Abstractions;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class EtlDataOrchestratorTests(DataPipelineFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<DataPipelineFixture>
{
    [Fact]
    public async Task ExecutePipelineAsync_OK()
    {
        fixture.UseXUnitLoggerFactory(testOutputHelper);
        var serviceProvider = fixture.Services.BuildServiceProvider();

        var dataOrchestrator = new EtlDataOrchestrator(serviceProvider,
            serviceProvider.GetRequiredService<INodeLookupService>());

        var r = await dataOrchestrator.ExecutePipelineAsync(TestPipelineConfigurations.Test1,
            new DefaultEtlContext("test1", OctoObjectId.GenerateNewId(), Guid.NewGuid(),
                new RtEntityId("System.Communication/EdgeAdapter", OctoObjectId.GenerateNewId()), DateTime.UtcNow, null,
                new GlobalConfiguration(new List<ConfigurationDto>()), new Dictionary<string, object?>()));

        Assert.NotNull(r);

        // Transformed from InvoiceNumber, but linear scaling was applied
        var jToken = (JToken)r;
        Assert.Equal(3, jToken.Count());
        Assert.Equal(760, jToken.SelectToken("$.InvoiceNumber"));

        // Transformed from Items
        var t = (JArray?)jToken.SelectToken("$.OrderItems");
        Assert.NotNull(t);
        Assert.Equal(3, t.Count());

        // This property was not excluded from source
        Assert.NotNull(jToken.SelectToken("$.InvoiceDate"));
    }

    [Fact]
    public async Task ExecutePipelineAsync_WithDebug_OK()
    {
        fixture.UseXUnitLoggerFactory(testOutputHelper);
        var serviceProvider = fixture.Services.BuildServiceProvider();

        var dataOrchestrator = new EtlDataOrchestrator(serviceProvider,
            serviceProvider.GetRequiredService<INodeLookupService>());
        var debugger = new DefaultPipelineDebugger(serviceProvider.GetRequiredService<ILoggerFactory>());

        var pipelineExecutionId = Guid.NewGuid();
        var pipelineEntityId = new RtEntityId("System.Communication/EdgePipeline", OctoObjectId.GenerateNewId());
        debugger.RegisterPipelineRtEntityId(pipelineEntityId, pipelineExecutionId);

        var r = await dataOrchestrator.ExecutePipelineAsync(TestPipelineConfigurations.Test1,
            new DefaultEtlContext("test1", OctoObjectId.GenerateNewId(), pipelineExecutionId, pipelineEntityId,
                DateTime.UtcNow, null,
                new GlobalConfiguration(new List<ConfigurationDto>()), new Dictionary<string, object?>()), debugger);

        Assert.NotNull(r);

        IPipelineDebugSerializer serializer = serviceProvider.GetRequiredService<IPipelineDebugSerializer>();
        var debugInfo = await serializer.SerializeAsync(debugger.GetDebugInformation());
        Assert.NotNull(debugInfo);
    }

    [Fact]
    public async Task ExecutePipelineAsync_WithDebugWithException_OK()
    {
        fixture.UseXUnitLoggerFactory(testOutputHelper);
        var serviceProvider = fixture.Services.BuildServiceProvider();

        var dataOrchestrator = new EtlDataOrchestrator(serviceProvider,
            serviceProvider.GetRequiredService<INodeLookupService>());
        var debugger = new DefaultPipelineDebugger(serviceProvider.GetRequiredService<ILoggerFactory>());

        var pipelineExecutionId = Guid.NewGuid();
        var pipelineEntityId = new RtEntityId("System.Communication/EdgePipeline", OctoObjectId.GenerateNewId());
        debugger.RegisterPipelineRtEntityId(pipelineEntityId, pipelineExecutionId);

        await Assert.ThrowsAsync<MyCustomException>(async () => await dataOrchestrator.ExecutePipelineAsync(
            TestPipelineConfigurations.Test2,
            new DefaultEtlContext("test1", OctoObjectId.GenerateNewId(), pipelineExecutionId, pipelineEntityId,
                DateTime.UtcNow, null,
                new GlobalConfiguration(new List<ConfigurationDto>()), new Dictionary<string, object?>()), debugger));

        var debugInfo = debugger.GetDebugInformation();
        Assert.NotNull(debugInfo);
        var db = debugInfo.DebugPoints.FirstOrDefault(db => db.NodePath == "PipelineExecution");
        Assert.NotNull(db);
        var message = db.Messages?.FirstOrDefault(m => m.Severity == LoggerSeverity.Error);
        Assert.True(message?.ExceptionMessage?.StartsWith("Test exception"));
    }

    [Fact]
    public async Task ExecutePipelineAsync_WithDebugWithOutput_OK()
    {
        fixture.UseXUnitLoggerFactory(testOutputHelper);
        var serviceProvider = fixture.Services.BuildServiceProvider();

        var dataOrchestrator = new EtlDataOrchestrator(serviceProvider,
            serviceProvider.GetRequiredService<INodeLookupService>());
        var debugger = new DefaultPipelineDebugger(serviceProvider.GetRequiredService<ILoggerFactory>());

        var pipelineExecutionId = Guid.NewGuid();
        var pipelineEntityId = new RtEntityId("System.Communication/EdgePipeline", OctoObjectId.GenerateNewId());
        debugger.RegisterPipelineRtEntityId(pipelineEntityId, pipelineExecutionId);

        var result = await dataOrchestrator.ExecutePipelineAsync(TestPipelineConfigurations.TestDataSingleNode,
            new DefaultEtlContext("test1", OctoObjectId.GenerateNewId(), pipelineExecutionId, pipelineEntityId,
                DateTime.UtcNow, null, new GlobalConfiguration(new List<ConfigurationDto>()),
                new Dictionary<string, object?>()), debugger);

        Assert.Equal("{\"TestOutput\":100}",
            ((JObject?)result)?.ToString(Newtonsoft.Json.Formatting.None));
        var debugInfo = debugger.GetDebugInformation();
        Assert.NotNull(debugInfo);
        var db = debugInfo.DebugPoints.FirstOrDefault(db => db.NodePath == "PipelineExecution/TestOutput@1");
        Assert.NotNull(db);
        Assert.Equal("{\"TestOutput\":100}", db.Output);
    }

    [Fact]
    public async Task ExecutePipelineAsync_WithDebugWithOutputMultiple_OK()
    {
        fixture.UseXUnitLoggerFactory(testOutputHelper);
        var serviceProvider = fixture.Services.BuildServiceProvider();

        var dataOrchestrator = new EtlDataOrchestrator(serviceProvider,
            serviceProvider.GetRequiredService<INodeLookupService>());
        var debugger = new DefaultPipelineDebugger(serviceProvider.GetRequiredService<ILoggerFactory>());

        var pipelineExecutionId = Guid.NewGuid();
        var pipelineEntityId = new RtEntityId("System.Communication/EdgePipeline", OctoObjectId.GenerateNewId());
        debugger.RegisterPipelineRtEntityId(pipelineEntityId, pipelineExecutionId);

        var result = await dataOrchestrator.ExecutePipelineAsync(TestPipelineConfigurations.TestDataMultipleNodes,
            new DefaultEtlContext("test1", OctoObjectId.GenerateNewId(), pipelineExecutionId, pipelineEntityId,
                DateTime.UtcNow, null, new GlobalConfiguration(new List<ConfigurationDto>()),
                new Dictionary<string, object?>()), debugger);


        Assert.Equal("{\"TestOutput0\":100,\"TestOutput1\":101,\"TestOutput2\":102,\"TestOutput3\":103}",
            ((JObject?)result)?.ToString(Newtonsoft.Json.Formatting.None));
        var debugInfo = debugger.GetDebugInformation();
        Assert.NotNull(debugInfo);
        Assert.Equal(5, debugInfo.DebugPoints.Count);
        Assert.Equal("{\"TestOutput0\":100}",
            debugInfo.DebugPoints.FirstOrDefault(db => db.NodePath == "PipelineExecution/TestOutput@1")?.Output);
        Assert.Equal("{\"TestOutput0\":100,\"TestOutput1\":101}",
            debugInfo.DebugPoints.FirstOrDefault(db => db.NodePath == "PipelineExecution/TestOutput@1[1]")?.Output);
        Assert.Equal("{\"TestOutput0\":100,\"TestOutput1\":101,\"TestOutput2\":102}",
            debugInfo.DebugPoints.FirstOrDefault(db => db.NodePath == "PipelineExecution/TestOutput@1[2]")?.Output);
        Assert.Equal("{\"TestOutput0\":100,\"TestOutput1\":101,\"TestOutput2\":102,\"TestOutput3\":103}",
            debugInfo.DebugPoints.FirstOrDefault(db => db.NodePath == "PipelineExecution/TestOutput@1[3]")?.Output);

        Assert.Equal("{\"TestOutput0\":100,\"TestOutput1\":101,\"TestOutput2\":102,\"TestOutput3\":103}",
            debugInfo.DebugPoints.FirstOrDefault(db => db.NodePath == "PipelineExecution")?.Output);
    }

    [Fact]
    public async Task ExecutePipelineAsync_WithDebugWithInput_OK()
    {
        fixture.UseXUnitLoggerFactory(testOutputHelper);
        var serviceProvider = fixture.Services.BuildServiceProvider();

        var testData = Generator.GenerateSimpleData();

        var dataOrchestrator = new EtlDataOrchestrator(serviceProvider,
            serviceProvider.GetRequiredService<INodeLookupService>());
        var debugger = new DefaultPipelineDebugger(serviceProvider.GetRequiredService<ILoggerFactory>());

        var pipelineExecutionId = Guid.NewGuid();
        var pipelineEntityId = new RtEntityId("System.Communication/EdgePipeline", OctoObjectId.GenerateNewId());
        debugger.RegisterPipelineRtEntityId(pipelineEntityId, pipelineExecutionId);

        var result = await dataOrchestrator.ExecutePipelineAsync(TestPipelineConfigurations.TestDataSingleNode,
            new DefaultEtlContext("test1", OctoObjectId.GenerateNewId(), pipelineExecutionId, pipelineEntityId,
                DateTime.UtcNow, null, new GlobalConfiguration(new List<ConfigurationDto>()),
                new Dictionary<string, object?>()), debugger, testData);

        Assert.IsType<JObject>(result);

        var jObjectResult = (JObject)result;
        Assert.Equal(100, jObjectResult.SelectToken("$.TestOutput"));
        Assert.Equal(testData.Value, jObjectResult.SelectToken("$.Value"));

        var debugInfo = debugger.GetDebugInformation();
        Assert.NotNull(debugInfo);
        Assert.Equal(2, debugInfo.DebugPoints.Count);

        Assert.Equal("{\"Value\":" + testData.Value + "}",
            debugInfo.DebugPoints.FirstOrDefault(db => db.NodePath == "PipelineExecution")?.Input);
        Assert.Equal("{\"Value\":" + testData.Value + ",\"TestOutput\":100}",
            debugInfo.DebugPoints.FirstOrDefault(db => db.NodePath == "PipelineExecution")?.Output);
    }
}