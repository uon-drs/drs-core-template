@export()
type AppServiceIdentityOutputs = {
  tenantId: string
  principalId: string
}

@export()
type AppServiceAppInsightsOutputs = {
  name: string
  connectionString: string
}

@export()
type ConnectionStringDictionary = {
  *: {
    value: string
    type: 'SQLServer' | 'Custom'
  }
}

@export()
type EnvironmentType = 'dev' | 'qa' | 'uat' | 'prod'
