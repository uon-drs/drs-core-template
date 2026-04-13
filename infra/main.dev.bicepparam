using './main.bicep'

param appBaseName = 'templateapp'
param environment = 'dev'

// Azure region — update to your target region
// param location = 'australiaeast'  // defaults to resource group location

// App Service Plan SKU
param aspSkuName = 'B1'

// PostgreSQL
param postgresSkuName = 'Standard_B1ms'
param postgresSkuTier = 'Burstable'
param postgresStorageSizeGB = 32
param postgresAdminLogin = 'templateappadmin'
// postgresAdminPassword — supplied at deploy time via pipeline secret, never stored here

// Keycloak — update to your Keycloak server
param keycloakAuthority = 'https://keycloak.example.com/realms/templateapp'
param keycloakApiAudience = 'templateapp-api'
param keycloakFrontendClientId = 'templateapp-frontend'
param keycloakIssuerUrl = 'https://keycloak.example.com/realms/templateapp'

// Log retention (days)
param logRetentionDays = 30

// Not zone-redundant or VNet-integrated for dev
param enableZoneRedundancy = false
param enableVnet = false
