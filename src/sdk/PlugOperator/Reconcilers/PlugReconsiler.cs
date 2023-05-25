using System.Collections.ObjectModel;
using k8s;
using k8s.Models;
using KubeOps.Operator.Entities.Extensions;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using PlugOperator.Common;
using PlugOperator.Entities;
using PlugOperator.Models;

namespace PlugOperator.Reconcilers;

public class PlugReconciler : IPlugReconciler
{
    private readonly IKubernetes _kubernetes;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PlugReconciler"/>
    /// </summary>
    /// <param name="kubernetes">Kubernetes client to use</param>
    /// <param name="logger">Logger to write log message to</param>
    public PlugReconciler(IKubernetes kubernetes, ILogger<PlugReconciler> logger)
    {
        _logger = logger;
        _kubernetes = kubernetes;
    }

    /// <summary>
    /// Reconciles the plugs for the plug pool resource.
    /// </summary>
    /// <param name="poolDescriptor">Meta data about the pool</param>
    /// <param name="plugPoolPlug">The pool plug to reconcile</param>
    /// <param name="entity">Plug pool entity for reconcile</param>
    public async Task ReconcileAsync(PoolDescriptor poolDescriptor, PlugPoolPlugDto plugPoolPlug, V1PlugPoolEntity entity)
    {
        await ReconcilePlugDeploymentAsync(poolDescriptor, plugPoolPlug, entity);
        await ReconcilePlugServiceAsync(poolDescriptor, plugPoolPlug, entity);
    }
    
    /// <summary>
    /// Deletes the plugs for the plug pool resource.
    /// </summary>
    /// <param name="poolDescriptor">Meta data about the pool</param>
    public async Task DeleteAsync(PoolDescriptor poolDescriptor)
    {
        await DeletePlugDeploymentAsync(poolDescriptor);
        await DeletePlugServiceAsync(poolDescriptor);
    }

    private async Task DeletePlugDeploymentAsync(PoolDescriptor poolDescriptor)
    {
        var deploymentLabels = new Dictionary<string, string>
        {
            ["octo-mesh.meshmakers.io/component"] = "octo-mesh-plug",
            ["octo-mesh.meshmakers.io/plug-pool"] = poolDescriptor.PoolName,
            ["octo-mesh.meshmakers.io/tenant"] = poolDescriptor.TenantId
        };
        
        var existingDeployments = await _kubernetes.ListNamespacedDeploymentAsync(
            poolDescriptor.Namespace, labelSelector: deploymentLabels.AsLabelSelector());

        foreach (var existingDeployment in existingDeployments.Items)
        {
            _logger.DeletingDeployment(existingDeployment.Metadata.Name, poolDescriptor.PoolName, poolDescriptor.Namespace);
            await _kubernetes.DeleteNamespacedDeploymentAsync(existingDeployment.Metadata.Name, poolDescriptor.Namespace);
        }
    }

    private async Task DeletePlugServiceAsync(PoolDescriptor poolDescriptor)
    {
        var serviceLabels = new Dictionary<string, string>
        {
            ["octo-mesh.meshmakers.io/component"] = "octo-mesh-plug",
            ["octo-mesh.meshmakers.io/plug-pool"] = poolDescriptor.PoolName,
            ["octo-mesh.meshmakers.io/tenant"] = poolDescriptor.TenantId
        };
        
        var existingServices = await _kubernetes.ListNamespacedServiceAsync(
            poolDescriptor.Namespace, labelSelector: serviceLabels.AsLabelSelector());

        foreach (var existingService in existingServices.Items)
        {
            _logger.DeletingService(existingService.Metadata.Name, poolDescriptor.PoolName, poolDescriptor.Namespace);
            await _kubernetes.DeleteNamespacedServiceAsync(existingService.Metadata.Name, poolDescriptor.Namespace);
        }
    }

