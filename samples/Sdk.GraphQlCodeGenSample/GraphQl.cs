namespace Sdk.GraphQlCodeGenSample;

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
  
    public const string GetTenantConfigurations = @"
      query getTenantConfiguration{
        runtime{
          systemTenantConfiguration{
            items{
              rtId
              rtWellKnownName
              configurationValue
            }
          }
        }
      }
      ";
}