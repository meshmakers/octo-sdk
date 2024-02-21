namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Represents the default implementation of the <see cref="IEtlContext"/> interface.
/// </summary>
public class DefaultEtlContext : IEtlContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="DefaultEtlContext"/> class.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="properties">properties that are shared between the different stages of the ETL process and different runs of the pipeline</param>
    public DefaultEtlContext(string tenantId, IDictionary<string, object?> properties)
    {
        TenantId = tenantId;
        Properties = properties;
    }

    /// <inheritdoc />
    public string TenantId { get; }
    
    
    /// <inheritdoc />
    public IDictionary<string, object?> Properties { get; } 
}