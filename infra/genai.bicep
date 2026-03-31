@description('Location for GenAI resources - always swedencentral for GPT-4o quota')
param location string = 'swedencentral'

@description('Resource name suffix')
param nameSuffix string = uniqueString(resourceGroup().id)

@description('Principal ID of the managed identity to assign roles to')
param managedIdentityPrincipalId string

@description('Resource ID of the managed identity to assign to the Chat App Service')
param managedIdentityId string

@description('Client ID of the managed identity for the Chat App Service')
param managedIdentityClientId string

@description('Location for web resources (Chat App Service) - uksouth for App Service compliance')
param ukLocation string = 'uksouth'

// Azure OpenAI - always swedencentral, lowercase name required
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: 'aoai-expensemgmt-${toLower(nameSuffix)}'
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: 'aoai-expensemgmt-${toLower(nameSuffix)}'
    publicNetworkAccess: 'Enabled'
  }
}

// GPT-4o model deployment
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
  }
}

// Azure AI Search
resource aiSearch 'Microsoft.Search/searchServices@2023-11-01' = {
  name: 'srch-expensemgmt-${toLower(nameSuffix)}'
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    publicNetworkAccess: 'enabled'
  }
}

// Role assignment: Cognitive Services OpenAI User for managed identity
// Role definition ID: 5e0bd9bd-7b93-4f28-af87-19fc36ad61bd
resource openAIUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, 'CognitiveServicesOpenAIUser')
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Role assignment: Search Index Data Reader for managed identity
resource searchIndexReaderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiSearch.id, managedIdentityPrincipalId, 'SearchIndexDataReader')
  scope: aiSearch
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// App Service Plan for Chat UI (uksouth - general compute region)
resource chatAppServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: 'asp-chat-expensemgmt-${toLower(nameSuffix)}'
  location: ukLocation
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
  properties: {
    reserved: false
  }
}

// Chat UI App Service
resource chatAppService 'Microsoft.Web/sites@2023-01-01' = {
  name: 'app-chat-expensemgmt-${toLower(nameSuffix)}'
  location: ukLocation
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: chatAppServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      alwaysOn: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentityClientId
        }
        {
          name: 'ManagedIdentityClientId'
          value: managedIdentityClientId
        }
      ]
    }
  }
}

// Outputs
output openAIEndpoint string = openAI.properties.endpoint
output openAIModelName string = gpt4oDeployment.name
output openAIName string = openAI.name
output searchEndpoint string = 'https://${aiSearch.name}.search.windows.net'
output searchName string = aiSearch.name
output chatAppServiceName string = chatAppService.name
output chatAppServiceUrl string = 'https://${chatAppService.properties.defaultHostName}'
