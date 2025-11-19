# Expense Management System - Modern Cloud Application

![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

A modern, cloud-native expense management application built with ASP.NET Core and Azure services. This application demonstrates how legacy systems can be modernized into scalable, secure, and feature-rich solutions.

## Features

✨ **Modern Web Interface**
- Clean, responsive UI with gradient design
- Real-time expense tracking
- Interactive dashboard with statistics
- Mobile-friendly design

🔐 **Secure Authentication**
- Azure Managed Identity for database access
- No credentials stored in code
- TLS/HTTPS everywhere

💬 **AI-Powered Chat Assistant** (Optional)
- Natural language expense queries
- Conversational expense creation
- Intelligent insights and summaries
- Powered by Azure OpenAI GPT-4o

🚀 **RESTful API**
- Comprehensive expense management endpoints
- Swagger/OpenAPI documentation
- Easy integration capabilities

## Quick Start

### Prerequisites

- Azure subscription
- Azure CLI installed and configured (`az login`)
- Git
- .NET 8.0 SDK (for local development only)

### Deployment (Simple - One Command)

1. **Fork this repository** (important for testing)

2. **Clone your fork locally**
   ```bash
   git clone https://github.com/YOUR-USERNAME/YOUR-REPO-NAME.git
   cd YOUR-REPO-NAME
   ```

3. **Login to Azure and set subscription**
   ```bash
   az login
   az account set --subscription "YOUR-SUBSCRIPTION-NAME"
   ```

4. **Run the deployment script**
   ```bash
   ./deploy.sh
   ```

5. **Access your application**
   - Navigate to the URL displayed at the end of deployment
   - **Important**: Add `/Index` to the URL (e.g., `https://app-name.azurewebsites.net/Index`)

### Deployment with Gen AI Features (Chat UI)

To enable the AI chat assistant:

1. Edit `deploy.sh` and set:
   ```bash
   DEPLOY_GENAI=true
   ```

2. Run the deployment:
   ```bash
   ./deploy.sh
   ```

The AI chat button will appear in the bottom-right corner of the dashboard.

## Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for a detailed architecture diagram showing how all Azure services connect.

**Key Components:**
- **App Service** (B1 tier) - Hosts the web application
- **Managed Identity** - Secure authentication
- **Azure SQL Database** - Stores expense data
- **Azure OpenAI** (Optional) - Powers the chat assistant

## Application Structure

```
├── infrastructure/          # Bicep IaC templates
│   ├── main.bicep          # Main orchestrator
│   ├── app-service.bicep   # App Service resources
│   ├── managed-identity.bicep
│   └── genai-resources.bicep
├── ExpenseManagementApp/   # ASP.NET Core application
│   ├── Controllers/        # API controllers
│   ├── Models/            # Data models
│   ├── Services/          # Business logic
│   ├── Pages/             # Razor Pages (UI)
│   └── wwwroot/           # Static files
├── chatui/                # Chat UI documentation
│   └── RAG/              # Contextual information
├── deploy.sh              # Deployment orchestrator
├── run-sql.py            # Database setup script
├── script.sql            # SQL permissions
└── app.zip               # Application deployment package
```

## API Documentation

After deployment, access the Swagger documentation at:
```
https://your-app-name.azurewebsites.net/swagger
```

### Key Endpoints

- `GET /api/expenses` - List expenses (with filters)
- `POST /api/expenses` - Create new expense
- `POST /api/expenses/{id}/submit` - Submit for approval
- `POST /api/expenses/{id}/approve` - Approve expense
- `POST /api/expenses/{id}/reject` - Reject expense
- `GET /api/categories` - List expense categories
- `GET /api/users` - List users

## Database Schema

The application uses Azure SQL Database with the following tables:
- `Users` - Employee and manager information
- `Expenses` - Expense records
- `ExpenseCategories` - Travel, Meals, Supplies, etc.
- `ExpenseStatus` - Draft, Submitted, Approved, Rejected

See `Database-Schema/database_schema.sql` for complete schema.

## Cost Optimization

This solution is optimized for development/POC scenarios:
- **App Service**: B1 (Basic) tier ~£35/month
- **Azure SQL**: Basic tier (if you create one)
- **Azure OpenAI**: S0 tier, pay-per-use ~£0.03-0.06 per 1K tokens

**Estimated Monthly Cost**: £35-50 (without AI) or £50-100 (with AI, depending on usage)

## Security Features

✅ Managed Identity authentication (no passwords)
✅ TLS 1.2 encryption for all connections
✅ Azure AD integration for SQL
✅ HTTPS enforcement
✅ Secure credential storage in App Settings

## Development

### Local Development

1. Install .NET 8.0 SDK
2. Update connection strings in `appsettings.json`
3. Run the application:
   ```bash
   cd ExpenseManagementApp
   dotnet run
   ```

### Building from Source

```bash
cd ExpenseManagementApp
dotnet build
dotnet publish -c Release -o ../app
cd ..
zip -r app.zip app/
```

## Screenshots

Modern UI screenshots are available in the `Modern-Screenshots/` folder.

## Troubleshooting

### App Service returns 404
- Ensure you navigate to `/Index` not just the root URL

### Database connection fails
- Verify Managed Identity permissions using `run-sql.py`
- Check App Service has the correct identity assigned
- Confirm firewall rules allow Azure services

### Chat UI not working
- Verify `DEPLOY_GENAI=true` was set before deployment
- Check `GenAISettings.json` exists with correct configuration
- Review App Service logs for errors

## Contributing

This is a template repository for modernizing applications. To contribute:

1. Fork the repository (use a name like `AMA-YourTest01`)
2. Make your changes
3. Test thoroughly
4. Submit a pull request

**Important**: Always fork before running the coding agent to avoid polluting the base template.

## Guiding Principles

See [Guiding-Principles](./Guiding-Principles) for contribution and design guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or contributions:
- Open an issue in the repository
- Review existing documentation
- Check the Swagger API docs for endpoint details

## Acknowledgments

- Built with ASP.NET Core 8.0
- Azure services for cloud infrastructure
- OpenAI GPT-4o for AI capabilities

---

**Note**: This application is designed for development/POC purposes. For production deployment, review [PRODUCTION_CONSIDERATIONS](./docs/PRODUCTION_CONSIDERATIONS.md) and implement additional security, monitoring, and scalability features.
