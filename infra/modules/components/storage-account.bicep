@description('Name for the storage account. Must be globally unique, 3-24 chars, lowercase alphanumeric only (no hyphens).')
param storageAccountName string

@description('SKU name. Use Standard_LRS for dev/qa, Standard_ZRS or Standard_GRS for production.')
@allowed(['Standard_LRS', 'Standard_ZRS', 'Standard_GRS', 'Standard_RAGRS'])
param skuName string = 'Standard_LRS'

@description('Azure region for this resource.')
param location string = resourceGroup().location

@description('Resource tags.')
param tags object = {}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: skuName
  }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
  tags: union({ Source: 'Bicep' }, tags)
}

output name string = storageAccount.name
output id string = storageAccount.id
output primaryBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob
