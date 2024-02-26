namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// A function that can process a node in the pipeline
/// </summary>
public delegate Task NodeDelegate(IDataContext dataContext);
