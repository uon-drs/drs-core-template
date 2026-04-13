using './main.bicep'

param appBaseName = 'templateapp'
param environment = 'uat'

param aspSkuName = 'B2'

param postgresSkuName = 'Standard_B2ms'
param postgresSkuTier = 'Burstable'
param postgresStorageSizeGB = 64
param postgresAdminLogin = 'templateappadmin'

param keycloakAuthority = 'https://keycloak.example.com/realms/templateapp'
param keycloakApiAudience = 'templateapp-api'
param keycloakFrontendClientId = 'templateapp-frontend'
param keycloakIssuerUrl = 'https://keycloak.example.com/realms/templateapp'

param logRetentionDays = 60

param enableZoneRedundancy = false
param enableVnet = false
