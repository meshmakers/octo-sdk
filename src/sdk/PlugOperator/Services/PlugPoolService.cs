using k8s.Models;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Client.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.Client.PlugControllerServices;
using PlugOperator.Entities;
using PlugOperator.Models;
using PlugOperator.Reconcilers;

namespace PlugOperator.Services;

public class PlugPoolService : IPlugPoolService
{
    private readonly IPlugReconciler _plugReconciler;
    private readonly Dictionary<string, Pool> _pools = new();

    public PlugPoolService(IPlugReconciler plugReconciler)
    {
        _plugReconciler = plugReconciler;
    }
    
    public async Task RegisterPoolAsync(V1PlugPoolEntity entity)
    {
        if (_pools.ContainsKey(entity.Spec.PlugPoolName))
        {
            return;
        }
        
        var plugPoolControllerClient = new PlugPoolControllerClient(new PlugControllerClientOptions
        {
            EndpointUri = entity.Spec.PlugControllerUri,
            TenantId = entity.Spec.TenantId
        }, new ServiceClientAccessToken());

        await plugPoolControllerClient.StartAsync();
        
        var result = await plugPoolControllerClient.RegisterPlugPoolAsync(entity.Spec.PlugPoolName);

        var pool = new Pool(new PoolDescriptor
        {
            Namespace = entity.Namespace(),
            TenantId = entity.Spec.TenantId,
            PoolName = entity.Spec.PlugPoolName,
            PlugControllerUri = entity.Spec.PlugControllerUri,
            BrokerHost = entity.Spec.BrokerHost,
            BrokerVirtualHost = string.IsNullOrWhiteSpace(entity.Spec.BrokerVirtualHost) ? "/" : entity.Spec.BrokerVirtualHost,
            BrokerPort = entity.Spec.BrokerPort,
        }, plugPoolControllerClient, entity);

        _pools.Add(entity.Spec.PlugPoolName, pool);   
        
        foreach (var plugPoolPlugDto in result.Plugs)
        {
            await DeployPlug(pool.PoolDescriptor, plugPoolPlugDto, pool.Entity);
        }
    }

    public async Task UnRegisterPoolAsync(V1PlugPoolEntity entity)
    {
        if (_pools.ContainsKey(entity.Spec.PlugPoolName))
        {
            return;
        }
        
        var pool = _pools[entity.Spec.PlugPoolName];
        await _plugReconciler.DeleteAsync(pool.PoolDescriptor);
    }

    private async Task DeployPlug(PoolDescriptor poolDescriptor, PlugPoolPlugDto poolPlugDto , V1PlugPoolEntity entity)
    {
        await _plugReconciler.ReconcileAsync(poolDescriptor, poolPlugDto, entity);
    }
}