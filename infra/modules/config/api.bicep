// Configures the ASP.NET Core backend App Service.
// Sets all required app settings including Application Insights, Keycloak, Key Vault, and CORS.
// The PostgreSQL connection string is stored in Key Vault and referenced, not stored directly.

import { referenceSecret } from '../utils/functions.bicep'

@description('Name of the existing App Service to configure.')
param appName string

@description('.NET runtime framework version.')
@allowed(['DOTNETCORE|8.0', 'DOTNETCORE|9.0', 'DOTNETCORE|10.0'])
param appFramework string = 'DOTNETCORE|10.0'

@description('Application Insights connection string (from app-service.bicep output).')
param appInsightsConnectionString string

@description('Name of the Key Vault containing the postgres-connection-string secret.')
param keyVaultName string

@description('Name of the Key Vault (used for the KeyVaultName app setting so Program.cs can locate it).')
param keyVaultNameSetting string

@description('Keycloak OIDC authority URL (e.g. https://keycloak.example.com/realms/myrealm).')
param keycloakAuthority string

@description('Keycloak audience (client ID of the API client in Keycloak).')
param keycloakAudience string

@description('Comma-separated list of allowed CORS origins (frontend URL).')
param allowedOrigins array = []

@description('Additional app settings to merge.')
param additionalAppSettings object = {}

module settings 'base/app-service.bicep' = {
  name: 'apiConfig-${uniqueString(appName)}'
  params: {
    appName: appName
    appFramework: appFramework
    appSettings: union(
      {
        // Application Insights
        APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
        ApplicationInsightsAgent_EXTENSION_VERSION: '~3'

        // Key Vault name — used by Program.cs to initialise the Key Vault config provider
        KeyVaultName: keyVaultNameSetting

        // Keycloak JWT Bearer configuration (maps to Keycloak:Authority and Keycloak:Audience in appsettings.json)
        Keycloak__Authority: keycloakAuthority
        Keycloak__Audience: keycloakAudience

        // CORS — frontend origin(s)
        AllowedOrigins__0: length(allowedOrigins) > 0 ? allowedOrigins[0] : ''

        WEBSITE_RUN_FROM_PACKAGE: '1'
      },
      additionalAppSettings
    )
    connectionStrings: {
      DefaultConnection: {
        // Connection string is stored in Key Vault; App Service resolves the reference at runtime
        value: referenceSecret(keyVaultName, 'postgres-connection-string')
        type: 'Custom'
      }
    }
  }
}
