namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Communication state of an adapter or pool, indicating its connection to the communication service.
/// Values match the CK enum System.Communication/CommunicationState.
/// </summary>
public enum CommunicationState
{
    /// <summary>
    /// Adapter has not registered with the communication service
    /// </summary>
    Unregistered = 0,

    /// <summary>
    /// Adapter is connected and operational
    /// </summary>
    Online = 1,

    /// <summary>
    /// Adapter is disconnected from the communication service
    /// </summary>
    Offline = 2
}
