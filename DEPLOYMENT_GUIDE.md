# Deployment Guide - Expense Management System

This guide provides detailed instructions for deploying the modernized Expense Management System to Azure.

## Prerequisites

Before you begin, ensure you have:

1. **Azure Subscription** with appropriate permissions to create resources
2. **Azure CLI** (version 2.30.0 or higher)
   - Install: https://docs.microsoft.com/cli/azure/install-azure-cli
3. **Git** for cloning the repository
4. **Python 3.7+** (for database setup script)
5. **Basic understanding** of Azure services

## Pre-Deployment Steps

### 1. Fork and Clone Repository

```bash
# Fork the repository on GitHub first, then:
git clone https://github.com/YOUR-USERNAME/AMA19Nov1501.git
cd AMA19Nov1501
```

### 2. Login to Azure

```bash
# Login to Azure
az login

# List your subscriptions
az account list --output table

# Set the subscription you want to use
az account set --subscription "YOUR-SUBSCRIPTION-ID-OR-NAME"

# Verify the current subscription
az account show --output table
```

### 3. Review Configuration

Before deployment, review and optionally modify these settings in `deploy.sh`:

```bash
RESOURCE_GROUP="rg-expense-mgmt"        # Azure resource group name
LOCATION="uksouth"                       # Azure region
DEPLOY_GENAI=false                       # Set to true to deploy Azure OpenAI
```

## Standard Deployment (Without Gen AI)

This deployment creates the core application without the AI chat assistant.

### Step 1: Run Deployment Script

```bash
chmod +x deploy.sh
./deploy.sh
```

### Step 2: What Gets Deployed

The script will create:
- Resource Group: `rg-expense-mgmt`
- User-Assigned Managed Identity: `mid-AppModAssist`
- App Service Plan: `asp-expense-mgmt` (B1 tier)
- App Service: `app-expense-mgmt-XXXXX` (with unique suffix)

### Step 3: Wait for Completion

Deployment typically takes 5-10 minutes. You'll see:
- ✓ Infrastructure deployed
- ✓ Application deployed
- Access URL displayed

### Step 4: Access Application

Navigate to: `https://YOUR-APP-NAME.azurewebsites.net/Index`

**Important:** Always add `/Index` to the URL!

## Gen AI Deployment (With Chat Assistant)

To enable the AI-powered chat assistant:

### Step 1: Edit Configuration

```bash
nano deploy.sh
# or use your preferred editor
```

Change:
```bash
DEPLOY_GENAI=false
```
to:
```bash
DEPLOY_GENAI=true
```

### Step 2: Deploy

```bash
./deploy.sh
```

### Step 3: Additional Resources

With Gen AI enabled, the script also creates:
- Azure OpenAI Service in Sweden Central
- GPT-4o model deployment
- Configuration stored in App Service

### Step 4: Access Chat UI

- Navigate to the application
- Click the chat button (💬) in the bottom-right corner
- Or go directly to: `https://YOUR-APP-NAME.azurewebsites.net/Chat`

## Database Setup

### Automated Setup

The deployment script automatically:
1. Installs required Python packages (`pyodbc`, `azure-identity`)
2. Runs `run-sql.py` to configure database permissions
3. Grants managed identity access to Azure SQL

### Manual Setup (if automated fails)

If you need to set up the database manually:

```bash
# Install Python dependencies
pip3 install pyodbc azure-identity

# Run the SQL setup script
python3 run-sql.py
```

The script connects to:
- Server: `sql-expense-mgmt-xyz.database.windows.net`
- Database: `ExpenseManagementDB`

## Post-Deployment Verification

### 1. Check App Service

```bash
az webapp show \
  --resource-group rg-expense-mgmt \
  --name YOUR-APP-NAME \
  --query "state" \
  --output table
```

Expected output: `Running`

### 2. Test the Application

1. Open: `https://YOUR-APP-NAME.azurewebsites.net/Index`
2. You should see the expense management dashboard
3. Test creating a new expense
4. Verify the API at: `https://YOUR-APP-NAME.azurewebsites.net/swagger`

### 3. Check Logs

```bash
az webapp log tail \
  --resource-group rg-expense-mgmt \
  --name YOUR-APP-NAME
```

### 4. Test Database Connection

The application will display dummy data if the database connection fails. Check:
- Managed identity is assigned to App Service
- Firewall rules allow Azure services
- Database permissions are correct

## Troubleshooting

### Issue: 404 Not Found

**Solution:** Ensure you're navigating to `/Index` not just the root URL.

### Issue: Database Connection Failed

**Possible causes:**
1. Managed identity not assigned
2. Database firewall blocking connections
3. SQL permissions not granted

