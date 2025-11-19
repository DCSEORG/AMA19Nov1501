// App Service Plan and App Service with Managed Identity
param location string = 'uksouth'
param appServicePlanName string = 'asp-expense-mgmt'
param appServiceName string = 'app-expense-mgmt-${uniqueString(resourceGroup().id)}'
param managedIdentityId string
param managedIdentityClientId string

// App Service Plan with low-cost development SKU
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    family: 'B'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
  }
}

// App Service with managed identity
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: false // Not available in Basic tier
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'MANAGED_IDENTITY_CLIENT_ID'
          value: managedIdentityClientId
        }
        {
          name: 'EnableChatUI'
          value: 'true'
        }
      ]
    }
  }
}

output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServiceName string = appService.name
output appServicePrincipalId string = appService.identity.principalId
