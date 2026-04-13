// Configures the Next.js frontend App Service.
// Sets all required app settings including Application Insights, Keycloak, and backend API URL.
// Sensitive values are referenced from Key Vault rather than stored directly.

import { referenceSecret } from '../utils/functions.bicep'

@description('Name of the existing App Service to configure.')
param appName string

@description('Next.js runtime framework version.')
@allowed(['NODE|20-lts', 'NODE|22-lts', 'NODE|24-lts'])
param appFramework string = 'NODE|20-lts'

@description('Application Insights connection string (from app-service.bicep output).')
param appInsightsConnectionString string

@description('Public URL of this frontend App Service (used for NEXTAUTH_URL).')
param frontendUrl string

@description('Keycloak issuer URL (e.g. https://keycloak.example.com/realms/myrealm).')
param keycloakIssuerUrl string

@description('Keycloak client ID for this frontend application.')
param keycloakClientId string

@description('Base URL of the backend API (used for NEXT_PUBLIC_API_BASE_URL).')
param apiBaseUrl string

@description('Name of the Key Vault containing the nextauth-secret and keycloak-client-secret secrets.')
param keyVaultName string

@description('Additional app settings to merge.')
param additionalAppSettings object = {}

module settings 'base/app-service.bicep' = {
  name: 'webappConfig-${uniqueString(appName)}'
  params: {
    appName: appName
    appFramework: appFramework
    appSettings: union(
      {
        // Application Insights
        APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
        ApplicationInsightsAgent_EXTENSION_VERSION: '~3'

        // NextAuth
        NEXTAUTH_URL: frontendUrl
        NEXTAUTH_SECRET: referenceSecret(keyVaultName, 'nextauth-secret')

        // Keycloak
        KEYCLOAK_CLIENT_ID: keycloakClientId
        KEYCLOAK_CLIENT_SECRET: referenceSecret(keyVaultName, 'keycloak-frontend-client-secret')
        KEYCLOAK_ISSUER: keycloakIssuerUrl

        // Backend API
        NEXT_PUBLIC_API_BASE_URL: apiBaseUrl

        // Required for Next.js standalone deployment on App Service
        WEBSITE_RUN_FROM_PACKAGE: '1'
      },
      additionalAppSettings
    )
    siteConfig: {
      appCommandLine: 'node server.js'
    }
  }
}
