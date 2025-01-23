# Ensure that you have logged in to identity services (om-login-local.ps1, om-login-k8-staging.ps1)

octo-cli -c AddClientCredentialsClient --clientId "sample-backend" --name "Sample Backend" --secret "l8L@w5iEv*Ym"
octo-cli -c AddScopeToClient --clientid sample-backend --name "systemAPI.full_access"





