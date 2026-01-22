namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Type of trigger that initiated a pipeline execution
/// </summary>
public enum PipelineTriggerType
{
    /// <summary>
    /// Pipeline was manually triggered
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Pipeline was triggered by a schedule
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Pipeline was triggered by an event
    /// </summary>
    Event = 2,

    /// <summary>
    /// Pipeline was triggered on adapter startup
    /// </summary>
    Startup = 3,
}
