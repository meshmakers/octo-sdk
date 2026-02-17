using System.Net;
using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Load;

public class ToWebhookNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(ToWebhookNodeConfiguration config, object? data = null)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(data ?? new { value = "test" })
        };

        var rootNodeContext =
            NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("ToWebhookNode", 0, config, dataContext);

        return (dataContext, nodeContext);
    }

    private static (IHttpClientFactory, FakeHttpMessageHandler) CreateFakeHttpClientFactory(
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(statusCode);
        var client = new HttpClient(handler);
        var factory = A.Fake<IHttpClientFactory>();
        A.CallTo(() => factory.CreateClient("Webhook")).Returns(client);
        return (factory, handler);
    }

    [Fact]
    public async Task ProcessObjectAsync_PostsPayloadToUri_OK()
    {
        // Arrange
        var config = new ToWebhookNodeConfiguration
        {
            Uri = "https://example.com/webhook",
            Path = "$"
        };
        var testData = new { temperature = 42.5, sensor = "T1" };
        var (dataContext, nodeContext) = PrepareTest(config, testData);
        var (httpClientFactory, handler) = CreateFakeHttpClientFactory();
        var fn = A.Fake<NodeDelegate>();
        var testee = new ToWebhookNode(fn, httpClientFactory);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("https://example.com/webhook", handler.LastRequest.RequestUri?.ToString());

        Assert.NotNull(handler.LastRequestBody);
        var json = JObject.Parse(handler.LastRequestBody);
        Assert.Equal(42.5, json["temperature"]?.Value<double>());
        Assert.Equal("T1", json["sensor"]?.Value<string>());

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithApiKey_SetsHeader()
    {
        // Arrange
        var config = new ToWebhookNodeConfiguration
        {
            Uri = "https://example.com/webhook",
            ApiKey = "my-secret-key",
            Path = "$"
        };
        var (dataContext, nodeContext) = PrepareTest(config);
        var (httpClientFactory, handler) = CreateFakeHttpClientFactory();
        var fn = A.Fake<NodeDelegate>();
        var testee = new ToWebhookNode(fn, httpClientFactory);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest.Headers.Contains("XApiKey"));
        Assert.Equal("my-secret-key", handler.LastRequest.Headers.GetValues("XApiKey").First());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithoutApiKey_NoHeader()
    {
        // Arrange
        var config = new ToWebhookNodeConfiguration
        {
            Uri = "https://example.com/webhook",
            Path = "$"
        };
        var (dataContext, nodeContext) = PrepareTest(config);
        var (httpClientFactory, handler) = CreateFakeHttpClientFactory();
        var fn = A.Fake<NodeDelegate>();
        var testee = new ToWebhookNode(fn, httpClientFactory);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(handler.LastRequest);
        Assert.False(handler.LastRequest.Headers.Contains("XApiKey"));
    }

    [Fact]
    public async Task ProcessObjectAsync_HttpError_ThrowsException()
    {
        // Arrange
        var config = new ToWebhookNodeConfiguration
        {
            Uri = "https://example.com/webhook",
            Path = "$"
        };
        var (dataContext, nodeContext) = PrepareTest(config);
        var (httpClientFactory, _) = CreateFakeHttpClientFactory(HttpStatusCode.InternalServerError);
        var fn = A.Fake<NodeDelegate>();
        var testee = new ToWebhookNode(fn, httpClientFactory);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => testee.ProcessObjectAsync(dataContext, nodeContext));

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithSubPath_SendsSubset()
    {
        // Arrange
        var config = new ToWebhookNodeConfiguration
        {
            Uri = "https://example.com/webhook",
            Path = "$.nested"
        };
        var testData = new { nested = new { key = "value" }, other = "ignored" };
        var (dataContext, nodeContext) = PrepareTest(config, testData);
        var (httpClientFactory, handler) = CreateFakeHttpClientFactory();
        var fn = A.Fake<NodeDelegate>();
        var testee = new ToWebhookNode(fn, httpClientFactory);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(handler.LastRequestBody);
        var json = JObject.Parse(handler.LastRequestBody);
        Assert.Equal("value", json["key"]?.Value<string>());

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Test helper that captures HTTP requests for verification
    /// </summary>
    private class FakeHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(statusCode);
        }
    }
}
