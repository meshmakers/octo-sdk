using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using PlugOperator.Entities;
using PlugOperator.Models;

namespace PlugOperator.Reconcilers;

public interface IPlugReconciler
{
    /// <summary>
    /// Reconciles the orion server resources.
    /// </summary>
    /// <param name="plugPoolPlug"></param>
    /// <param name="entity">Workspace to reconcile the orion server resources for.</param>
    /// <param name="poolDescriptor"></param>
    Task ReconcileAsync(PoolDescriptor poolDescriptor, PlugPoolPlugDto plugPoolPlug, V1PlugPoolEntity entity);
}