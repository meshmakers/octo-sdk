namespace PlugOperator.Models;

public class PoolDescriptor
{
    public string Namespace { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string PoolName { get; set; } = string.Empty;
    public string PlugControllerUri { get; set; } = string.Empty;
    public string BrokerHost { get; set; } = string.Empty;
    public string BrokerVirtualHost { get; set; } = string.Empty;
    public int BrokerPort { get; set; } = 5672;
}