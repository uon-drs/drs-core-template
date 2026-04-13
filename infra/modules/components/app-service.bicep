import { AppServiceIdentityOutputs, AppServiceAppInsightsOutputs } from '../utils/types.bicep'
import { AppServiceKindDescriptor, getAppServiceKind } from '../utils/app-service-kind.bicep'

// Creates an App Service and its associated Application Insights resource.
//
// What this module does:
//   - Creates an Application Insights instance linked to the provided Log Analytics Workspace
//   - Creates an App Service with system-assigned Managed Identity
//   - Optionally configures VNet integration (requires SKU >= S1)
//   - Optionally binds custom hostnames (DNS verification must already be done)
//
// What this module does NOT do (do after deployment):
//   - App Settings / Connection Strings — may depend on Key Vault access
//   - SSL certificates — use managed-cert.bicep after hostname binding

@description('Name for the App Service.')
param appName string

@description('Name of the existing App Service Plan to deploy into.')
param aspName string

@description('Name of the existing Log Analytics Workspace for Application Insights.')
param logAnalyticsWorkspaceName string

@description('Kind of App Service to create.')
param appServiceKind AppServiceKindDescriptor = 'Linux Web App'

@description('Optional custom hostnames to bind. DNS verification must be complete before deployment.')
param appHostnames array = []

@description('Optional subnet resource ID for VNet integration. Requires SKU >= S1.')
param vnetIntegrationSubnetId string = ''

@description('Azure region for this resource.')
param location string = resourceGroup().location

@description('Resource tags.')
param tags object = {}

resource asp 'Microsoft.Web/serverfarms@2022-09-01' existing = {
  name: aspName
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${appName}-ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    IngestionMode: 'LogAnalytics'
    RetentionInDays: 90
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
  tags: union({ Source: 'Bicep' }, tags)
}

resource app 'Microsoft.Web/sites@2022-09-01' = {
  name: appName
  location: location
  kind: getAppServiceKind(appServiceKind)
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: asp.id
    enabled: true
    httpsOnly: true
    virtualNetworkSubnetId: empty(vnetIntegrationSubnetId) ? null : vnetIntegrationSubnetId
  }
  tags: union({ Source: 'Bicep' }, tags)
}

@batchSize(1) // hostname bindings must be applied serially
resource hostnameBindings 'Microsoft.Web/sites/hostNameBindings@2022-09-01' = [
  for hostname in appHostnames: {
    name: hostname
    parent: app
    properties: {
      siteName: appName
      sslState: 'Disabled'
      hostNameType: 'Verified'
      customHostNameDnsRecordType: 'CName'
    }
  }
]

output name string = app.name
output id string = app.id
output aspId string = asp.id
output defaultHostName string = app.properties.defaultHostName
output defaultUrl string = 'https://${app.properties.defaultHostName}'
output identity AppServiceIdentityOutputs = {
  tenantId: app.identity.tenantId
  principalId: app.identity.principalId
}
output appInsights AppServiceAppInsightsOutputs = {
  name: appInsights.name
  connectionString: appInsights.properties.ConnectionString
}
