@description('Primary deployment location')
param location string = 'uksouth'

@description('Entra ID admin object ID for SQL')
param adminObjectId string

@description('Entra ID admin login (UPN) for SQL')
param adminLogin string

@description('Whether to deploy GenAI resources')
param deployGenAI bool = false

// Unique suffix for resource names
var nameSuffix = uniqueString(resourceGroup().id)

// App Service module (includes Managed Identity)
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    nameSuffix: nameSuffix
  }
}

// SQL Database module
module sqlDatabase 'azure-sql.bicep' = {
  name: 'sqlDatabaseDeployment'
  params: {
    location: location
    nameSuffix: nameSuffix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    managedIdentityClientId: appService.outputs.managedIdentityClientId
  }
}

// GenAI module (conditional)
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    nameSuffix: nameSuffix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityId string = appService.outputs.managedIdentityId
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output sqlServerFqdn string = sqlDatabase.outputs.sqlServerFqdn
output sqlServerName string = sqlDatabase.outputs.sqlServerName
output databaseName string = sqlDatabase.outputs.databaseName
output connectionString string = sqlDatabase.outputs.connectionString

// Null-safe GenAI outputs (only populated when deployGenAI=true)
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
