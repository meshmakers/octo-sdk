#pragma warning disable CS8602 // Dereference of a possibly null reference.

using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class JoinNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(JoinNodeConfiguration joinNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                orders = new[]
                {
                    new { orderId = "123", customerName = "John Doe", customerId = "c1" },
                    new { orderId = "456", customerName = "Jane Smith", customerId = "c2" },
                    new { orderId = "789", customerName = "Bob Wilson", customerId = "c3" }
                },
                orderItems = new[]
                {
                    new { orderId = "123", productName = "Widget", quantity = 2, price = 10.50 },
                    new { orderId = "123", productName = "Gadget", quantity = 1, price = 25.00 },
                    new { orderId = "456", productName = "Tool", quantity = 3, price = 15.75 },
                    new { orderId = "456", productName = "Widget", quantity = 1, price = 10.50 },
                    new { orderId = "999", productName = "Orphaned", quantity = 1, price = 5.00 } // No matching order
                },
                customers = new[]
                {
                    new { customerId = "c1", address = "123 Main St", city = "New York" },
                    new { customerId = "c2", address = "456 Oak Ave", city = "Chicago" },
                    new { customerId = "c3", address = "789 Pine Rd", city = "Seattle" }
                }
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Join", 0, joinNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_BasicJoin_OK()
    {
        // Arrange
        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[*]",
            KeyPath = "$.orderId",
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var (dataContext, nodeContext) = PrepareTest(joinNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();

        // Check first order (orderId: "123") has 2 items
        var order1Items = dataContext.Current["orders"]![0]!["items"] as JArray;
        Assert.NotNull(order1Items);
        Assert.Equal(2, order1Items.Count);
        Assert.Equal("Widget", order1Items[0]!["productName"]!.ToString());
        Assert.Equal("Gadget", order1Items[1]!["productName"]!.ToString());

        // Check second order (orderId: "456") has 2 items
        var order2Items = dataContext.Current["orders"]![1]!["items"] as JArray;
        Assert.NotNull(order2Items);
        Assert.Equal(2, order2Items.Count);
        Assert.Equal("Tool", order2Items[0]!["productName"]!.ToString());
        Assert.Equal("Widget", order2Items[1]!["productName"]!.ToString());

        // Check third order (orderId: "789") has no items (empty array)
        var order3Items = dataContext.Current["orders"]![2]!["items"] as JArray;
        Assert.NotNull(order3Items);
        Assert.Empty(order3Items);
    }

    [Fact]
    public async Task ProcessObjectAsync_MultipleMatches_OK()
    {
        // Arrange
        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[0]", // Only first order
            KeyPath = "$.orderId",
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.matchedItems"
        };

        var (dataContext, nodeContext) = PrepareTest(joinNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();

        var matchedItems = dataContext.Current["orders"]![0]!["matchedItems"] as JArray;
        Assert.NotNull(matchedItems);
        Assert.Equal(2, matchedItems.Count);

        // Verify the matched items contain complete records
        Assert.Equal("123", matchedItems[0]!["orderId"]!.ToString());
        Assert.Equal(2, matchedItems[0]!["quantity"]!.ToObject<int>());
        Assert.Equal(10.50, matchedItems[0]!["price"]!.ToObject<double>());

        Assert.Equal("123", matchedItems[1]!["orderId"]!.ToString());
        Assert.Equal(1, matchedItems[1]!["quantity"]!.ToObject<int>());
        Assert.Equal(25.00, matchedItems[1]!["price"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NoMatches_EmptyArray()
    {
        // Arrange
        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[2]", // Third order (orderId: "789") has no matching items
            KeyPath = "$.orderId",
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var (dataContext, nodeContext) = PrepareTest(joinNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();

        var items = dataContext.Current["orders"]![2]!["items"] as JArray;
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task ProcessObjectAsync_DifferentJoinPath_OK()
    {
        // Arrange - Join orders with customers
        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[*]",
            KeyPath = "$.customerId",
            JoinPath = "$.customers[*]",
            JoinKeyPath = "$.customerId",
            ItemPath = "$.customerDetails"
        };

        var (dataContext, nodeContext) = PrepareTest(joinNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();

        // Each order should have exactly one customer detail (1:1 join)
        var order1Customer = dataContext.Current["orders"]![0]!["customerDetails"] as JArray;
        Assert.NotNull(order1Customer);
        Assert.Single(order1Customer);
        Assert.Equal("123 Main St", order1Customer[0]!["address"]!.ToString());
        Assert.Equal("New York", order1Customer[0]!["city"]!.ToString());

        var order2Customer = dataContext.Current["orders"]![1]!["customerDetails"] as JArray;
        Assert.NotNull(order2Customer);
        Assert.Single(order2Customer);
        Assert.Equal("456 Oak Ave", order2Customer[0]!["address"]!.ToString());
        Assert.Equal("Chicago", order2Customer[0]!["city"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullInputData_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext { Current = null };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);

        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[*]",
            KeyPath = "$.orderId",
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var nodeContext = rootNodeContext.RegisterChildNode("Join", 0, joinNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoSourceData_ThrowsException()
    {
        // Arrange
        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.nonexistent[*]", // Path doesn't exist
            KeyPath = "$.orderId",
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var (dataContext, nodeContext) = PrepareTest(joinNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoJoinData_ThrowsException()
    {
        // Arrange
        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[*]",
            KeyPath = "$.orderId",
            JoinPath = "$.nonexistent[*]", // Join path doesn't exist
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var (dataContext, nodeContext) = PrepareTest(joinNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyKeyValue_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                orders = new[]
                {
                    new { orderId = "", customerName = "John Doe" }, // Empty orderId
                },
                orderItems = new[]
                {
                    new { orderId = "123", productName = "Widget" }
                }
            })
        };

        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[*]",
            KeyPath = "$.orderId",
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Join", 0, joinNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullKeyValue_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                orders = new[]
                {
                    new { orderId = (string?)null, customerName = "John Doe" }, // Null orderId
                },
                orderItems = new[]
                {
                    new { orderId = "123", productName = "Widget" }
                }
            })
        };

        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[*]",
            KeyPath = "$.orderId",
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Join", 0, joinNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_MissingKeyPath_ThrowsException()
    {
        // Arrange
        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[*]",
            KeyPath = "$.nonexistentKey", // Key path doesn't exist in source data
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var (dataContext, nodeContext) = PrepareTest(joinNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_CaseSensitiveMatching_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                orders = new[]
                {
                    new { orderId = "ABC", customerName = "John Doe" },
                },
                orderItems = new[]
                {
                    new { orderId = "abc", productName = "Widget" }, // Different case
                    new { orderId = "ABC", productName = "Gadget" }  // Exact match
                }
            })
        };

        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[*]",
            KeyPath = "$.orderId",
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Join", 0, joinNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();

        var items = dataContext.Current["orders"]![0]!["items"] as JArray;
        Assert.NotNull(items);
        Assert.Single(items); // Only exact case match should be included
        Assert.Equal("Gadget", items[0]!["productName"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_NumericKeys_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                orders = new[]
                {
                    new { orderId = 123, customerName = "John Doe" },
                    new { orderId = 456, customerName = "Jane Smith" }
                },
                orderItems = new[]
                {
                    new { orderId = 123, productName = "Widget" },
                    new { orderId = 456, productName = "Gadget" },
                    new { orderId = 123, productName = "Tool" }
                }
            })
        };

        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.orders[*]",
            KeyPath = "$.orderId",
            JoinPath = "$.orderItems[*]",
            JoinKeyPath = "$.orderId",
            ItemPath = "$.items"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Join", 0, joinNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();

        // First order should have 2 items (orderId: 123)
        var order1Items = dataContext.Current["orders"]![0]!["items"] as JArray;
        Assert.NotNull(order1Items);
        Assert.Equal(2, order1Items.Count);

        // Second order should have 1 item (orderId: 456)
        var order2Items = dataContext.Current["orders"]![1]!["items"] as JArray;
        Assert.NotNull(order2Items);
        Assert.Single(order2Items);
    }

    [Fact]
    public async Task ProcessObjectAsync_NestedPaths_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                data = new
                {
                    orders = new[]
                    {
                        new { details = new { id = "123" }, customerName = "John Doe" }
                    },
                    items = new[]
                    {
                        new { order = new { id = "123" }, productName = "Widget" }
                    }
                }
            })
        };

        JoinNodeConfiguration joinNodeConfiguration = new()
        {
            Path = "$.data.orders[*]",
            KeyPath = "$.details.id",
            JoinPath = "$.data.items[*]",
            JoinKeyPath = "$.order.id",
            ItemPath = "$.products"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Join", 0, joinNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new JoinNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();

        var products = dataContext.Current["data"]!["orders"]![0]!["products"] as JArray;
        Assert.NotNull(products);
        Assert.Single(products);
        Assert.Equal("Widget", products[0]!["productName"]!.ToString());
    }
}