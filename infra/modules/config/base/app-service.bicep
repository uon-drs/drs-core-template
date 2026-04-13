// Base App Service configuration module.
// Apply common site config, app settings, and connection strings to an existing App Service.
// Use the higher-level webapp.bicep or api.bicep modules rather than this one directly.

import { getDotNetVersion } from '../../utils/functions.bicep'
import { ConnectionStringDictionary } from '../../utils/types.bicep'

@description('Name of the existing App Service to configure.')
param appName string

@description('Linux runtime framework string (e.g. DOTNETCORE|9.0, NODE|20-lts). See: az webapp list-runtimes --os-type linux')
param appFramework string

@description('Additional site config properties to merge with the base config.')
param siteConfig object = {}

@description('App settings (key-value pairs) to apply.')
param appSettings object = {}

@description('Connection strings to apply.')
param connectionStrings ConnectionStringDictionary = {}

resource app 'Microsoft.Web/sites@2022-09-01' existing = {
  name: appName
}

var baseSiteConfig = {
  linuxFxVersion: appFramework
  netFrameworkVersion: getDotNetVersion(appFramework)
  http20Enabled: true
  minTlsVersion: '1.2'
  ftpsState: 'Disabled'
  alwaysOn: true
  use32BitWorkerProcess: false
  requestTracingEnabled: true
  httpLoggingEnabled: true
}

resource siteConfigResource 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: app
  name: 'web'
  properties: union(baseSiteConfig, siteConfig)
}

resource appSettingsResource 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: app
  name: 'appsettings'
  properties: appSettings
}

resource connectionStringsResource 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: app
  name: 'connectionstrings'
  properties: connectionStrings
}