    private async Task ReconcilePlugDeploymentAsync(PoolDescriptor poolDescriptor, PlugPoolPlugDto plugPoolPlug, V1PlugPoolEntity entity)
    {
        var deploymentLabels = new Dictionary<string, string>
        {
            ["octo-mesh.meshmakers.io/component"] = "octo-mesh-plug",
            ["octo-mesh.meshmakers.io/plug"] = plugPoolPlug.PlugId.ToString(),
            ["octo-mesh.meshmakers.io/plug-pool"] = poolDescriptor.PoolName,
            ["octo-mesh.meshmakers.io/tenant"] = poolDescriptor.TenantId
        };

        var deploymentName = $"{poolDescriptor.TenantId}-{plugPoolPlug.PlugId.ToString()}-octo-mesh-plug";
        var secretName = $"{poolDescriptor.TenantId}-{poolDescriptor.PoolName}-octo-mesh-connection";

        var existingDeployments = await _kubernetes.ListNamespacedDeploymentAsync(
            poolDescriptor.Namespace, labelSelector: deploymentLabels.AsLabelSelector());

        if (existingDeployments.Items.Count == 0)
        {
            _logger.CreatingDeployment(deploymentName, poolDescriptor.PoolName, poolDescriptor.Namespace);

            var deploymentImageName = plugPoolPlug.ImageName + ":" + plugPoolPlug.Version;

            var deployment = new V1Deployment
            {
                Metadata = new V1ObjectMeta
                {
                    Name = deploymentName,
                    Labels = deploymentLabels
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = deploymentLabels
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = deploymentLabels,
                            Name = deploymentName
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new Collection<V1Container>
                            {
                                new()
                                {
                                    Name = deploymentName,
                                    Image = deploymentImageName,
                                   // Command = new Collection<string> { "prefect", "orion", "start" },
                                    Env = new Collection<V1EnvVar>
                                    {
      
                                        new()
                                        {
                                            Name = "OCTO_PLUG__TENANTID",
                                            Value = poolDescriptor.TenantId
                                        },
                                        new()
                                        {
                                            Name = "OCTO_PLUG__PLUGCONTROLLERSERVICESURI",
                                            Value = poolDescriptor.PlugControllerUri
                                        },
                                        new()
                                        {
                                            Name = "OCTO_PLUG__PLUGID",
                                            Value = plugPoolPlug.PlugId.ToString()
                                        },
                                        new()
                                        {
                                            Name = "OCTO_PLUG__BROKERHOST",
                                            Value = poolDescriptor.BrokerHost
                                        },
                                        new()
                                        {
                                            Name = "OCTO_PLUG__BROKERVIRTUALHOST",
                                            Value = poolDescriptor.BrokerVirtualHost
                                        },
                                        new()
                                        {
                                            Name = "OCTO_PLUG__BROKERPORT",
                                            Value = poolDescriptor.BrokerPort.ToString()
                                        },
                                        new()
                                        {
                                            Name = "OCTO_PLUG_BROKERUSERNAME",
                                            ValueFrom = new V1EnvVarSource
                                            {
                                                SecretKeyRef = new V1SecretKeySelector
                                                {
                                                    Name = secretName,
                                                    Key = "brokerusername"
                                                }
                                            }
                                        },
                                        new()
                                        {
                                            Name = "OCTO_PLUG_BROKERPASSWORD",
                                            ValueFrom = new V1EnvVarSource
                                            {
                                                SecretKeyRef = new V1SecretKeySelector
                                                {
                                                    Name = secretName,
                                                    Key = "brokerpassword"
                                                }
                                            }
                                        },
                                    },
                                    Ports = new Collection<V1ContainerPort>
                                    {
                                        new(containerPort: 4200, name: "http-orion")
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<string, ResourceQuantity>
                                        {
                                            ["cpu"] = new("200m"),
                                            ["memory"] = new("512Mi")
                                        },
                                        Limits = new Dictionary<string, ResourceQuantity>
                                        {
                                            ["cpu"] = new("500m"),
                                            ["memory"] = new("1Gi")
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            await _kubernetes.CreateNamespacedDeploymentAsync(
                deployment.WithOwnerReference(entity), poolDescriptor.Namespace);
        }
    }

    private async Task ReconcilePlugServiceAsync(PoolDescriptor poolDescriptor, PlugPoolPlugDto plugPoolPlug, V1PlugPoolEntity entity)
    {
        var serviceLabels = new Dictionary<string, string>
        {
            ["octo-mesh.meshmakers.io/component"] = "octo-mesh-plug",
            ["octo-mesh.meshmakers.io/plug"] = plugPoolPlug.PlugId.ToString(),
            ["octo-mesh.meshmakers.io/plug-pool"] = poolDescriptor.PoolName,
            ["octo-mesh.meshmakers.io/tenant"] = poolDescriptor.TenantId
        };

        var serviceName = $"{poolDescriptor.TenantId}-{plugPoolPlug.PlugId}-octo-mesh-plug";

        var existingServices = await _kubernetes.ListNamespacedServiceAsync(
            entity.Namespace(), labelSelector: serviceLabels.AsLabelSelector());

        if (existingServices.Items.Count == 0)
        {
            _logger.CreatingService(serviceName, entity.Name(), entity.Namespace());

            var service = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = serviceName,
                    Labels = serviceLabels,
                },
                Spec = new V1ServiceSpec
                {
                    Type = "ClusterIP",
                    Selector = serviceLabels,
                    Ports = new Collection<V1ServicePort>
                    {
                        new()
                        {
                            Name = "http-orion",
                            Port = 4200,
                            Protocol = "TCP",
                            TargetPort = 4200
                        }
                    }
                }
            };

            await _kubernetes.CreateNamespacedServiceAsync(
                service.WithOwnerReference(entity),
                entity.Namespace());
        }
    }
}