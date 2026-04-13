// Grants a service principal (e.g. App Service Managed Identity) read access to Key Vault secrets
// using Azure RBAC (Key Vault Secrets User role).
//
// Prerequisites:
//   - The Key Vault must have enableRbacAuthorization: true (set in key-vault.bicep)
//   - The deploying identity must have 'Owner' or 'Role Based Access Control Administrator' on the vault
//
// Role reference: https://www.azadvertizer.net/azrolesadvertizer_all.html

@description('Name of the existing Key Vault to grant access to.')
param keyVaultName string

@description('Object (principal) ID of the Managed Identity to grant access to.')
param principalId string

var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().subscriptionId, keyVaultName, keyVaultSecretsUserRoleId, principalId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
