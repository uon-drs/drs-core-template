// Creates an Azure Managed SSL certificate for a custom hostname and binds it to an App Service.
//
// This must be deployed AFTER the hostname binding is created (use app-service.bicep first).
// When deploying multiple certificates, use @batchSize(1) in the calling module to avoid
// concurrent updates to the same App Service, e.g.:
//
//   @batchSize(1)
//   module cert 'modules/components/managed-cert.bicep' = [for hostname in appHostnames: {
//     name: 'cert-${uniqueString(hostname)}'
//     params: { appName: ..., aspId: ..., hostname: hostname }
//   }]

@description('Name of the existing App Service to bind the certificate to.')
param appName string

@description('Custom hostname for which to create the managed certificate.')
param hostname string

@description('Resource ID of the App Service Plan (required for managed cert creation).')
param aspId string

@description('Azure region for this resource.')
param location string = resourceGroup().location

resource app 'Microsoft.Web/sites@2022-09-01' existing = {
  name: appName
}

resource certificate 'Microsoft.Web/certificates@2022-09-01' = {
  name: '${appName}_${hostname}'
  location: location
  properties: {
    canonicalName: hostname
    serverFarmId: aspId
  }
}

resource hostnameBinding 'Microsoft.Web/sites/hostNameBindings@2022-09-01' = {
  name: hostname
  parent: app
  properties: {
    siteName: appName
    sslState: 'SniEnabled'
    hostNameType: 'Verified'
    customHostNameDnsRecordType: 'CName'
    thumbprint: certificate.properties.thumbprint
  }
}
