using GraphQL;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Services;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Runtime.Contracts;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
///     Implements the notification repository using the tenant client.
/// </summary>
// ReSharper disable once UnusedType.Global
public class WsNotificationRepository : INotificationRepository
{
    private readonly ITenantClient _tenantClient;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="tenantClient">The tenant client</param>
    public WsNotificationRepository(ITenantClient tenantClient)
    {
        _tenantClient = tenantClient;
    }

    /// <inheritdoc />
    public async Task AddShortMessageAsync(string tenantId, string toPhoneNumber, string message)
    {
        await AddShortMessageAsync(tenantId, toPhoneNumber, message, null);
    }

    /// <inheritdoc />
    public async Task AddEMailMessageAsync(string tenantId, string emailAddress, string subject,
        string? htmlMessage)
    {
        await AddEMailMessageAsync(tenantId, emailAddress, subject, htmlMessage, null);
    }

    /// <inheritdoc />
    public async Task AddShortMessageAsync(string tenantId, string toPhoneNumber,
        string message, RtEntityId? associatedRtId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(toPhoneNumber), toPhoneNumber);
        ArgumentValidation.ValidateString(nameof(message), message);

        try
        {
            var notificationMessage = new NotificationMessageDto
            {
                SendStatus = SendStatusDto.Pending,
                BodyText = message,
                RecipientAddress = toPhoneNumber,
                NotificationType = NotificationTypesDto.Sms
            };

            await AddMessageAsync(notificationMessage, associatedRtId);
        }
        catch (Exception e)
        {
            throw new NotificationSendFailedException("Message send failed.", e);
        }
    }

    /// <inheritdoc />
    public async Task AddEMailMessageAsync(string tenantId, string emailAddress, string? subject,
        string? htmlMessage, RtEntityId? associatedRtId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);
        ArgumentValidation.ValidateString(nameof(emailAddress), emailAddress);

        try
        {
            var notificationMessage = new NotificationMessageDto
            {
                SendStatus = SendStatusDto.Pending,
                SubjectText = subject,
                BodyText = htmlMessage,
                RecipientAddress = emailAddress,
                NotificationType = NotificationTypesDto.EMail
            };

            await AddMessageAsync(notificationMessage, associatedRtId);
        }
        catch (Exception e)
        {
            throw new NotificationSendFailedException("Message send failed.", e);
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<NotificationMessageDto>> GetPendingMessagesAsync(string tenantId,
        NotificationTypesDto notificationType, int? take = null)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var getQuery = new GraphQLRequest
        {
            Query = GraphQl.GetNotificationMessages,
            Variables = new
            {
                fieldFilters = new[]
                {
                    new FieldFilterDto
                    {
                        AttributeName = nameof(NotificationMessageDto.SendStatus),
                        Operator = FieldFilterOperatorDto.Equals,
                        ComparisonValue = (int)SendStatusDto.Pending
                    },
                    new FieldFilterDto
                    {
                        AttributeName = nameof(NotificationMessageDto.LastTryDateTime),
                        Operator = FieldFilterOperatorDto.LessEqualThan,
                        ComparisonValue = DateTime.UtcNow.AddMinutes(-5)
                    },
                    new FieldFilterDto
                    {
                        AttributeName = nameof(NotificationMessageDto.NotificationType),
                        Operator = FieldFilterOperatorDto.Equals,
                        ComparisonValue = (int)notificationType
                    }
                },
                first = take
            }
        };

        var result = await _tenantClient.SendQueryAsync<NotificationMessageDto>(getQuery);
        return new PagedResult<NotificationMessageDto>(result?.Items ?? new List<NotificationMessageDto>());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NotificationMessageDto>> StoreNotificationMessages(string tenantId,
        IEnumerable<NotificationMessageDto> notificationMessages)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var t = notificationMessages.Select(nm =>
        {
            var mutationDto = new MutationDto<NotificationMessageInputDto>
            {
                RtId = nm.RtId,
                Item = new NotificationMessageInputDto
                {
                    BodyText = nm.BodyText,
                    SubjectText = nm.SubjectText,
                    ErrorText = nm.ErrorText,
                    SendStatus = nm.SendStatus,
                    SentDateTime = nm.SentDateTime,
                    LastTryDateTime = nm.LastTryDateTime
                }
            };
            return mutationDto;
        });

        try
        {
            var query = new GraphQLRequest
            {
                Query = GraphQl.UpdateNotificationMessage,
                Variables = new { entities = t }
            };

            return await _tenantClient.SendMutationAsync<IEnumerable<NotificationMessageDto>>(query);
        }
        catch (ServiceClientException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new NotificationSendFailedException("Storage of messages failed.", e);
        }
    }


    private async Task AddMessageAsync(NotificationMessageDto notificationMessageDto,
        RtEntityId? associatedRtId)
    {
        var notificationMessageInputDto = new NotificationMessageInputDto
        {
            SubjectText = notificationMessageDto.SubjectText,
            BodyText = notificationMessageDto.BodyText,
            RecipientAddress = notificationMessageDto.RecipientAddress,
            SendStatus = notificationMessageDto.SendStatus,
            NotificationType = notificationMessageDto.NotificationType,
            LastTryDateTime = DateTime.MinValue
        };

        if (associatedRtId != null)
        {
            notificationMessageInputDto.RelatesTo =
            [
                new RtAssociationInputDto
                {
                    Target = new RtEntityIdDto
                    {
                        RtId = associatedRtId.Value.RtId,
                        CkTypeId = associatedRtId.Value.CkTypeId,
                    },
                    ModOption = AssociationModOptionsDto.Create
                }
            ];
        }

        var query = new GraphQLRequest
        {
            Query = GraphQl.CreateNotificationMessage,
            Variables = new { entities = new[] { notificationMessageInputDto } }
        };

        await _tenantClient.SendMutationAsync<IEnumerable<RtServiceHookDto>>(query);
    }
}