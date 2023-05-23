namespace Meshmakers.Octo.Sdk.PlugExecutor;

public class PlugOptions
{
    public PlugOptions()
    {
        TenantId = "meshTest";
        PlugControllerServicesUri = "https://localhost:5015";
    }
    
    public string? PlugId { get; set; }
    
    public string? TenantId { get; set; }

    public string? PlugControllerServicesUri { get; set; }
}