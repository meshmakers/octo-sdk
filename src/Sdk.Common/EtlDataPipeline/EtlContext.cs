namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// A context for an ETL process
/// </summary>
public interface IEtlContext
{
    /// <summary>
    /// Gets the tenant id
    /// </summary>
    string TenantId { get; }
    
    /// <summary>
    /// Gets properties that are shared between the different stages of the ETL process and different runs of the
    /// pipeline
    /// </summary>
    IDictionary<string, object?> Properties { get; } 
}