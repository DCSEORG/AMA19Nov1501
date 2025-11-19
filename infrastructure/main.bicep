// Main deployment orchestrator
targetScope = 'subscription'

param resourceGroupName string = 'rg-expense-mgmt'
param location string = 'uksouth'
param deployGenAI bool = false

// Create Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
}

// Deploy Managed Identity
module managedIdentity 'managed-identity.bicep' = {
  scope: rg
  name: 'managedIdentityDeployment'
  params: {
    location: location
    managedIdentityName: 'mid-AppModAssist'
  }
}

// Deploy App Service
module appService 'app-service.bicep' = {
  scope: rg
  name: 'appServiceDeployment'
  params: {
    location: location
    managedIdentityId: managedIdentity.outputs.managedIdentityId
    managedIdentityClientId: managedIdentity.outputs.managedIdentityClientId
  }
}

// Deploy Gen AI Resources (conditional)
module genAI 'genai-resources.bicep' = {
  scope: rg
  name: 'genAIDeployment'
  params: {
    location: location
    deployGenAI: deployGenAI
  }
}

output appServiceUrl string = appService.outputs.appServiceUrl
output appServiceName string = appService.outputs.appServiceName
output managedIdentityClientId string = managedIdentity.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = managedIdentity.outputs.managedIdentityPrincipalId
output openAiEndpoint string = genAI.outputs.openAiEndpoint
output openAiDeploymentName string = genAI.outputs.openAiDeploymentName
