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
        yield return [new[] { Generator.GenerateOrder(), Generator.GenerateOrder(), Generator.GenerateOrder(), Generator.GenerateOrder() }];
    }

    #region Primitive Data

    [Theory]
    [MemberData(nameof(GetPrimitiveTestData))]
    public void SetCurrentValueByName_Primitive_WithName_OK(object o)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName("Test", o);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(o, ((JValue)dataContext.Current["Test"]!).Value);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveTestData))]
    public void SetCurrentValueByName_Primitive_NoName_OK(object o)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName(null, o);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(o, ((JValue)dataContext.Current).Value);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void SetCurrentValueByName_PrimitiveArray_WithName_OK<TValue>(ICollection<TValue> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName("Test", e);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, ((JArray)dataContext.Current["Test"]!).Values<TValue>()!);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void SetCurrentValueByName_PrimitiveArray_NoName_OK<TValue>(ICollection<TValue> o)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName(null, o);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(o, ((JArray)dataContext.Current).Values<TValue>()!);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void GetCurrentValueByName_PrimitiveArrayMismatch_Fail<TValue>(TValue e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName("Test", e);

        Assert.Throws<DataPipelineException>(() => dataContext.GetCurrentValueByName<TValue>("Test"));
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void GetCurrentValuesByName_WithNameNoNameMismatch_Fail<TValue>(TValue e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName("Test", e);

        Assert.Throws<DataPipelineException>(() => dataContext.GetCurrentValuesByName<TValue>(null));
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveTestData))]
    public void GetCurrentValueByName_Primitive_NoName_OK<TValue>(TValue e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName(null, e);

        var r = dataContext.GetCurrentValueByName<TValue>(null);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, r);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveTestData))]
    public void GetCurrentValueByName_Primitive_WithName_OK<TValue>(TValue e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName("test", e);

        var r = dataContext.GetCurrentValueByName<TValue>("test");

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, r);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void GetCurrentValueByName_PrimitiveArray_NoName_OK<TValue>(ICollection<TValue> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName(null, e);

        var r = dataContext.GetCurrentValuesByName<TValue>(null);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, r!);
    }

    [Theory]
    [MemberData(nameof(GetPrimitiveArrayTestData))]
    public void GetCurrentValueByName_PrimitiveArray_WithName_OK<TValue>(ICollection<TValue> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName("test", e);

        var r = dataContext.GetCurrentValuesByName<TValue>("test");

        Assert.NotNull(dataContext.Current);
        Assert.Equal(e, r!);
    }

    #endregion Primitive Data

    #region Complex Data

    [Theory]
    [MemberData(nameof(GetComplexTestData))]
    public void SetCurrentValueByName_Complex_WithName_OK(Order e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName("Test", e);

        Assert.NotNull(dataContext.Current);
        var a = (JObject)dataContext.Current["Test"]!;
        Assert.Equal(e.InvoiceNumber, a["InvoiceNumber"]);
    }

    [Theory]
    [MemberData(nameof(GetComplexTestData))]
    public void SetCurrentValueByName_Complex_NoName_OK(Order e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName(null, e);

        Assert.NotNull(dataContext.Current);
        var a = (JObject)dataContext.Current;
        Assert.Equal(e.InvoiceNumber, a["InvoiceNumber"]);
    }

    [Theory]
    [MemberData(nameof(GetComplexArrayTestData))]
    public void SetCurrentValueByName_ComplexArray_WithName_OK(ICollection<Order> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName("Test", e);

        Assert.NotNull(dataContext.Current);
        var a = (JArray)dataContext.Current["Test"]!;

        for (int i = 0; i < e.Count; i++)
        {
            Assert.Equal(e.ElementAt(i).InvoiceNumber, a[i]["InvoiceNumber"]);
        }
    }

    [Theory]
    [MemberData(nameof(GetComplexArrayTestData))]
    public void SetCurrentValueByName_ComplexArray_NoName_OK(ICollection<Order> e)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName(null, e);

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
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        Assert.Null(dataContext.Current);

        dataContext.CreateCurrentIfNull();

        Assert.NotNull(dataContext.Current);
    }

    [Fact]
    public void CreateCurrentIfNull_NoReplace_OK()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider)
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
    public void AppendToCurrentValue_Single_OK()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var e = Generator.GenerateOrder();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.AppendToCurrentValue("$.Item.Test", e);

        Assert.NotNull(dataContext.Current);
        var a = (JArray?)dataContext.Current.SelectToken("$.Item.Test");
        Assert.NotNull(a);
        Assert.Equal(e.InvoiceNumber, a[0]["InvoiceNumber"]);
    }
    
    [Fact]
    public void AppendToCurrentValue_Multiple_OK()
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();

        var e1 = Generator.GenerateOrder();
        var e2 = Generator.GenerateOrder();

        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.AppendToCurrentValue("$.Item.Test", e1);
        dataContext.AppendToCurrentValue("$.Item.Test", e2);

        Assert.NotNull(dataContext.Current);
        var a = (JArray?)dataContext.Current.SelectToken("$.Item.Test");
        Assert.NotNull(a);
        Assert.Equal(2, a.Count);
        Assert.Equal(e1.InvoiceNumber, a[0]["InvoiceNumber"]);
        Assert.Equal(e2.InvoiceNumber, a[1]["InvoiceNumber"]);
    }
}