**Solution:**
```bash
# Re-run database setup
python3 run-sql.py

# Check managed identity assignment
az webapp identity show \
  --resource-group rg-expense-mgmt \
  --name YOUR-APP-NAME
```

### Issue: Chat UI Not Working

**Possible causes:**
1. `DEPLOY_GENAI=false` (Gen AI not deployed)
2. OpenAI configuration missing
3. API key not configured

**Solution:**
```bash
# Check if OpenAI is deployed
az cognitiveservices account list \
  --resource-group rg-expense-mgmt \
  --output table

# If not, redeploy with DEPLOY_GENAI=true
```

### Issue: Deployment Script Fails

**Common issues:**
- Not logged into Azure CLI: Run `az login`
- Insufficient permissions: Check subscription role
- Resource names conflict: Modify names in scripts
- Region doesn't support services: Change `LOCATION`

## Redeployment

To redeploy the application code only (without recreating infrastructure):

```bash
# Rebuild the application
cd ExpenseManagementApp
dotnet publish -c Release -o ../app
cd ..
zip -r app.zip app/

# Deploy just the application
az webapp deploy \
  --resource-group rg-expense-mgmt \
  --name YOUR-APP-NAME \
  --src-path ./app.zip \
  --type zip
```

## Cleanup

To remove all deployed resources:

```bash
az group delete \
  --name rg-expense-mgmt \
  --yes \
  --no-wait
```

**Warning:** This permanently deletes all resources in the resource group!

## Cost Management

### Estimated Monthly Costs (UK South)

**Without Gen AI:**
- App Service (B1): ~£35/month
- Total: ~£35/month

**With Gen AI:**
- App Service (B1): ~£35/month
- Azure OpenAI (S0): Pay-per-use, ~£0.03-0.06 per 1K tokens
- Typical usage: ~£15-25/month
- Total: ~£50-60/month

### Cost Optimization Tips

1. **Stop App Service when not in use:**
   ```bash
   az webapp stop --resource-group rg-expense-mgmt --name YOUR-APP-NAME
   ```

2. **Use Free tier for development** (modify Bicep):
   - Change SKU from B1 to F1 (Free)
   - Limited to 60 minutes/day compute time

3. **Monitor OpenAI usage:**
   ```bash
   az monitor metrics list \
     --resource YOUR-OPENAI-RESOURCE-ID \
     --metric "TotalTokens"
   ```

## Security Considerations

### For Production Use

1. **Enable Azure AD Authentication:**
   ```bash
   az webapp auth update \
     --resource-group rg-expense-mgmt \
     --name YOUR-APP-NAME \
     --enabled true \
     --action LoginWithAzureActiveDirectory
   ```

2. **Use Key Vault for Secrets:**
   - Move API keys to Azure Key Vault
   - Update App Service to reference Key Vault

3. **Enable Application Insights:**
   - Add monitoring and diagnostics
   - Track application performance

4. **Configure Custom Domain:**
   - Use your own domain with SSL certificate
   - Configure DNS and SSL bindings

5. **Set up Backup:**
   ```bash
   az webapp config backup create \
     --resource-group rg-expense-mgmt \
     --webapp-name YOUR-APP-NAME \
     --container-url "YOUR-STORAGE-SAS-URL" \
     --backup-name "initial-backup"
   ```

## Advanced Configuration

### Custom Settings

Add custom app settings:

```bash
az webapp config appsettings set \
  --resource-group rg-expense-mgmt \
  --name YOUR-APP-NAME \
  --settings KEY=VALUE
```

### Scale Up/Out

Scale to higher tier:

```bash
az appservice plan update \
  --name asp-expense-mgmt \
  --resource-group rg-expense-mgmt \
  --sku S1
```

### Enable Continuous Deployment

Set up GitHub Actions for automatic deployment when code changes.

## Support and Resources

- **Azure Documentation:** https://docs.microsoft.com/azure/
- **App Service Docs:** https://docs.microsoft.com/azure/app-service/
- **Bicep Docs:** https://docs.microsoft.com/azure/azure-resource-manager/bicep/
- **Azure OpenAI:** https://docs.microsoft.com/azure/cognitive-services/openai/

## Next Steps

After successful deployment:

1. ✅ Test all features in the application
2. ✅ Review the Swagger API documentation
3. ✅ Take screenshots for the Modern-Screenshots folder
4. ✅ Configure production settings if needed
5. ✅ Set up monitoring and alerts
6. ✅ Document any customizations made

---

**Note:** This guide assumes development/testing purposes. For production deployments, implement additional security, monitoring, backup, and scalability measures as outlined in PRODUCTION_CONSIDERATIONS.md
