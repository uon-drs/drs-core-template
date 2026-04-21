using './main.bicep'

param appBaseName = 'templateapp'
param environment = 'prod'

// Use a production-grade App Service Plan SKU (supports VNet Integration)
param aspSkuName = 'P1v3'

// PostgreSQL — larger SKU, zone-redundant HA
param postgresSkuName = 'Standard_D2s_v3'
param postgresSkuTier = 'GeneralPurpose'
param postgresStorageSizeGB = 128
param postgresAdminLogin = 'templateappadmin'

param keycloakAuthServerUrl = 'https://keycloak.example.com'
param keycloakRealm = 'templateapp-realm'
param keycloakApiClientId = 'templateapp-realm-api'
param keycloakFrontendClientId = 'templateapp-realm-frontend'

param logRetentionDays = 90

// Enable zone-redundant HA and VNet integration for production
param enableZoneRedundancy = true
param enableVnet = true
