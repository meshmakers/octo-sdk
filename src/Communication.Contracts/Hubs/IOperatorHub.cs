namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
/// Server-side hub interface for operator management connections.
/// Used by the Communication Operator to register for tenant lifecycle notifications.
/// </summary>
public interface IOperatorHub
{
    /// <summary>
    /// Registers the operator for receiving tenant lifecycle events.
    /// </summary>
    /// <returns>List of tenant IDs that currently have communication enabled</returns>
    Task<IEnumerable<string>> RegisterOperatorAsync();

    /// <summary>
    /// Unregisters the operator from receiving tenant lifecycle events.
    /// </summary>
    Task UnregisterOperatorAsync();
}
