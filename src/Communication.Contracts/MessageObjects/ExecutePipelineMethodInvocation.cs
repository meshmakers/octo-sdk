namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
///     Marker that tells a pipeline executor the run was started by the invocation of a Construction
///     Kit method (AB#7458, Feature AB#6884 F3.7), so nodes that guard against creating entities of a
///     type whose method <c>replacesGenericCreate</c> can let the replacing method's own pipeline run
///     through.
/// </summary>
/// <remarks>
///     <para>
///         🔴 <b>Set only by the communication controller's method invoker, never by clients.</b> A
///         caller-supplied marker would let any client lift the generic-create guard for a type; the
///         controller overwrites or clears the member on every request it did not build itself.
///         Nothing in this record is a credential and none of it is a proof — consumers must not treat
///         it as one (no signing of bus messages is in scope).
///     </para>
///     <para>
///         🟢 <b>Additive &amp; back-compatible.</b> The property carrying this record on
///         <see cref="ExecutePipelineRequest" /> and
///         <see cref="DataTransferObjects.LeaseDto" /> is nullable: a payload of an older peer simply
///         has no marker, and an older peer reading a newer payload ignores the unknown member.
///     </para>
/// </remarks>
public record ExecutePipelineMethodInvocation
{
    /// <summary>
    ///     Qualified id of the invoked CK method (model, owning element and method name as the
    ///     controller resolved it), for example <c>"Model-1/Type-1/Method"</c>.
    /// </summary>
    public string MethodId { get; init; } = string.Empty;

    /// <summary>
    ///     Unique id of this invocation. Lets an executor correlate the run with the invocation that
    ///     started it and ignore a marker that is replayed for a different run.
    /// </summary>
    public Guid InvocationId { get; init; }

    /// <summary>
    ///     Qualified CK type id the method was invoked on (the type whose generic create the method may
    ///     replace).
    /// </summary>
    public string CkTypeId { get; init; } = string.Empty;

    /// <summary>
    ///     Runtime id (ObjectId string) of the pipeline the method resolved to and that this request
    ///     executes.
    /// </summary>
    public string PipelineRtId { get; init; } = string.Empty;
}
