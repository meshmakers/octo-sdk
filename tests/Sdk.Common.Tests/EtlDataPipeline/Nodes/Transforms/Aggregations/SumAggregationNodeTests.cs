#pragma warning disable CS8602 // Dereference of a possibly null reference.

using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms.Aggregations;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms.Aggregations;

public class SumAggregationNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(SumAggregationNodeConfiguration sumAggregationNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                orderItems = new[]
                {
                    new { type = "product", price = 100.0, quantity = 2, status = "active" },
                    new { type = "product", price = 50.0, quantity = 1, status = "active" },
                    new { type = "service", price = 25.0, quantity = 3, status = "active" },
                    new { type = "product", price = 75.0, quantity = 1, status = "inactive" }
                },
                adjustments = new[]
                {
                    new { type = "discount", amount = 10.0, category = "promotional" },
                    new { type = "discount", amount = 5.0, category = "loyalty" },
                    new { type = "tax", amount = 15.0, category = "vat" },
                    new { type = "fee", amount = 3.0, category = "processing" }
                },
                inventory = new[]
                {
                    new { productId = "A1", unitCost = 20.0, stock = 10 },
                    new { productId = "B2", unitCost = 15.5, stock = 25 },
                    new { productId = "C3", unitCost = 8.75, stock = 50 }
                },
                financials = new
                {
                    revenues = new[] { 1000.0, 1500.0, 2000.0 },
                    expenses = new[] { 500.0, 750.0, 900.0 },
                    taxes = new[] { 100.0, 150.0, 200.0 }
                }
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SumAggregation", 0, sumAggregationNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_SimpleSum_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.totalPrice",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.orderItems[*]",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.price",
                    Value = 1.0
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(250.0, dataContext.Current["totalPrice"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithMultiplier_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.discountedTotal",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.orderItems[*]",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.price",
                    Value = 0.9 // 10% discount
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Should sum all prices with 10% discount: (100 + 50 + 25 + 75) * 0.9 = 225.0
        Assert.Equal(225.0, dataContext.Current["discountedTotal"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_MultipleAggregations_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.combinedTotal",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.orderItems[*]",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.price",
                    Value = 1.0 // Add all order items
                },
                new SumAggregationItem
                {
                    Path = "$.adjustments[*]",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.amount",
                    Value = -0.5 // Subtract half of all adjustments
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Order items: 250, adjustments total: 33 * -0.5 = -16.5, combined: 250 - 16.5 = 233.5
        Assert.Equal(233.5, dataContext.Current["combinedTotal"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_ArrayAggregation_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.totalRevenue",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.financials",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.revenues[*]",
                    Value = 1.0
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Should sum all revenue values: 1000 + 1500 + 2000 = 4500
        Assert.Equal(4500.0, dataContext.Current["totalRevenue"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WeightedCalculation_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.inventoryValue",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.inventory[0]", // First item
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.unitCost",
                    Value = 10 // 10 units
                },
                new SumAggregationItem
                {
                    Path = "$.inventory[1]", // Second item
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.unitCost",
                    Value = 25 // 25 units
                },
                new SumAggregationItem
                {
                    Path = "$.inventory[2]", // Third item
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.unitCost",
                    Value = 50 // 50 units
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Should calculate: (20 * 10) + (15.5 * 25) + (8.75 * 50) = 200 + 387.5 + 437.5 = 1025
        Assert.Equal(1025.0, dataContext.Current["inventoryValue"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyPath_ReturnsZero()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.emptyTotal",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.nonexistent[*]",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.price",
                    Value = 1.0
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(0.0, dataContext.Current["emptyTotal"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullInputData_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext { Current = null };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);

        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.total",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.items[*]",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.value",
                    Value = 1.0
                }
            }
        };

        var nodeContext = rootNodeContext.RegisterChildNode("SumAggregation", 0, sumAggregationNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NegativeMultiplier_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.netProfit",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.financials",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.revenues[*]",
                    Value = 1.0 // Add revenues
                },
                new SumAggregationItem
                {
                    Path = "$.financials",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.expenses[*]",
                    Value = -1.0 // Subtract expenses
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Revenues: 4500, Expenses: 2150, Net: 4500 - 2150 = 2350
        Assert.Equal(2350.0, dataContext.Current["netProfit"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_ZeroMultiplier_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.zeroTotal",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.orderItems[*]",
                    FilterPath = null,
                    ComparisonValue = null,
                    AggregationPath = "$.price",
                    Value = 0.0 // Zero multiplier
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(0.0, dataContext.Current["zeroTotal"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyFilterPath_IncludesAll()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.allItemsTotal",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.orderItems[*]",
                    FilterPath = "", // Empty filter path
                    ComparisonValue = "anything",
                    AggregationPath = "$.price",
                    Value = 1.0
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Should sum all items since filter path is empty: 100 + 50 + 25 + 75 = 250
        Assert.Equal(250.0, dataContext.Current["allItemsTotal"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithStringFilter_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.activeTotal",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.orderItems[*]",
                    FilterPath = "$.status",
                    ComparisonValue = "active",
                    AggregationPath = "$.price",
                    Value = 1.0
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Should sum only active items: 100 + 50 + 25 = 175 (excludes inactive 75)
        Assert.Equal(175.0, dataContext.Current["activeTotal"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithTypeFilter_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.productTotal",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.orderItems[*]",
                    FilterPath = "$.type",
                    ComparisonValue = "product",
                    AggregationPath = "$.price",
                    Value = 1.0
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Should sum only product items: 100 + 50 + 75 = 225 (excludes service 25)
        Assert.Equal(225.0, dataContext.Current["productTotal"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_FilteredMultipleAggregations_OK()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.netTotal",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.orderItems[*]",
                    FilterPath = "$.status",
                    ComparisonValue = "active",
                    AggregationPath = "$.price",
                    Value = 1.0
                },
                new SumAggregationItem
                {
                    Path = "$.adjustments[*]",
                    FilterPath = "$.type",
                    ComparisonValue = "discount",
                    AggregationPath = "$.amount",
                    Value = -1.0 // Subtract discounts
                },
                new SumAggregationItem
                {
                    Path = "$.adjustments[*]",
                    FilterPath = "$.type",
                    ComparisonValue = "tax",
                    AggregationPath = "$.amount",
                    Value = 1.0 // Add taxes
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Active items: 175, discounts: -15, taxes: +15 = 175
        Assert.Equal(175.0, dataContext.Current["netTotal"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NoFilterMatches_ReturnsZero()
    {
        // Arrange
        SumAggregationNodeConfiguration sumAggregationNodeConfiguration = new()
        {
            TargetPath = "$.noMatchTotal",
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite,
            Aggregations = new[]
            {
                new SumAggregationItem
                {
                    Path = "$.orderItems[*]",
                    FilterPath = "$.status",
                    ComparisonValue = "nonexistent",
                    AggregationPath = "$.price",
                    Value = 1.0
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(sumAggregationNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SumAggregationNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(0.0, dataContext.Current["noMatchTotal"]!.ToObject<double>());
    }
}