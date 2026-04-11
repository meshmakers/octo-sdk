namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Configuration state of an adapter or pool.
/// Values match the CK enum System.Communication/ConfigurationState.
/// </summary>
public enum ConfigurationState
{
    /// <summary>
    /// No configuration has been applied
    /// </summary>
    Unconfigured = 0,

    /// <summary>
    /// Configuration is being applied
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Configuration has been successfully applied
    /// </summary>
    Configured = 2,

    /// <summary>
    /// Configuration failed to apply
    /// </summary>
    Error = 3
}
