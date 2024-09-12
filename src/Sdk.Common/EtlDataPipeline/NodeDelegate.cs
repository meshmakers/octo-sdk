namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// A function that can process a node in the pipeline
/// </summary>
/// <param name="dataContext">The data context to process</param>
public delegate Task NodeDelegate(IDataContext dataContext);
