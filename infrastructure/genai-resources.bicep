// Azure OpenAI Service for Gen AI Chat UI
param openAiName string = 'aoai-expense-mgmt-${uniqueString(resourceGroup().id)}'
param deployGenAI bool = false

// Azure OpenAI Service
resource openAiService 'Microsoft.CognitiveServices/accounts@2023-05-01' = if (deployGenAI) {
  name: openAiName
  location: 'swedencentral' // GPT-4o is available in Sweden
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAiName
    publicNetworkAccess: 'Enabled'
  }
}

// Deploy GPT-4o model
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = if (deployGenAI) {
  parent: openAiService
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
  }
}

output openAiEndpoint string = deployGenAI ? openAiService.properties.endpoint : ''
output openAiKey string = '' // Do not output secrets - configure via portal or key vault
output openAiDeploymentName string = deployGenAI ? gpt4oDeployment.name : ''
