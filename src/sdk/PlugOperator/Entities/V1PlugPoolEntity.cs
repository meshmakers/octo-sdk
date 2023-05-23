using k8s.Models;
using KubeOps.Operator.Entities;

namespace PlugOperator.Entities;

[KubernetesEntity(Group = "octo-mesh.meshmakers.io", ApiVersion = "v1alpha1", Kind = "PlugPool")]
public class V1PlugPoolEntity : CustomKubernetesEntity<V1PlugPoolEntity.V1PlugPoolEntitySpec, V1PlugPoolEntity.V1PlugPoolEntityStatus>
{
    public class V1PlugPoolEntitySpec
    {
        public string PlugPoolName { get; set; } = string.Empty;
        public string PlugControllerUri { get; set; } = string.Empty;
    }

    public class V1PlugPoolEntityStatus
    {
        public string DemoStatus { get; set; } = string.Empty;
    }
}
