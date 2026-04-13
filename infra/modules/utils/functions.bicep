// Generates a Key Vault reference string for use in App Service app settings.
// The App Service Managed Identity must have Key Vault Secrets User on the vault.
@export()
func referenceSecret(vaultName string, secretName string) string =>
  '@Microsoft.KeyVault(VaultName=${vaultName};SecretName=${secretName})'

// Extracts the .NET version number from a linuxFxVersion framework string.
// e.g. 'DOTNETCORE|9.0' -> 'v9.0', 'NODE|20-lts' -> 'v4.0'
@export()
func getDotNetVersion(appFramework string) string =>
  startsWith(split(appFramework, '|')[0], 'DOTNET')
    ? 'v${split(appFramework, '|')[1]}'
    : 'v4.0'
