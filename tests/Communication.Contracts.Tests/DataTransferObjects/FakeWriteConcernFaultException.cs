// ReSharper disable once CheckNamespace - the classifier recognises driver exceptions by their namespace
namespace MongoDB.Driver;

/// <summary>
///     AB#8037 test double: stands in for a MongoDB driver exception that carries a write-concern error, without a driver
///     reference in this test project.
/// </summary>
internal sealed class FakeWriteConcernFaultException() : Exception("waiting for replication timed out")
{
    // ReSharper disable once UnusedMember.Global - read by reflection in UnacknowledgedWriteClassifier
    public object WriteConcernError { get; } = new();
}
