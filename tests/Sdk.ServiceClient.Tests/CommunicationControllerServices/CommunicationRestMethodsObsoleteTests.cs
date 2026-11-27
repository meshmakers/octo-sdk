using System.Reflection;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

namespace Sdk.ServiceClient.Tests.CommunicationControllerServices;

/// <summary>
///     AB#9052 (part 2, after the CLI/MCP switch of AB#9053): the REST operation methods that the CK methods of
///     <c>System.Communication</c> replace are obsolete on the interface and on the client, and the message names the
///     replacement method client. The obsolete methods are warnings, not errors: they keep calling REST during their REST
///     window. The reads, the tenant-level operations and the methods F5.3 did not migrate stay without the attribute.
/// </summary>
public class CommunicationRestMethodsObsoleteTests
{
#pragma warning disable CS0618 // nameof of the obsolete members under test
    [Theory]
    [InlineData(nameof(ICommunicationServicesClient.RotateServiceAccountSecretAsync), "RtAdapterMethodClient.RotateServiceAccountSecretAsync")]
    [InlineData(nameof(ICommunicationServicesClient.DeployPipelineAsync), "RtPipelineMethodClient.DeployAsync")]
    [InlineData(nameof(ICommunicationServicesClient.ExecutePipelineAsync), "RtPipelineMethodClient.ExecuteAsync")]
    [InlineData(nameof(ICommunicationServicesClient.SetPipelineDebuggingAsync), "RtPipelineMethodClient.SetDebuggingAsync")]
    [InlineData(nameof(ICommunicationServicesClient.MovePipelinesToAdapterAsync), "RtPipelineMethodClient.MoveToAdapterAsync")]
    [InlineData(nameof(ICommunicationServicesClient.DeployTriggersAsync), "RtPipelineTriggerMethodClient.DeployAllAsync")]
    [InlineData(nameof(ICommunicationServicesClient.UndeployTriggersAsync), "RtPipelineTriggerMethodClient.UndeployAllAsync")]
    [InlineData(nameof(ICommunicationServicesClient.DeployDeploymentSiteAsync), "RtDeploymentSiteMethodClient.DeployAsync")]
    [InlineData(nameof(ICommunicationServicesClient.UndeployDeploymentSiteAsync), "RtDeploymentSiteMethodClient.UndeployAsync")]
    [InlineData(nameof(ICommunicationServicesClient.CancelQueuedExecutionAsync), "RtAdapterPoolMethodClient.CancelQueuedExecutionAsync")]
    [InlineData(nameof(ICommunicationServicesClient.DeployDataFlowAsync), "RtDataFlowMethodClient.DeployAsync")]
    [InlineData(nameof(ICommunicationServicesClient.UndeployDataFlowAsync), "RtDataFlowMethodClient.UndeployAsync")]
    [InlineData(nameof(ICommunicationServicesClient.UpdateWorkloadChartVersionAsync), "SetChartVersionAsync of the workload's method client")]
    [InlineData(nameof(ICommunicationServicesClient.DeployWorkloadAsync), "DeployAsync of the workload's method client")]
    [InlineData(nameof(ICommunicationServicesClient.UndeployWorkloadAsync), "UndeployAsync of the workload's method client")]
#pragma warning restore CS0618
    public void MigratedRestMethod_IsObsolete_WithTheReplacement(string method, string replacement)
    {
        foreach (var type in new[] { typeof(ICommunicationServicesClient), typeof(CommunicationServicesClient) })
        {
            var obsolete = type.GetMethod(method)!.GetCustomAttribute<ObsoleteAttribute>();

            Assert.NotNull(obsolete);
            Assert.False(obsolete.IsError, "the REST method keeps working during its REST window");
            Assert.StartsWith($"Use {replacement}", obsolete.Message);
            Assert.Contains("generated from System.Communication-[4.19,5.0)", obsolete.Message);
            Assert.Contains("method-client-migration-guide.md", obsolete.Message);
        }
    }

    [Theory]
    [InlineData(typeof(ICommunicationServicesClient))]
    [InlineData(typeof(CommunicationServicesClient))]
    public void OnlyTheMigratedOperations_AreObsolete(Type type)
    {
        // DeclaredOnly: the client's own members, not those of its base class.
        var obsolete = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() != null)
            .Select(m => m.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal([
            "CancelQueuedExecutionAsync", "DeployDataFlowAsync", "DeployDeploymentSiteAsync", "DeployPipelineAsync",
            "DeployTriggersAsync", "DeployWorkloadAsync", "ExecutePipelineAsync", "MovePipelinesToAdapterAsync",
            "RotateServiceAccountSecretAsync", "SetPipelineDebuggingAsync", "UndeployDataFlowAsync",
            "UndeployDeploymentSiteAsync", "UndeployTriggersAsync", "UndeployWorkloadAsync", "UpdateWorkloadChartVersionAsync"
        ], obsolete);
    }
}
