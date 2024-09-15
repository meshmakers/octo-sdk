namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a debug message with severity, message and date time
/// </summary>
/// <param name="Severity">Severity of the message</param>
/// <param name="NodePath">Path of node the log message comes from</param>
/// <param name="Message">Message to log</param>
/// <param name="DateTime">Date time of the message</param>
/// <param name="ExceptionMessage">The exception message optionally</param>
public record DebugMessage(LoggerSeverity Severity, string NodePath, string Message, DateTime DateTime, string? ExceptionMessage = null);
