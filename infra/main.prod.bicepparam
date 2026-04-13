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

param keycloakAuthority = 'https://keycloak.example.com/realms/templateapp'
param keycloakApiAudience = 'templateapp-api'
param keycloakFrontendClientId = 'templateapp-frontend'
param keycloakIssuerUrl = 'https://keycloak.example.com/realms/templateapp'

param logRetentionDays = 90

// Enable zone-redundant HA and VNet integration for production
param enableZoneRedundancy = true
param enableVnet = true
