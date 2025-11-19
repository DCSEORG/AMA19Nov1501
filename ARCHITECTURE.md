# Azure Services Architecture - Expense Management System

```
┌──────────────────────────────────────────────────────────────────────┐
│                         Azure Cloud Services                          │
└──────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                            Users & Access                            │
│  ┌──────────┐                                    ┌──────────┐       │
│  │ Employee │                                    │ Manager  │       │
│  └─────┬────┘                                    └────┬─────┘       │
│        │                                              │              │
│        └─────────────────┬────────────────────────────┘              │
│                          │                                           │
│                          ▼                                           │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                      Azure App Service (Linux)                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │  ASP.NET Core Web Application (Razor Pages + API)            │  │
│  │  ┌─────────────┐  ┌──────────┐  ┌────────────┐              │  │
│  │  │  Web UI     │  │ REST API │  │  Chat UI   │              │  │
│  │  │  /Index     │  │ /api/*   │  │  /Chat     │              │  │
│  │  └─────────────┘  └──────────┘  └────────────┘              │  │
│  │                                                               │  │
│  │  Swagger/OpenAPI: /swagger                                   │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  App Service Plan: B1 (Basic, Low-Cost Development)                 │
│  Region: UK South                                                    │
│  Runtime: .NET 8.0                                                   │
└─────────────────────────────────────────────────────────────────────┘
                          │
                          │ (Uses)
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│              User-Assigned Managed Identity                          │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │  mid-AppModAssist                                           │    │
│  │  • Authenticates to Azure SQL Database                      │    │
│  │  • No connection strings or passwords needed                │    │
│  │  • Assigned to App Service                                  │    │
│  └────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                          │
                          │ (Authenticates)
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Azure SQL Database                               │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │  Database: ExpenseManagementDB                              │    │
│  │  Server: sql-expense-mgmt-xyz.database.windows.net         │    │
│  │                                                             │    │
│  │  Tables:                                                    │    │
│  │  • Users (employees and managers)                           │    │
│  │  • Expenses (expense records)                               │    │
│  │  • ExpenseCategories (Travel, Meals, etc.)                 │    │
│  │  • ExpenseStatus (Draft, Submitted, Approved, Rejected)    │    │
│  └────────────────────────────────────────────────────────────┘    │
│                                                                      │
│  Authentication: Active Directory Managed Identity                   │
│  Encryption: TLS 1.2                                                 │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│           Azure OpenAI Service (Optional - If DEPLOY_GENAI=true)     │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │  Model: GPT-4o                                              │    │
│  │  Deployment Region: Sweden Central                          │    │
│  │  SKU: S0 (Standard, Low-Cost)                              │    │
│  │                                                             │    │
│  │  Features:                                                  │    │
│  │  • Natural language expense queries                         │    │
│  │  • Function calling to interact with APIs                   │    │
│  │  • Conversational expense creation                          │    │
│  │  • RAG pattern for contextual responses                     │    │
│  └────────────────────────────────────────────────────────────┘    │
│                                                                      │
│  Connected to App Service via API key (stored in App Settings)      │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                      Security & Access                               │
│                                                                      │
│  • All connections use TLS/HTTPS                                     │
│  • Managed Identity eliminates credential management                 │
│  • App Service: System-assigned + User-assigned identity            │
│  • SQL Database: Azure AD authentication                             │
│  • API Keys: Stored in App Service Configuration                    │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                      Deployment Flow                                 │
│                                                                      │
│  1. deploy.sh orchestrates all deployments                          │
│  2. Bicep templates create infrastructure:                          │
│     • main.bicep → Resource Group                                   │
│     • managed-identity.bicep → User-assigned identity               │
│     • app-service.bicep → App Service + Plan                        │
│     • genai-resources.bicep → Azure OpenAI (conditional)            │
│  3. run-sql.py configures database permissions                      │
│  4. app.zip deploys application code                                │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                      Key Configuration                               │
│                                                                      │
│  • DEPLOY_GENAI: false (default) - Chat UI files present but       │
│                                      Azure OpenAI not deployed       │
│  • DEPLOY_GENAI: true - Full Gen AI features enabled                │
│  • EnableChatUI: true (default) - Chat button visible in UI         │
│  • Access URL: https://<app-name>.azurewebsites.net/Index          │
└─────────────────────────────────────────────────────────────────────┘

```

## Component Summary

### Core Services
1. **App Service** - Hosts the web application and APIs
2. **Managed Identity** - Secure authentication mechanism
3. **Azure SQL** - Relational database for expense data

### Optional Services (DEPLOY_GENAI=true)
4. **Azure OpenAI** - AI-powered chat assistant

### Infrastructure as Code
- All resources deployed via Bicep templates
- One-line deployment command: `./deploy.sh`
- Region: UK South (except OpenAI: Sweden Central)
- Cost-optimized: Basic/S0 SKUs for development

## Data Flow

1. **User Access** → App Service URL
2. **Web UI** → JavaScript → REST API → Database
3. **Chat UI** → Azure OpenAI → Function Calling → REST API → Database
4. **Authentication** → App Service → Managed Identity → SQL Database

## Monitoring & Management

- Azure Portal for resource management
- Swagger UI at `/swagger` for API testing
- Application logs in App Service
- Database query monitoring in Azure SQL
