using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Common.Plugs;

public record PlugStartup
{
    public string TenantId { get; init; } = null!;
    public PlugConfigurationDto Configuration { get; init; } = null!;
}