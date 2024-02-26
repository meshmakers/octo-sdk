using System.Collections;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class DataContextTests(ServiceCollectionFixture fixture) : IClassFixture<ServiceCollectionFixture>
{
    public class TestDataGenerator : IEnumerable<object[]>
    {
        private readonly List<object[]> _data =
        [
            [new[]{"test", "abc"}],
            [new[]{"demo", "def"}],
        ];

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    
    [Theory]
    [InlineData("TestValue")]
    [InlineData(5)]
    [InlineData(5.67d)]
    [InlineData(5.67f)]
    [InlineData(true)]
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
    [InlineData("TestValue")]
    [InlineData(5)]
    [InlineData(5.67d)]
    [InlineData(5.67f)]
    [InlineData(true)]
    public void SetCurrentValueByName_Primitive_Null_OK(object o)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();
        
        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName(null, o);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(o, ((JValue)dataContext.Current).Value);
    }
    
    [Theory]
    [ClassData(typeof(TestDataGenerator))]
    public void SetCurrentValueByName_ClassData_WithName_Array_OK(object o)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();
        
        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName("Test", o);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(o, ((JArray)dataContext.Current["Test"]!));
    }
    
    [Theory]
    [ClassData(typeof(TestDataGenerator))]
    public void SetCurrentValueByName_ClassData_Null_Array_OK(object o)
    {
        var globalServiceProvider = fixture.Services.BuildServiceProvider();
        var pipelineServiceProvider = fixture.PipelineServices.BuildServiceProvider();
        
        var dataContext = new DataContext(globalServiceProvider, pipelineServiceProvider);
        dataContext.SetCurrentValueByName(null, o);

        Assert.NotNull(dataContext.Current);
        Assert.Equal(o, ((JArray)dataContext.Current).Values<string>());
    }
}