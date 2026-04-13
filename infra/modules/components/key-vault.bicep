@description('Name for the Key Vault. Must be globally unique, 3-24 chars, alphanumeric and hyphens.')
param keyVaultName string

@description('Azure region for this resource.')
param location string = resourceGroup().location

@description('Enable soft delete protection. Recommended for production.')
param enableSoftDelete bool = true

@description('Soft delete retention in days (7-90). Only applies when enableSoftDelete is true.')
param softDeleteRetentionDays int = 7

@description('Resource tags.')
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    // RBAC authorisation is the recommended access model (not access policies).
    // Grant access via Microsoft.Authorization/roleAssignments using keyvault-access.bicep.
    enableRbacAuthorization: true
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionDays
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
  }
  tags: union({ Source: 'Bicep' }, tags)
}

output name string = keyVault.name
output id string = keyVault.id
output uri string = keyVault.properties.vaultUri
