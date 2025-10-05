using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class FormatStringNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(FormatStringNodeConfiguration configuration, JObject? testData = null)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = testData ?? new JObject
            {
                ["user"] = new JObject
                {
                    ["name"] = "John Doe",
                    ["age"] = 30,
                    ["email"] = "john@example.com"
                },
                ["items"] = new JArray { "apple", "banana", "orange" },
                ["count"] = 5,
                ["isActive"] = true,
                ["nullField"] = null
            }
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("FormatString", 0, configuration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_SimpleFormat_OK()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "Hello {$.user.name}, you have {$.count} items",
            TargetPath = "$.message"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal("Hello John Doe, you have 5 items", dataContext.GetSimpleValueByPath<string>("$.message"));
    }

    [Fact]
    public async Task ProcessObjectAsync_MultipleJsonPaths_OK()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "User {$.user.name} ({$.user.email}) is {$.user.age} years old and has {$.count} items",
            TargetPath = "$.formattedUser"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("User John Doe (john@example.com) is 30 years old and has 5 items",
            dataContext.GetSimpleValueByPath<string>("$.formattedUser"));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValue_UsesNullString()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "Field value: {$.nullField}",
            TargetPath = "$.result",
            NullValue = "NULL"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("Field value: NULL", dataContext.GetSimpleValueByPath<string>("$.result"));
    }

    [Fact]
    public async Task ProcessObjectAsync_CustomNullValue_OK()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "Status: {$.nullField}",
            TargetPath = "$.status",
            NullValue = "N/A"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        Assert.Equal("Status: N/A", dataContext.GetSimpleValueByPath<string>("$.status"));
    }

    [Fact]
    public async Task ProcessObjectAsync_BooleanValue_OK()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "User is active: {$.isActive}",
            TargetPath = "$.activeStatus"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        Assert.Equal("User is active: True", dataContext.GetSimpleValueByPath<string>("$.activeStatus"));
    }

    [Fact]
    public async Task ProcessObjectAsync_PathNotFound_ThrowsException()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "Value: {$.nonexistent.path}",
            TargetPath = "$.result"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(
            async () => await testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_ArrayPath_ThrowsException()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "Items: {$.items}",
            TargetPath = "$.result"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(
            async () => await testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_ObjectPath_ThrowsException()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "User: {$.user}",
            TargetPath = "$.result"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(
            async () => await testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoPlaceholders_ReturnsOriginalFormat()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "This is a static message with no placeholders",
            TargetPath = "$.staticMessage"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        Assert.Equal("This is a static message with no placeholders",
            dataContext.GetSimpleValueByPath<string>("$.staticMessage"));
    }

    [Fact]
    public async Task ProcessObjectAsync_RepeatedPlaceholder_OK()
    {
        var configuration = new FormatStringNodeConfiguration
        {
            Format = "{$.user.name} - {$.user.name} has {$.count} items",
            TargetPath = "$.repeated"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new FormatStringNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        Assert.Equal("John Doe - John Doe has 5 items",
            dataContext.GetSimpleValueByPath<string>("$.repeated"));
    }
}