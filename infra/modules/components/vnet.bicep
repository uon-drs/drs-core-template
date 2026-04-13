// Basic VNet with two subnets:
//   - Default subnet: for general connectivity (VMs, private endpoints)
//   - Integration subnet: for App Service VNet Integration (requires SKU >= S1)
//
// Extend this module if you need additional subnets or more complex network topology.

@description('Name for the Virtual Network.')
param vnetName string

@description('Address space for the VNet.')
param addressPrefix string = '10.0.0.0/16'

@description('Address prefix for the default subnet.')
param defaultSubnetPrefix string = '10.0.0.0/24'

@description('Address prefix for the App Service VNet integration subnet.')
param integrationSubnetPrefix string = '10.0.1.0/24'

@description('Azure region for this resource.')
param location string = resourceGroup().location

@description('Resource tags.')
param tags object = {}

resource vnet 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [addressPrefix]
    }
    subnets: [
      {
        name: 'default'
        properties: {
          addressPrefix: defaultSubnetPrefix
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
      {
        name: 'integration'
        properties: {
          addressPrefix: integrationSubnetPrefix
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
          delegations: [
            {
              name: 'delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
    ]
  }
  tags: union({ Source: 'Bicep' }, tags)
}

output name string = vnet.name
output id string = vnet.id
output defaultSubnetId string = vnet.properties.subnets[0].id
output integrationSubnetId string = vnet.properties.subnets[1].id
