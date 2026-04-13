@description('Name for the PostgreSQL Flexible Server.')
param serverName string

@description('Administrator login username.')
param adminLogin string

@description('Administrator login password. Supply via a secure pipeline variable — never store in source.')
@secure()
param adminPassword string

@description('SKU name for the PostgreSQL Flexible Server (e.g. Standard_B1ms, Standard_D2s_v3).')
param skuName string = 'Standard_B1ms'

@description('SKU tier. Burstable is cost-effective for dev/qa; GeneralPurpose or MemoryOptimized for production.')
@allowed(['Burstable', 'GeneralPurpose', 'MemoryOptimized'])
param skuTier string = 'Burstable'

@description('Storage size in GB.')
param storageSizeGB int = 32

@description('PostgreSQL major version.')
@allowed(['14', '15', '16'])
param postgresVersion string = '16'

@description('Initial database name to create.')
param initialDatabaseName string = 'app'

@description('Enable zone-redundant high availability. Recommended for production.')
param enableZoneRedundancy bool = false

@description('Azure region for this resource.')
param location string = resourceGroup().location

@description('Resource tags.')
param tags object = {}

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: serverName
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    version: postgresVersion
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    storage: {
      storageSizeGB: storageSizeGB
    }
    backup: {
      backupRetentionDays: enableZoneRedundancy ? 35 : 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: enableZoneRedundancy ? 'ZoneRedundant' : 'Disabled'
    }
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
  }
  tags: union({ Source: 'Bicep' }, tags)
}

// Allow connections from other Azure services (including App Service)
resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-12-01-preview' = {
  name: 'allow-azure-services'
  parent: postgresServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource initialDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  name: initialDatabaseName
  parent: postgresServer
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

output serverName string = postgresServer.name
output serverFqdn string = postgresServer.properties.fullyQualifiedDomainName
output databaseName string = initialDatabase.name
