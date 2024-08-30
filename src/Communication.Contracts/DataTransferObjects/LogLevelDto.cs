namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Defines the log level for diagnostic logging.
/// </summary>
public enum LogLevelDto
{
    /// <summary>
    /// Most verbose level. Used for development and seldom enabled in production.
    /// </summary>
    Trace = 0,
    
    /// <summary>
    /// Debugging the application behavior from internal events of interest.
    /// </summary>
    Debug = 1,
    
    /// <summary>
    /// Information that highlights progress or application lifetime events.
    /// </summary>
    Info = 2,
    
    /// <summary>
    /// Warnings about validation issues or temporary failures that can be recovered.
    /// </summary>
    Warn = 3,
    
    /// <summary>
    /// Errors where functionality has failed or Exception have been caught.
    /// </summary>
    Error = 4,
    
    /// <summary>
    /// Most critical level. Application is about to abort.
    /// </summary>
    Fatal = 5,
    
    /// <summary>
    /// Off log level, no logging is performed.
    /// </summary>
    Off = 6
}