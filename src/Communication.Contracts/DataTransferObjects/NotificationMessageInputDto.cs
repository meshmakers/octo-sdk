using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a notification message for input
/// </summary>
public class NotificationMessageInputDto
{
    /// <summary>
    /// Returns the subject text.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SubjectText { get; set; }

    /// <summary>
    /// Returns the body text.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? BodyText { get; set; }

    /// <summary>
    /// Returns the recipient address.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? RecipientAddress { get; set; }

    /// <summary>
    /// Returns the notification type.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NotificationTypesDto? NotificationType { get; set; }

    /// <summary>
    /// Returns the sent date time.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? SentDateTime { get; set; }

    /// <summary>
    /// Returns the send status.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SendStatusDto? SendStatus { get; set; }

    /// <summary>
    /// Returns the date time of the last try to send.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? LastTryDateTime { get; set; }

    /// <summary>
    /// Returns an optional error text if the send status is <see cref="SendStatusDto.Error"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ErrorText { get; set; }

    /// <summary>
    /// Defines optionally the related entities.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public RtAssociationInputDto[]? RelatesTo { get; set; }
}