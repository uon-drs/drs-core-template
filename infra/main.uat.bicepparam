using './main.bicep'

param appBaseName = 'templateapp'
param environment = 'uat'

param aspSkuName = 'B2'

param postgresSkuName = 'Standard_B2ms'
param postgresSkuTier = 'Burstable'
param postgresStorageSizeGB = 64
param postgresAdminLogin = 'templateappadmin'

param keycloakAuthServerUrl = 'https://keycloak.example.com'
param keycloakRealm = 'templateapp-realm'
param keycloakApiClientId = 'templateapp-realm-api'
param keycloakFrontendClientId = 'templateapp-realm-frontend'

param logRetentionDays = 60

param enableZoneRedundancy = false
param enableVnet = false
