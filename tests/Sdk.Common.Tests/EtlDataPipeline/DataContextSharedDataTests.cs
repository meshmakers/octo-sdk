using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class DataContextSharedDataTests
{
    [Fact]
    public void GetSimpleValueByPath_SharedPath_ReturnsSharedValue()
    {
        var sharedDoc = new JObject
        {
            ["body"] = new JObject { ["name"] = "TestName" },
            ["status"] = "active"
        };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx = new DataContext(parent, new JObject(), sharedData);

        var result = ctx.GetSimpleValueByPath<string>("$.full.body.name");
        Assert.Equal("TestName", result);

        var statusResult = ctx.GetSimpleValueByPath<string>("$.full.status");
        Assert.Equal("active", statusResult);
    }

    [Fact]
    public void GetSimpleValueByPath_NormalPath_ReturnsFromCurrent()
    {
        var sharedData = new Dictionary<string, JToken>
        {
            ["full"] = new JObject { ["x"] = 1 }
        };
        var parent = new DataContext();
        var input = new JObject { ["key"] = "myValue" };

        var ctx = new DataContext(parent, input, sharedData);

        var result = ctx.GetSimpleValueByPath<string>("$.key");
        Assert.Equal("myValue", result);
    }

    [Fact]
    public void GetComplexObjectByPath_SharedPath_ReturnsSharedObject()
    {
        var sharedDoc = new JObject
        {
            ["body"] = new JObject { ["id"] = 42, ["label"] = "test" }
        };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx = new DataContext(parent, new JObject(), sharedData);

        var result = ctx.GetComplexObjectByPath<JObject>("$.full.body");
        Assert.NotNull(result);
        Assert.Equal(42, result["id"]?.Value<int>());
        Assert.Equal("test", result["label"]?.Value<string>());
    }

    [Fact]
    public void GetComplexObjectByPath_SharedPathDirect_ReturnsEntireSharedToken()
    {
        var sharedDoc = new JObject { ["prop"] = "value" };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx = new DataContext(parent, new JObject(), sharedData);

        var result = ctx.GetComplexObjectByPath<JToken>("$.full");
        Assert.NotNull(result);
        Assert.Equal("value", result["prop"]?.Value<string>());
    }

    [Fact]
    public void GetComplexObjectByPath_RootPath_MergesSharedData()
    {
        var sharedDoc = new JObject { ["nested"] = "sharedValue" };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();
        var input = new JObject { ["key"] = "localValue" };

        var ctx = new DataContext(parent, input, sharedData);

        var result = ctx.GetComplexObjectByPath<JToken>("$");
        Assert.NotNull(result);
        Assert.Equal("localValue", result["key"]?.Value<string>());
        Assert.NotNull(result["full"]);
        Assert.Equal("sharedValue", result["full"]?["nested"]?.Value<string>());
    }

    [Fact]
    public void GetComplexObjectByPath_RootPathNull_MergesSharedData()
    {
        var sharedDoc = new JObject { ["data"] = 123 };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();
        var input = new JObject { ["key"] = "val" };

        var ctx = new DataContext(parent, input, sharedData);

        var result = ctx.GetComplexObjectByPath<JToken>(null);
        Assert.NotNull(result);
        Assert.Equal("val", result["key"]?.Value<string>());
        Assert.Equal(123, result["full"]?["data"]?.Value<int>());
    }

    [Fact]
    public void SetValueByPath_SharedPath_CopyOnWrite_OriginalUnchanged()
    {
        var sharedDoc = new JObject { ["name"] = "original" };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx = new DataContext(parent, new JObject(), sharedData);

        // Write to shared path triggers copy-on-write
        ctx.SetValueByPath("$.full.name", DocumentModes.Extend, ValueKinds.Simple,
            TargetValueWriteModes.Overwrite, "modified");

        // Context sees the modified value
        var result = ctx.GetSimpleValueByPath<string>("$.full.name");
        Assert.Equal("modified", result);

        // Original shared data is unchanged
        Assert.Equal("original", sharedDoc["name"]?.Value<string>());
    }

    [Fact]
    public void CreateChildDataContext_PropagatesSharedData()
    {
        var sharedDoc = new JObject { ["data"] = "shared" };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx = new DataContext(parent, new JObject(), sharedData);
        var child = ctx.CreateChildDataContext(new JObject { ["childKey"] = "childVal" });

        // Child can read shared data
        var sharedResult = child.GetSimpleValueByPath<string>("$.full.data");
        Assert.Equal("shared", sharedResult);

        // Child has its own Current
        var childResult = child.GetSimpleValueByPath<string>("$.childKey");
        Assert.Equal("childVal", childResult);
    }

    [Fact]
    public void CreateChildDataContext_NestedPropagation()
    {
        var sharedDoc = new JObject { ["level"] = "root-shared" };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx = new DataContext(parent, new JObject(), sharedData);
        var child = ctx.CreateChildDataContext(new JObject());
        var grandchild = child.CreateChildDataContext(new JObject());

        var result = grandchild.GetSimpleValueByPath<string>("$.full.level");
        Assert.Equal("root-shared", result);
    }

    [Fact]
    public void IsPathSimpleArrayValue_SharedPath_Works()
    {
        var sharedDoc = new JObject { ["items"] = new JArray(1, 2, 3) };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx = new DataContext(parent, new JObject(), sharedData);

        Assert.True(ctx.IsPathSimpleArrayValue("$.full.items"));
        Assert.False(ctx.IsPathSimpleArrayValue("$.full"));
    }

    [Fact]
    public void GetSimpleArrayValueByPath_SharedPath_Works()
    {
        var sharedDoc = new JObject { ["items"] = new JArray(10, 20, 30) };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx = new DataContext(parent, new JObject(), sharedData);

        var result = ctx.GetSimpleArrayValueByPath<int>("$.full.items")?.ToList();
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(10, result[0]);
        Assert.Equal(30, result[2]);
    }

    [Fact]
    public void SelectByPath_SharedPath_Works()
    {
        var sharedDoc = new JObject
        {
            ["items"] = new JArray(
                new JObject { ["id"] = 1 },
                new JObject { ["id"] = 2 })
        };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx = new DataContext(parent, new JObject(), sharedData);

        var results = ctx.SelectByPath("$.full.items[*].id").ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Value<int>());
        Assert.Equal(2, results[1].Value<int>());
    }

    [Fact]
    public void CopyOnWrite_SecondInstance_IndependentFromFirst()
    {
        var sharedDoc = new JObject { ["counter"] = 0 };
        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var ctx1 = new DataContext(parent, new JObject(), sharedData);
        var ctx2 = new DataContext(parent, new JObject(), sharedData);

        // ctx1 writes to shared path
        ctx1.SetValueByPath("$.full.counter", DocumentModes.Extend, ValueKinds.Simple,
            TargetValueWriteModes.Overwrite, 42);

        // ctx2 still sees original shared value
        var result2 = ctx2.GetSimpleValueByPath<int>("$.full.counter");
        Assert.Equal(0, result2);

        // ctx1 sees modified value
        var result1 = ctx1.GetSimpleValueByPath<int>("$.full.counter");
        Assert.Equal(42, result1);

        // Original unchanged
        Assert.Equal(0, sharedDoc["counter"]?.Value<int>());
    }

    [Fact]
    public void ParallelReads_OnSharedData_ThreadSafe()
    {
        var sharedDoc = new JObject();
        for (int i = 0; i < 100; i++)
        {
            sharedDoc[$"prop{i}"] = $"value{i}";
        }

        var sharedData = new Dictionary<string, JToken> { ["full"] = sharedDoc };
        var parent = new DataContext();

        var exceptions = new List<Exception>();
        Parallel.For(0, 100, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            try
            {
                var ctx = new DataContext(parent, new JObject(), sharedData);
                var result = ctx.GetSimpleValueByPath<string>($"$.full.prop{i}");
                Assert.Equal($"value{i}", result);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void NoSharedData_BehavesNormally()
    {
        var ctx = new DataContext();
        ctx.SetValueByPath("$.test", DocumentModes.Extend, ValueKinds.Simple,
            TargetValueWriteModes.Overwrite, "hello");

        var result = ctx.GetSimpleValueByPath<string>("$.test");
        Assert.Equal("hello", result);
    }
}
