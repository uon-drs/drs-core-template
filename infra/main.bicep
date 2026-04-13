// TemplateApp — Main Infrastructure Deployment
//
// This file orchestrates all Azure resources for one environment.
// Deploy with a matching .bicepparam file:
//
//   az deployment group create \
//     --resource-group <rg-name> \
//     --template-file infra/main.bicep \
//     --parameters infra/main.dev.bicepparam \
//     --parameters postgresAdminPassword=<secret>
//
// All resource names are derived from appBaseName + environment.
// Change those two values and every resource name updates consistently.

// ============================================================
// Parameters
// ============================================================

@description('Base name for the application. All Azure resource names are derived from this. Use lowercase alphanumeric (no spaces or special characters).')
param appBaseName string

@description('Deployment environment.')
@allowed(['dev', 'qa', 'uat', 'prod'])
param environment string

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Keycloak OIDC authority URL used by the backend API for JWT validation (e.g. https://keycloak.example.com/realms/myrealm).')
param keycloakAuthority string

@description('Keycloak client ID used as the JWT audience for the backend API.')
param keycloakApiAudience string

@description('Keycloak client ID for the frontend application (used in KEYCLOAK_CLIENT_ID app setting).')
param keycloakFrontendClientId string

@description('Full Keycloak issuer URL for NextAuth.js (e.g. https://keycloak.example.com/realms/myrealm).')
param keycloakIssuerUrl string

@description('Administrator login username for PostgreSQL Flexible Server.')
param postgresAdminLogin string

@description('Administrator login password for PostgreSQL Flexible Server. Supply via pipeline secret variable — never store in .bicepparam files.')
@secure()
param postgresAdminPassword string

@description('SKU name for the App Service Plan.')
param aspSkuName string = 'B1'

@description('SKU name for PostgreSQL Flexible Server.')
param postgresSkuName string = 'Standard_B1ms'

@description('SKU tier for PostgreSQL Flexible Server.')
@allowed(['Burstable', 'GeneralPurpose', 'MemoryOptimized'])
param postgresSkuTier string = 'Burstable'

@description('PostgreSQL storage size in GB.')
param postgresStorageSizeGB int = 32

@description('Log Analytics Workspace retention in days.')
param logRetentionDays int = 30

@description('Enable zone-redundant high availability on PostgreSQL. Set true for production.')
param enableZoneRedundancy bool = false

@description('Deploy a VNet and configure App Service VNet Integration. Set true for production.')
param enableVnet bool = false

// ============================================================
// Derived resource names — single source of truth
// ============================================================

var aspName = '${appBaseName}-${environment}-asp'
var frontendAppName = '${appBaseName}-${environment}-frontend'
var backendAppName = '${appBaseName}-${environment}-api'
var keyVaultName = '${appBaseName}-${environment}-kv'
var logAnalyticsWorkspaceName = '${appBaseName}-shared-law'
var storageAccountName = '${toLower(appBaseName)}${environment}storage'
var postgresServerName = '${appBaseName}-${environment}-postgres'
var postgresInitialDbName = appBaseName
var vnetName = '${appBaseName}-${environment}-vnet'

var commonTags = {
  Source: 'Bicep'
  ServiceScope: appBaseName
  Environment: environment
  ManagedBy: 'IaC'
}

// ============================================================
// Log Analytics Workspace (shared across environments)
// ============================================================

module logAnalyticsWorkspace 'modules/components/log-analytics-workspace.bicep' = {
  name: 'deploy-law-${uniqueString(logAnalyticsWorkspaceName)}'
  params: {
    workspaceName: logAnalyticsWorkspaceName
    location: location
    retentionDays: logRetentionDays
    tags: commonTags
  }
}

// ============================================================
// Key Vault
// ============================================================

module keyVault 'modules/components/key-vault.bicep' = {
  name: 'deploy-kv-${uniqueString(keyVaultName)}'
  params: {
    keyVaultName: keyVaultName
    location: location
    enableSoftDelete: true
    softDeleteRetentionDays: environment == 'prod' ? 90 : 7
    tags: commonTags
  }
}

// ============================================================
// App Service Plan
// ============================================================

module appServicePlan 'modules/components/app-service-plan.bicep' = {
  name: 'deploy-asp-${uniqueString(aspName)}'
  params: {
    aspName: aspName
    aspSkuName: aspSkuName
    location: location
    tags: commonTags
  }
}

// ============================================================
// Storage Account
// ============================================================

module storageAccount 'modules/components/storage-account.bicep' = {
  name: 'deploy-storage-${uniqueString(storageAccountName)}'
  params: {
    storageAccountName: storageAccountName
    skuName: enableZoneRedundancy ? 'Standard_ZRS' : 'Standard_LRS'
    location: location
    tags: commonTags
  }
}

// ============================================================
// PostgreSQL Flexible Server
// ============================================================

module postgres 'modules/components/postgres-flexible-server.bicep' = {
  name: 'deploy-postgres-${uniqueString(postgresServerName)}'
  params: {
    serverName: postgresServerName
    adminLogin: postgresAdminLogin
    adminPassword: postgresAdminPassword
    skuName: postgresSkuName
    skuTier: postgresSkuTier
    storageSizeGB: postgresStorageSizeGB
    initialDatabaseName: postgresInitialDbName
    enableZoneRedundancy: enableZoneRedundancy
    location: location
    tags: commonTags
  }
}

// ============================================================
// VNet (optional — enable for production)
// ============================================================

module vnet 'modules/components/vnet.bicep' = if (enableVnet) {
  name: 'deploy-vnet-${uniqueString(vnetName)}'
  params: {
    vnetName: vnetName
    location: location
    tags: commonTags
  }
}

// ============================================================
// Frontend App Service (Next.js)
// ============================================================

module frontendApp 'modules/components/app-service.bicep' = {
  name: 'deploy-frontend-${uniqueString(frontendAppName)}'
  params: {
    appName: frontendAppName
    aspName: aspName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    appServiceKind: 'Linux Web App'
    vnetIntegrationSubnetId: enableVnet ? vnet.outputs.integrationSubnetId : ''
    location: location
    tags: commonTags
  }
  dependsOn: [appServicePlan, logAnalyticsWorkspace]
}

// ============================================================
// Backend App Service (ASP.NET Core API)
// ============================================================

module backendApp 'modules/components/app-service.bicep' = {
  name: 'deploy-backend-${uniqueString(backendAppName)}'
  params: {
    appName: backendAppName
    aspName: aspName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    appServiceKind: 'Linux Web App'
    vnetIntegrationSubnetId: enableVnet ? vnet.outputs.integrationSubnetId : ''
    location: location
    tags: commonTags
  }
  dependsOn: [appServicePlan, logAnalyticsWorkspace]
}

// ============================================================
// Key Vault access — grant Managed Identities read on secrets
// ============================================================

module frontendKvAccess 'modules/config/keyvault-access.bicep' = {
  name: 'kv-access-frontend-${uniqueString(frontendAppName)}'
  params: {
    keyVaultName: keyVaultName
    principalId: frontendApp.outputs.identity.principalId
  }
  dependsOn: [keyVault]
}

module backendKvAccess 'modules/config/keyvault-access.bicep' = {
  name: 'kv-access-backend-${uniqueString(backendAppName)}'
  params: {
    keyVaultName: keyVaultName
    principalId: backendApp.outputs.identity.principalId
  }
  dependsOn: [keyVault]
}

// ============================================================
// App Settings — configure after Key Vault access is granted
// ============================================================

module frontendConfig 'modules/config/webapp.bicep' = {
  name: 'config-frontend-${uniqueString(frontendAppName)}'
  params: {
    appName: frontendAppName
    appInsightsConnectionString: frontendApp.outputs.appInsights.connectionString
    frontendUrl: frontendApp.outputs.defaultUrl
    keycloakIssuerUrl: keycloakIssuerUrl
    keycloakClientId: keycloakFrontendClientId
    apiBaseUrl: backendApp.outputs.defaultUrl
    keyVaultName: keyVaultName
  }
  dependsOn: [frontendKvAccess]
}

module backendConfig 'modules/config/api.bicep' = {
  name: 'config-backend-${uniqueString(backendAppName)}'
  params: {
    appName: backendAppName
    appInsightsConnectionString: backendApp.outputs.appInsights.connectionString
    keyVaultName: keyVaultName
    keyVaultNameSetting: keyVaultName
    keycloakAuthority: keycloakAuthority
    keycloakAudience: keycloakApiAudience
    allowedOrigins: [frontendApp.outputs.defaultUrl]
  }
  dependsOn: [backendKvAccess]
}

// ============================================================
// Outputs
// ============================================================

output frontendUrl string = frontendApp.outputs.defaultUrl
output backendUrl string = backendApp.outputs.defaultUrl
output keyVaultName string = keyVault.outputs.name
output keyVaultUri string = keyVault.outputs.uri
output postgresServerFqdn string = postgres.outputs.serverFqdn
output postgresDatabaseName string = postgres.outputs.databaseName
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.outputs.name
