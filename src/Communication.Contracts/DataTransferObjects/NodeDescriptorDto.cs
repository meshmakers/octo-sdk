namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Data transfer object describing a pipeline node type with its configuration schema.
/// Sent from adapters to the communication controller during registration.
/// </summary>
/// <param name="NodeName">The name of the node (e.g. "Select")</param>
/// <param name="Version">The version of the node</param>
/// <param name="Category">The category (e.g. "Trigger", "Transform", "Control")</param>
/// <param name="IsTrigger">Whether this node is a trigger node</param>
/// <param name="SupportsChildren">Whether this node supports child transformations</param>
/// <param name="ConfigurationSchemaJson">JSON Schema string describing the configuration</param>
/// <param name="IsDeprecated">Whether this node is deprecated</param>
/// <param name="DeprecationMessage">Optional reason or migration hint when the node is deprecated</param>
/// <param name="RequiresRunningProcess">
///     Whether this trigger node only works while the adapter process is running (e.g. in-process
///     polling or event subscriptions). Workloads with pipelines using such triggers are not
///     on-demand capable — hibernating them would silently stop the trigger (AB#4984).
/// </param>
/// <param name="ExecutionClass">
///     The scheduling class this trigger implies when the pipeline runs on a leased adapter pool
///     (AB#4924): <c>0</c> Interactive — work a human is waiting for (an HTTP request, a manual
///     run) — or <c>1</c> Batch, everything scheduled or event-driven. Within one tenant's turn in
///     the round-robin rotation, Interactive is served before Batch; across tenants the class has no
///     effect at all, so it cannot reintroduce the starvation round-robin exists to prevent.
///     <para>
///         Defaults to Batch, which is the conservative answer: a trigger that declares nothing can
///         never jump a queue. An <c>int</c> rather than an enum deliberately — this DTO is the wire
///         contract between an adapter and the controller, and a value the receiving side does not
///         know must deserialize rather than throw.
///     </para>
/// </param>
public record NodeDescriptorDto(
    string NodeName,
    int Version,
    string Category,
    bool IsTrigger,
    bool SupportsChildren,
    string ConfigurationSchemaJson,
    bool IsDeprecated = false,
    string? DeprecationMessage = null,
    bool RequiresRunningProcess = false,
    int ExecutionClass = 1);
