using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

namespace Sdk.Common.IntegrationTests.Fixtures;

/// <summary>
/// Test implementation of IGlobalConfiguration for integration tests.
/// </summary>
internal class TestGlobalConfiguration : IGlobalConfiguration
{
    private readonly Dictionary<string, object> _configuration = new();

    public void SetValue<T>(string configurationName, T value) where T : class
    {
        _configuration[configurationName.ToLower()] = value;
    }

    public IEnumerable<string> GetNames()
    {
        return _configuration.Keys;
    }

    public bool IsDefined(string configurationName)
    {
        return _configuration.ContainsKey(configurationName.ToLower());
    }

    public T GetValue<T>(string configurationName) where T : class
    {
        if (_configuration.TryGetValue(configurationName.ToLower(), out var value))
        {
            return (T)value;
        }

        throw new InvalidOperationException($"Configuration parameter '{configurationName}' not found.");
    }
}
