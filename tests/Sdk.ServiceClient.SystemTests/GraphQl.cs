namespace Sdk.ServiceClient.SystemTests;

internal static class GraphQl
{
  public const string UpdateWalletNotificationUpdateDateTimeMutation = @"
      mutation updateWalletNotificationUpdateDateTime(
        $updates: [FireGuardiansWalletInputUpdate]!
      ) {
        runtime {
          fireGuardiansWallets {
            update(entities: $updates) {
              rtId
              identityId
              name
              lastNotificationUpdate
              location {
                point {
                  coordinates {
                    latitude
                    longitude
                  }
                }
              }
            }
          }
        }
      }
    ";
  
    public const string GetWalletsWithSubscriptions = @"
      query getWallets {
        runtime {
          fireGuardiansWallet {
            totalCount
            pageInfo {
              endCursor
            }
            items {
              rtChangedDateTime
              name
              location {
                point {
                  coordinates {
                    latitude
                    longitude
                  }
                }
              }
              children {
                fireGuardiansNotificationSubscription {
                  items {
                    rtId
                    endpoint
                    subscriptionConfiguration
                  }
                }
              }
            }
          }
        }
      }
      ";
}