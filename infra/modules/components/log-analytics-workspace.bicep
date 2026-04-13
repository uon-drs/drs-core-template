@description('Name for the Log Analytics Workspace.')
param workspaceName string

@description('Azure region for this resource.')
param location string = resourceGroup().location

@description('Log retention in days. 30 is sufficient for non-production.')
param retentionDays int = 30

@description('Resource tags.')
param tags object = {}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionDays
  }
  tags: union({ Source: 'Bicep' }, tags)
}

output name string = logAnalyticsWorkspace.name
output id string = logAnalyticsWorkspace.id
