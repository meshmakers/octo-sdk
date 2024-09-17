using FakeItEasy;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Runtime.Contracts;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Meshmakers.Octo.Runtime.Contracts.Serialization;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class DataContextTests(ServiceCollectionFixture fixture) : IClassFixture<ServiceCollectionFixture>
{
    public static IEnumerable<object[]> GetPrimitiveTestData()
    {
        yield return [5];
        yield return [5.358f];
        yield return [677.7758d];
        yield return [true];
        yield return [false];
        yield return ["Test"];
    }

    public static IEnumerable<object[]> GetPrimitiveArrayTestData()
    {
        yield return [new[] { 5, 6, 7 }];
        yield return [new[] { 5.358f, 674.5995f }];
        yield return [new[] { 677.7758d, 2334.38855d }];
        yield return [new[] { true, false }];
        yield return [new[] { "Test", "Demo", "Hansi" }];
    }

    public static IEnumerable<object[]> GetComplexTestData()
    {
        yield return [Generator.GenerateOrder()];
    }

    public static IEnumerable<object[]> GetComplexArrayTestData()
    {
        yield return
        [
            new[]
            {
                Generator.GenerateOrder(), Generator.GenerateOrder(), Generator.GenerateOrder(),
                Generator.GenerateOrder()
            }
        ];
    }

    #region Primitive Data

    [Theory]
    [MemberData(nameof(GetPrimitiveTestData))]
    public void SetValueByPath_Primitive_WithName_OK(object o)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("Test", ValueKind.Simple, WriteMode.Overwrite, o);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(o, ((JValue)dataContext.Current["Test"]!).Value);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveTestData))]
    public void SetValueByPath_Primitive_NoName_OK(object o)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath(null, ValueKind.Simple, WriteMode.Overwrite, o);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(o, ((JValue)dataContext.Current).Value);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void SetValueByPath_PrimitiveArray_WithName_OK<TValue>(ICollection<TValue> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("Test", ValueKind.Simple, WriteMode.Overwrite, e);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, ((JArray)dataContext.Current["Test"]!).Values<TValue>()!);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void SetValueByPath_PrimitiveArray_NoName_OK<TValue>(ICollection<TValue> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath(null, ValueKind.Simple, WriteMode.Overwrite, e);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, ((JArray)dataContext.Current).Values<TValue>()!);
    }
    
    [Fact]
    public void SetValueByPath_JArray_Fail()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger)
        {
            Current = new JArray()
        };
        Assert.Throws<DataPipelineException>(() => dataContext.SetValueByPath("$.x", ValueKind.Simple, WriteMode.Overwrite, new JArray()));
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void GetSimpleValueByPath_PrimitiveArrayMismatch_Fail<TValue>(TValue e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("Test", ValueKind.Simple, WriteMode.Overwrite, e);

        Assert.Throws<DataPipelineException>(() => dataContext.GetSimpleValueByPath<TValue>("Test"));
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void GetSimpleValueByPath_WithNameNoNameMismatch_Fail<TValue>(TValue e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("Test", ValueKind.Simple, WriteMode.Overwrite, e);

        Assert.Throws<DataPipelineException>(() => dataContext.GetSimpleArrayValueByPath<TValue>(null));
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveTestData))]
    public void GetSimpleValueByPath_Primitive_NoName_OK<TValue>(TValue e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath(null, ValueKind.Simple, WriteMode.Overwrite, e);

        var r = dataContext.GetSimpleValueByPath<TValue>(null);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, r);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveTestData))]
    public void GetSimpleValueByPath_Primitive_WithName_OK<TValue>(TValue e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("test", ValueKind.Simple, WriteMode.Overwrite, e);

        var r = dataContext.GetSimpleValueByPath<TValue>("test");

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, r);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void GetSimpleArrayValueByPath_PrimitiveArray_NoName_OK<TValue>(ICollection<TValue> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath(null, ValueKind.Simple, WriteMode.Overwrite, e);

        var r = dataContext.GetSimpleArrayValueByPath<TValue>(null);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, r!);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void GetCurrentValueByPath_PrimitiveArray_WithName_OK<TValue>(ICollection<TValue> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("test", ValueKind.Simple, WriteMode.Overwrite, e);

        var r = dataContext.GetSimpleArrayValueByPath<TValue>("test");

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, r!);
    }

    #endregion Primitive Data

    #region Complex Data

    [Theory]
    [MemberData(nameof(GetComplexTestData))]
    public void SetValueByPath_Complex_WithName_OK(Order e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("Test", ValueKind.Simple, WriteMode.Overwrite, e);

        Assert.NotNull(dataContext.Current);
        var a = (JObject)dataContext.Current["Test"]!;
        Assert.Equal(e.InvoiceNumber, a["InvoiceNumber"]);
    }

    [Theory]
    [MemberData(nameof(GetComplexTestData))]
    public void SetValueByPath_Complex_NoName_OK(Order e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath(null, ValueKind.Simple, WriteMode.Overwrite, e);

        Assert.NotNull(dataContext.Current);
        var a = (JObject)dataContext.Current;
        Assert.Equal(e.InvoiceNumber, a["InvoiceNumber"]);
    }

    [Theory]
    [MemberData(nameof(GetComplexArrayTestData))]
    public void SetValueByPath_ComplexArray_WithName_OK(ICollection<Order> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("Test", ValueKind.Simple, WriteMode.Overwrite, e);

        Assert.NotNull(dataContext.Current);
        var a = (JArray)dataContext.Current["Test"]!;

        for (int i = 0; i < e.Count; i++)
        {
            Assert.Equal(e.ElementAt(i).InvoiceNumber, a[i]["InvoiceNumber"]);
        }
    }

    [Theory]
    [MemberData(nameof(GetComplexArrayTestData))]
    public void SetValueByPath_ComplexArray_NoName_OK(ICollection<Order> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath(null, ValueKind.Simple, WriteMode.Overwrite, e);

        Assert.NotNull(dataContext.Current);
        var a = (JArray)dataContext.Current;
        for (int i = 0; i < e.Count; i++)
        {
            Assert.Equal(e.ElementAt(i).InvoiceNumber, a[i]["InvoiceNumber"]);
        }
    }

    #endregion Region Complex Data


    [Fact]
    public void CreateCurrentIfNull_OK()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);
        Assert.Null(dataContext.Current);

        dataContext.CreateCurrentIfNull();

        Assert.NotNull(dataContext.Current);
    }

    [Fact]
    public void CreateCurrentIfNull_NoReplace_OK()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger)
        {
            Current = new JObject
            {
                ["Date"] = DateTime.Now,
                ["Album"] = "Me Against The World",
                ["Year"] = 1995,
                ["Artist"] = "2Pac"
            }
        };
        Assert.NotNull(dataContext.Current);

        dataContext.CreateCurrentIfNull();

        Assert.NotNull(dataContext.Current);
        Assert.Equal(1995, dataContext.Current["Year"]);
    }

    [Fact]
    public void SetValueByPath_Array_Append_Single_OK()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var e = Generator.GenerateOrder();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("$.Item.Test", ValueKind.Array, WriteMode.Append, e);

        Assert.NotNull(dataContext.Current);
        var a = (JArray?)dataContext.Current.SelectToken("$.Item.Test");
        Assert.NotNull(a);
        Assert.Equal(e.InvoiceNumber, a[0]["InvoiceNumber"]?.Value<int>());
    }

    [Fact]
    public void SetValueByPath_Array_Append_Multiple_OK()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var e1 = Generator.GenerateOrder();
        var e2 = Generator.GenerateOrder();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("$.Item.Test", ValueKind.Array, WriteMode.Append, e1);
        dataContext.SetValueByPath("$.Item.Test", ValueKind.Array, WriteMode.Append, e2);

        Assert.NotNull(dataContext.Current);
        var a = (JArray?)dataContext.Current.SelectToken("$.Item.Test");
        Assert.NotNull(a);
        Assert.Equal(2, a.Count);
        Assert.Equal(e1.InvoiceNumber, a[0]["InvoiceNumber"]?.Value<int>());
        Assert.Equal(e2.InvoiceNumber, a[1]["InvoiceNumber"]?.Value<int>());
    }

    [Fact]
    public void SetValueByPath_Array_Prepend_Single_OK()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var e = Generator.GenerateOrder();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("$.Item.Test", ValueKind.Array, WriteMode.Prepend, e);

        Assert.NotNull(dataContext.Current);
        var a = (JArray?)dataContext.Current.SelectToken("$.Item.Test");
        Assert.NotNull(a);
        Assert.Equal(e.InvoiceNumber, a[0]["InvoiceNumber"]?.Value<int>());
    }

    [Fact]
    public void SetValueByPath_Array_Prepend_Multiple_OK()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var e1 = Generator.GenerateOrder();
        var e2 = Generator.GenerateOrder();

        var dataContext = new DataContext(globalServiceProvider, logger);
        dataContext.SetValueByPath("$.Item.Test", ValueKind.Array, WriteMode.Prepend, e1);
        dataContext.SetValueByPath("$.Item.Test", ValueKind.Array, WriteMode.Prepend, e2);

        Assert.NotNull(dataContext.Current);
        var a = (JArray?)dataContext.Current.SelectToken("$.Item.Test");
        Assert.NotNull(a);
        Assert.Equal(2, a.Count);
        Assert.Equal(e2.InvoiceNumber, a[0]["InvoiceNumber"]?.Value<int>());
        Assert.Equal(e1.InvoiceNumber, a[1]["InvoiceNumber"]?.Value<int>());
    }

    [Fact]
    public void GetComplexObjectByPath_UpdateItems_OK()
    {
        var rtEntity = new RtEntity("System/MyType", OctoObjectId.GenerateNewId(),
            new Dictionary<string, object?>
            {
                ["Test"] = "Test"
            });

        List<IEntityUpdateInfo<RtEntity>> e =
        [
            EntityUpdateInfo<RtEntity>.CreateUpdate(rtEntity.ToRtEntityId(), rtEntity)
        ];

        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();

        var dataContext = new DataContext(globalServiceProvider, logger);

        dataContext.SetValueByPath("Test", e, ValueKind.Simple, WriteMode.Overwrite,
            RtNewtonsoftSerializer.DefaultSerializer);

        Assert.NotNull(dataContext.Current);
        var a = dataContext.GetComplexObjectByPath<List<EntityUpdateInfo<RtEntity>>>("Test",
            RtNewtonsoftSerializer.DefaultSerializer);
        Assert.Equivalent(e, a);
    }

    [Fact]
    public void GetComplexObjectByPath_EmptyFields_OK()
    {
        var rtEntity = new RtEntity(null!, OctoObjectId.Empty,
            new Dictionary<string, object?>
            {
                ["Test"] = "Test"
            });
       
        List<IEntityUpdateInfo<RtEntity>> e =
        [
            EntityUpdateInfo<RtEntity>.CreateUpdate(new RtEntityId("System/Test", OctoObjectId.GenerateNewId()), rtEntity)
        ];
        
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var logger = A.Fake<IPipelineLogger>();
        
        var dataContext = new DataContext(globalServiceProvider, logger);
    
        dataContext.SetValueByPath("Test", e, ValueKind.Simple, WriteMode.Overwrite,
            RtNewtonsoftSerializer.DefaultSerializer);
        Assert.NotNull(dataContext.Current);
        var a = dataContext.GetComplexObjectByPath<List<EntityUpdateInfo<RtEntity>>>("Test", RtNewtonsoftSerializer.DefaultSerializer);
        Assert.Equivalent(e, a);
    }
}