@description('Name for the App Service Plan.')
param aspName string

@description('SKU name for the App Service Plan (e.g. B1, B2, P1v3).')
param aspSkuName string = 'B1'

@description('Azure region for this resource.')
param location string = resourceGroup().location

@description('Resource tags.')
param tags object = {}

resource asp 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: aspName
  location: location
  kind: 'linux'
  sku: {
    name: aspSkuName
  }
  properties: {
    reserved: true // required for Linux App Service Plans
  }
  tags: union({ Source: 'Bicep' }, tags)
}

output name string = asp.name
output id string = asp.id
