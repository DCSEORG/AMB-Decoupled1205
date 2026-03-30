@description('Location for the App Service')
param location string = 'uksouth'

@description('Resource name suffix for uniqueness')
param nameSuffix string = uniqueString(resourceGroup().id)

// User-assigned managed identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mid-appmodassist-30-11-30'
  location: location
}

// App Service Plan (S1 SKU)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'asp-expensemgmt-${nameSuffix}'
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
  properties: {
    reserved: false
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: 'app-expensemgmt-${nameSuffix}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ManagedIdentityClientId'
          value: managedIdentity.properties.clientId
        }
      ]
    }
  }
}

// Outputs
output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output managedIdentityId string = managedIdentity.id
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
