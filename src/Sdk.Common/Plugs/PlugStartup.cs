using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Common.Plugs;

public record PlugStartup
{
    public PlugStartup(string tenantId, PlugConfigurationDto configuration)
    {
        TenantId = tenantId;
        Configuration = configuration;
    }

    public string TenantId { get; }
    public PlugConfigurationDto Configuration { get; }
}