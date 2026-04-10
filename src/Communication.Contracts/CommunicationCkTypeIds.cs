namespace Meshmakers.Octo.Communication.Contracts;

/// <summary>
///     Well-known CK type identifiers for System.Communication entities.
/// </summary>
public static class CommunicationCkTypeIds
{
    /// <summary>System.Communication/Adapter</summary>
    public const string Adapter = "System.Communication/Adapter";

    /// <summary>System.Communication/Pipeline</summary>
    public const string Pipeline = "System.Communication/Pipeline";

    /// <summary>System.Communication/Pool</summary>
    public const string Pool = "System.Communication/Pool";

    /// <summary>System.Communication/DataFlow</summary>
    public const string DataFlow = "System.Communication/DataFlow";

    /// <summary>System.Communication/PipelineTrigger</summary>
    public const string PipelineTrigger = "System.Communication/PipelineTrigger";

    /// <summary>
    ///     Constructs a composite RtEntityId string from a CK type ID and a runtime object ID.
    /// </summary>
    public static string ToCompositeId(string ckTypeId, string rtId) => $"{ckTypeId}@{rtId}";
}
