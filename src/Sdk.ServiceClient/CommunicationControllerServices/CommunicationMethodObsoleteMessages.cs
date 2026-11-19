namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     AB#9052: messages of the <see cref="System.ObsoleteAttribute" /> on the REST operation methods of
///     <see cref="ICommunicationServicesClient" /> that the CK methods of <c>System.Communication</c> (4.17.0 - 4.19.0)
///     replace. The replacement is the method client a consumer generates from <c>System.Communication-[4.19,5.0)</c>
///     (<c>construction-kits.yaml</c>, together with <c>System-[2.7,3.0)</c>); see
///     <c>docs/method-client-migration-guide.md</c>, "Communication wave 3". The REST methods keep working during their
///     REST window. A workload method is called on the client of the workload's concrete type
///     (<c>RtAdapterMethodClient</c>, <c>RtAdapterPoolMethodClient</c> or <c>RtApplicationMethodClient</c>).
/// </summary>
internal static class CommunicationMethodObsoleteMessages
{
    private const string Suffix =
        " generated from System.Communication-[4.19,5.0) (construction-kits.yaml); the REST method keeps working during its REST window. See docs/method-client-migration-guide.md, Communication wave 3 (AB#9052).";

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.RotateServiceAccountSecretAsync" />.</summary>
    public const string RotateServiceAccountSecret = "Use RtAdapterMethodClient.RotateServiceAccountSecretAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.DeployPipelineAsync" />.</summary>
    public const string DeployPipeline = "Use RtPipelineMethodClient.DeployAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.ExecutePipelineAsync" />.</summary>
    public const string ExecutePipeline = "Use RtPipelineMethodClient.ExecuteAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.SetPipelineDebuggingAsync" />.</summary>
    public const string SetPipelineDebugging = "Use RtPipelineMethodClient.SetDebuggingAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.MovePipelinesToAdapterAsync" />.</summary>
    public const string MovePipelinesToAdapter = "Use RtPipelineMethodClient.MoveToAdapterAsync (once per pipeline)" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.DeployTriggersAsync" />.</summary>
    public const string DeployTriggers = "Use RtPipelineTriggerMethodClient.DeployAllAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.UndeployTriggersAsync" />.</summary>
    public const string UndeployTriggers = "Use RtPipelineTriggerMethodClient.UndeployAllAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.DeployDeploymentSiteAsync" />.</summary>
    public const string DeployDeploymentSite = "Use RtDeploymentSiteMethodClient.DeployAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.UndeployDeploymentSiteAsync" />.</summary>
    public const string UndeployDeploymentSite = "Use RtDeploymentSiteMethodClient.UndeployAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.CancelQueuedExecutionAsync" />.</summary>
    public const string CancelQueuedExecution = "Use RtAdapterPoolMethodClient.CancelQueuedExecutionAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.DeployDataFlowAsync" />.</summary>
    public const string DeployDataFlow = "Use RtDataFlowMethodClient.DeployAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.UndeployDataFlowAsync" />.</summary>
    public const string UndeployDataFlow = "Use RtDataFlowMethodClient.UndeployAsync" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.UpdateWorkloadChartVersionAsync" />.</summary>
    public const string UpdateWorkloadChartVersion =
        "Use SetChartVersionAsync of the workload's method client (RtAdapterMethodClient, RtAdapterPoolMethodClient or RtApplicationMethodClient)" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.DeployWorkloadAsync" />.</summary>
    public const string DeployWorkload =
        "Use DeployAsync of the workload's method client (RtAdapterMethodClient, RtAdapterPoolMethodClient or RtApplicationMethodClient)" + Suffix;

    /// <summary>Replacement of <see cref="ICommunicationServicesClient.UndeployWorkloadAsync" />.</summary>
    public const string UndeployWorkload =
        "Use UndeployAsync of the workload's method client (RtAdapterMethodClient, RtAdapterPoolMethodClient or RtApplicationMethodClient)" + Suffix;
}
