using './main.bicep'

param appBaseName = 'templateapp'
param environment = 'qa'

param aspSkuName = 'B2'

param postgresSkuName = 'Standard_B1ms'
param postgresSkuTier = 'Burstable'
param postgresStorageSizeGB = 32
param postgresAdminLogin = 'templateappadmin'

param keycloakAuthority = 'https://keycloak.example.com/realms/templateapp'
param keycloakApiAudience = 'templateapp-api'
param keycloakFrontendClientId = 'templateapp-frontend'
param keycloakIssuerUrl = 'https://keycloak.example.com/realms/templateapp'

param logRetentionDays = 30

param enableZoneRedundancy = false
param enableVnet = false
