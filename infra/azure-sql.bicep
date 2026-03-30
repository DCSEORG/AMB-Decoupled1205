@description('Location for SQL resources')
param location string = 'uksouth'

@description('Resource name suffix')
param nameSuffix string = uniqueString(resourceGroup().id)

@description('Entra ID admin object ID')
param adminObjectId string

@description('Entra ID admin login (UPN)')
param adminLogin string

@description('Managed Identity Principal ID for DB permissions')
param managedIdentityPrincipalId string

@description('Managed Identity Client ID')
param managedIdentityClientId string

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: 'sql-expensemgmt-${nameSuffix}'
  location: location
  properties: {
    // Azure AD-only authentication - no SQL auth (MCAPS policy SFI-ID4.2.2)
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: adminLogin
      sid: adminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// SQL Database - named 'Northwind' as required
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: 'Northwind'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

// Azure AD-only authentication setting
resource azureADOnlyAuth 'Microsoft.Sql/servers/azureADOnlyAuthentications@2021-11-01' = {
  parent: sqlServer
  name: 'Default'
  properties: {
    azureADOnlyAuthentication: true
  }
}

// Firewall rule: Allow Azure services
resource firewallRuleAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Outputs
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlServerName string = sqlServer.name
output databaseName string = sqlDatabase.name
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};Authentication=Active Directory Managed Identity;User Id=${managedIdentityClientId};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
