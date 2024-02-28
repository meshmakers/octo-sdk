using Meshmakers.Octo.Sdk.Common.Adapters;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;


internal interface IEtlContextAccessor
{
    Func<IEtlContext>? EtlContextFactory { get; set; } 
    Func<IAdapterEtlContext>? AdapterEtlContextFactory { get; set; }
    IEtlContext GetEtlContext();
    IAdapterEtlContext GetAdapterEtlContext();
}

internal interface IEtlRetrieverContextAccessor<TContext> where TContext : class, IEtlContext
{
    Func<TContext>? EtlContextFactory { get; set; } 
    TContext GetEtlContext();
}

internal class EtlRetrieverContextAccessor<TContext> : IEtlRetrieverContextAccessor<TContext> where TContext : class, IEtlContext
{
    public Func<TContext>? EtlContextFactory { get; set; } 
    
    public TContext GetEtlContext()
    {
        if(EtlContextFactory == null)
        {
            throw new InvalidOperationException("EtlContextFactory is not set");
        }

        return EtlContextFactory();
    }
}


/// <summary>
/// This class is used to access the ETL Context for each pipeline run in a scoped way.
/// The idea is, that this acts as a bridge to factory functions, that are used to create the ETL context.
/// </summary>
internal class EtlContextAccessor : IEtlContextAccessor
{
    public Func<IEtlContext>? EtlContextFactory { get; set; } 
    
    public Func<IAdapterEtlContext>? AdapterEtlContextFactory { get; set; }
    
    
    public IEtlContext GetEtlContext()
    {
        if(EtlContextFactory == null)
        {
            throw new InvalidOperationException("EtlContextFactory is not set");
        }

        return EtlContextFactory();
    }

    public IAdapterEtlContext GetAdapterEtlContext()
    {
        if(AdapterEtlContextFactory == null)
        {
            throw new InvalidOperationException("AdapterEtlContextFactory is not set");
        }

        return AdapterEtlContextFactory();
    }
}