namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

internal static class GraphQl
{
    public const string CreateNotificationMessage =
        @"mutation createNotification($entities: [SystemNotificationMessageInput]!) {
            createSystemNotificationMessages(entities: $entities) {
              notificationType
              sendStatus
              subjectText
              bodyText
              recipientAddress
              rtId
              sentDateTime
            }
          }
          ";

    public const string UpdateNotificationMessage = @"mutation updateNotification(
            $entities: [UpdateSystemNotificationMessageInput]!
          ) {
            updateSystemNotificationMessages(entities: $entities) {
              notificationType
              sendStatus
              subjectText
              bodyText
              recipientAddress
              rtId
              sentDateTime
            }
          }
          ";


    public const string GetNotificationMessages = @"query getNotifications(
            $after: String
            $first: Int
            $searchFilter: SearchFilter
            $fieldFilters: [FieldFilter]
            $sort: [Sort]
          ) {
            systemNotificationMessageConnection(
              after: $after
              first: $first
              searchFilter: $searchFilter
              fieldFilter: $fieldFilters
              sortOrder: $sort
            ) {
              totalCount
              items {
               notificationType
                sendStatus
                subjectText
                bodyText
                recipientAddress
                rtId
                sentDateTime
              }
            }
          }
          ";
}