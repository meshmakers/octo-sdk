using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using PlugOperator.Entities;
using PlugOperator.Finalizer;

namespace PlugOperator.Controller;

[EntityRbac(typeof(V1PlugPoolEntity), Verbs = RbacVerb.All)]
public class DemoController : IResourceController<V1PlugPoolEntity>
{
    private readonly ILogger<DemoController> _logger;
    private readonly IFinalizerManager<V1PlugPoolEntity> _finalizerManager;

    public DemoController(ILogger<DemoController> logger, IFinalizerManager<V1PlugPoolEntity> finalizerManager)
    {
        _logger = logger;
        _finalizerManager = finalizerManager;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(V1PlugPoolEntity entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(ReconcileAsync)}.");
        await _finalizerManager.RegisterFinalizerAsync<DemoFinalizer>(entity);

        return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15));
    }

    public Task StatusModifiedAsync(V1PlugPoolEntity entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(StatusModifiedAsync)}.");

        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1PlugPoolEntity entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(DeletedAsync)}.");

        return Task.CompletedTask;
    }
}
