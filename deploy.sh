#!/bin/bash

##############################################
# Expense Management System - Azure Deployment
# Description: Deploy all Azure infrastructure and application code
##############################################

set -e  # Exit on error

# Configuration Variables
RESOURCE_GROUP="rg-expense-mgmt"
LOCATION="uksouth"
DEPLOY_GENAI=false  # Set to true to deploy Gen AI resources (Azure OpenAI)

# Color codes for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Expense Management System Deployment${NC}"
echo -e "${BLUE}========================================${NC}"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Please install Azure CLI: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Check if user is logged in
echo -e "\n${BLUE}Checking Azure CLI login status...${NC}"
if ! az account show &> /dev/null; then
    echo -e "${RED}Not logged in to Azure CLI${NC}"
    echo "Please run: az login"
    exit 1
fi

SUBSCRIPTION=$(az account show --query name -o tsv)
echo -e "${GREEN}✓ Logged in to Azure subscription: ${SUBSCRIPTION}${NC}"

# Deploy Infrastructure
echo -e "\n${BLUE}Deploying Azure infrastructure...${NC}"
DEPLOYMENT_OUTPUT=$(az deployment sub create \
  --location $LOCATION \
  --template-file infrastructure/main.bicep \
  --parameters resourceGroupName=$RESOURCE_GROUP location=$LOCATION deployGenAI=$DEPLOY_GENAI \
  --query 'properties.outputs' \
  --output json)

echo -e "${GREEN}✓ Infrastructure deployed successfully${NC}"

# Extract deployment outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_PRINCIPAL_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityPrincipalId.value')

echo -e "\n${BLUE}Deployment Details:${NC}"
echo "  App Service: ${APP_SERVICE_NAME}"
echo "  App URL: ${APP_SERVICE_URL}"
echo "  Managed Identity Client ID: ${MANAGED_IDENTITY_CLIENT_ID}"

if [ "$DEPLOY_GENAI" = true ]; then
    OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAiEndpoint.value')
    OPENAI_DEPLOYMENT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAiDeploymentName.value')
    echo "  Azure OpenAI Endpoint: ${OPENAI_ENDPOINT}"
    echo "  Model Deployment: ${OPENAI_DEPLOYMENT}"
    
    # Get OpenAI key securely
    OPENAI_RESOURCE_NAME=$(echo $OPENAI_ENDPOINT | sed -n 's/https:\/\/\([^.]*\).*/\1/p')
    OPENAI_KEY=$(az cognitiveservices account keys list \
      --name $OPENAI_RESOURCE_NAME \
      --resource-group $RESOURCE_GROUP \
      --query key1 -o tsv)
    
    # Configure App Service with OpenAI settings
    az webapp config appsettings set \
      --resource-group $RESOURCE_GROUP \
      --name $APP_SERVICE_NAME \
      --settings \
        "AzureOpenAI__Endpoint=$OPENAI_ENDPOINT" \
        "AzureOpenAI__DeploymentName=$OPENAI_DEPLOYMENT" \
        "AzureOpenAI__ApiKey=$OPENAI_KEY" \
      --output none
    
    # Save Gen AI settings (without key)
    cat > GenAISettings.json <<EOF
{
  "AzureOpenAI": {
    "Endpoint": "${OPENAI_ENDPOINT}",
    "DeploymentName": "${OPENAI_DEPLOYMENT}",
    "ApiVersion": "2024-02-15-preview"
  },
  "Note": "API key is stored securely in App Service configuration"
}
EOF
    echo -e "${GREEN}✓ GenAI settings configured in App Service${NC}"
fi

# Setup Database Access (if Python is available)
if command -v python3 &> /dev/null; then
    echo -e "\n${BLUE}Setting up database access...${NC}"
    
    # Install required Python packages if not already installed
    pip3 install --quiet pyodbc azure-identity
    
    # Run the Python script to setup managed identity permissions
    if [ -f "run-sql.py" ]; then
        python3 run-sql.py
        echo -e "${GREEN}✓ Database permissions configured${NC}"
    else
        echo -e "${YELLOW}⚠ run-sql.py not found, skipping database setup${NC}"
    fi
else
    echo -e "${YELLOW}⚠ Python 3 not found, skipping database setup${NC}"
    echo "  Please run the database setup manually using run-sql.py"
fi

# Deploy Application Code
echo -e "\n${BLUE}Deploying application code...${NC}"
if [ -f "app.zip" ]; then
    az webapp deploy \
      --resource-group $RESOURCE_GROUP \
      --name $APP_SERVICE_NAME \
      --src-path ./app.zip \
      --type zip
    echo -e "${GREEN}✓ Application deployed successfully${NC}"
else
    echo -e "${YELLOW}⚠ app.zip not found${NC}"
    echo "  Please build the application first"
fi

# Display access information
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "\n${YELLOW}IMPORTANT: Access the application at:${NC}"
echo -e "  ${APP_SERVICE_URL}/Index"
echo -e "\n${YELLOW}Note: Navigate to /Index, not just the root URL${NC}"

if [ "$DEPLOY_GENAI" = true ]; then
    echo -e "\n${GREEN}Chat UI is enabled with Gen AI features${NC}"
else
    echo -e "\n${YELLOW}Chat UI files are present but Gen AI resources not deployed${NC}"
    echo -e "To enable Gen AI features, set DEPLOY_GENAI=true in this script and re-run"
fi

echo ""
