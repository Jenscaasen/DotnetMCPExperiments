# Azure Functions MCP Server - Deployment Guide

This guide provides step-by-step instructions for deploying the Azure Functions MCP server to Azure.

## 📋 Prerequisites

Before deploying, ensure you have:

1. **Azure CLI** installed and configured
   ```bash
   # Install Azure CLI (if not already installed)
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   
   # Login to Azure
   az login
   ```

2. **Azure Functions Core Tools** v4.x
   ```bash
   # Install via npm
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   
   # Verify installation
   func --version
   ```

3. **Azure Subscription** with appropriate permissions to create:
   - Resource Groups
   - Function Apps
   - Storage Accounts
   - Application Insights (optional)

## 🚀 Deployment Steps

### Step 1: Create Azure Resources

You can create resources using either the Azure CLI or Azure Portal.

#### Option A: Using Azure CLI (Recommended)

```bash
# Set variables
RESOURCE_GROUP="mcp-functions-rg"
LOCATION="eastus"
STORAGE_ACCOUNT="mcpfuncstore$(date +%s)"  # Must be unique
FUNCTION_APP="mcp-function-app-$(date +%s)"  # Must be unique
INSIGHTS_NAME="mcp-insights"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2

# Create Application Insights (optional but recommended)
az monitor app-insights component create \
  --app $INSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --kind web

# Create Function App
az functionapp create \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --storage-account $STORAGE_ACCOUNT \
  --runtime dotnet-isolated \
  --runtime-version 8.0 \
  --functions-version 4 \
  --app-insights $INSIGHTS_NAME \
  --consumption-plan-location $LOCATION
```

#### Option B: Using Azure Portal

1. **Create Resource Group**:
   - Go to Azure Portal → Resource Groups → Create
   - Choose subscription and region
   - Name: `mcp-functions-rg`

2. **Create Storage Account**:
   - Go to Storage Accounts → Create
   - Choose the resource group created above
   - Name: `mcpfuncstore[unique]`
   - Performance: Standard
   - Redundancy: LRS

3. **Create Function App**:
   - Go to Function Apps → Create
   - Choose the resource group and storage account
   - Runtime: .NET, Version 8.0 (LTS) Isolated
   - Operating System: Windows or Linux
   - Plan: Consumption (Serverless)

### Step 2: Configure Application Settings

Set up environment variables for your Function App:

```bash
# Configure function app settings
az functionapp config appsettings set \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
    "MCP_SERVER_NAME=Azure Functions MCP Server" \
    "MCP_SERVER_VERSION=1.0.0"

# Optional: Configure CORS for web clients
az functionapp cors add \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --allowed-origins "*"
```

### Step 3: Deploy the Function App

#### From Local Development Environment

1. **Navigate to project directory**:
   ```bash
   cd azurefunctions-mcp
   ```

2. **Build the project**:
   ```bash
   dotnet build --configuration Release
   ```

3. **Deploy using Azure Functions Core Tools**:
   ```bash
   func azure functionapp publish $FUNCTION_APP
   ```

#### From CI/CD Pipeline

Create a GitHub Actions workflow (`.github/workflows/deploy.yml`):

```yaml
name: Deploy Azure Functions MCP Server

on:
  push:
    branches: [main]
    paths: ['azurefunctions-mcp/**']
  workflow_dispatch:

env:
  AZURE_FUNCTIONAPP_NAME: 'your-function-app-name'
  AZURE_FUNCTIONAPP_PACKAGE_PATH: './azurefunctions-mcp'
  DOTNET_VERSION: '8.0.x'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }}
    
    - name: Build
      run: dotnet build ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }} --configuration Release --no-restore
    
    - name: Publish
      run: dotnet publish ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }} --configuration Release --no-build --output ./output
    
    - name: Deploy to Azure Functions
      uses: Azure/functions-action@v1
      with:
        app-name: ${{ env.AZURE_FUNCTIONAPP_NAME }}
        package: './output'
        publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}
```

### Step 4: Configure Function Keys (Security)

Azure Functions uses function keys for authentication by default:

```bash
# Get the default function key
FUNCTION_KEY=$(az functionapp function keys list \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --function-name mcp \
  --query "default" --output tsv)

echo "Function Key: $FUNCTION_KEY"
```

## 🔧 Configuration Settings

### Application Settings

Configure these settings in your Function App:

| Setting | Value | Description |
|---------|-------|-------------|
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` | Runtime configuration |
| `MCP_SERVER_NAME` | `Azure Functions MCP Server` | Server identification |
| `MCP_SERVER_VERSION` | `1.0.0` | Version information |
| `APPINSIGHTS_INSTRUMENTATIONKEY` | `auto-configured` | Application Insights |

### CORS Configuration

For web client access, configure CORS:

```bash
az functionapp cors add \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --allowed-origins \
    "https://yourdomain.com" \
    "http://localhost:3000" \
    "https://mcp-inspector.com"
```

### Authentication (Optional)

For production environments, consider:

1. **Function-level authentication** (default)
2. **Azure AD authentication**:
   ```bash
   az functionapp auth update \
     --name $FUNCTION_APP \
     --resource-group $RESOURCE_GROUP \
     --enabled true \
     --action LoginWithAzureActiveDirectory
   ```

## 🌐 Access Your Deployed Function

### Function URLs

After deployment, your functions will be available at:

```
https://{function-app-name}.azurewebsites.net/api/mcp
https://{function-app-name}.azurewebsites.net/api/health
https://{function-app-name}.azurewebsites.net/api/mcp-stream
```

### With Function Key Authentication

```bash
# MCP endpoint with function key
curl -X POST "https://$FUNCTION_APP.azurewebsites.net/api/mcp?code=$FUNCTION_KEY" \
  -H "Content-Type: application/json" \
  -d '{"method":"initialize","id":"1"}'

# Health check (no auth required)
curl "https://$FUNCTION_APP.azurewebsites.net/api/health"
```

### Testing MCP Connection

Use MCP Inspector to test:
1. **URL**: `https://your-function-app.azurewebsites.net/api/mcp?code=your-function-key`
2. **Transport**: Streamable HTTP
3. **Headers**: `Content-Type: application/json`

## 📊 Monitoring and Logging

### Application Insights

Monitor your deployed function through Application Insights:

1. **Navigate to Application Insights** in Azure Portal
2. **Key metrics to monitor**:
   - Request rate and response times
   - Failure rate and exceptions
   - Dependencies and performance
   - Custom telemetry

### Log Streaming

View real-time logs:

```bash
# Stream logs from Azure CLI
az webapp log tail --name $FUNCTION_APP --resource-group $RESOURCE_GROUP

# Or use Azure Functions Core Tools
func azure functionapp logstream $FUNCTION_APP
```

### Performance Monitoring

Key metrics to watch:

- **Cold start duration**: Initial request latency
- **Execution time**: Function execution duration
- **Memory usage**: Memory consumption patterns
- **Error rate**: Failed requests percentage

## 🔧 Troubleshooting

### Common Issues

1. **Deployment Failures**
   ```bash
   # Check deployment status
   az functionapp deployment list --name $FUNCTION_APP --resource-group $RESOURCE_GROUP
   
   # View deployment logs
   az functionapp log deployment show --name $FUNCTION_APP --resource-group $RESOURCE_GROUP
   ```

2. **Function Not Responding**
   ```bash
   # Check function app status
   az functionapp show --name $FUNCTION_APP --resource-group $RESOURCE_GROUP --query "state"
   
   # Restart function app
   az functionapp restart --name $FUNCTION_APP --resource-group $RESOURCE_GROUP
   ```

3. **CORS Issues**
   ```bash
   # List current CORS settings
   az functionapp cors show --name $FUNCTION_APP --resource-group $RESOURCE_GROUP
   
   # Remove and re-add CORS if needed
   az functionapp cors remove --name $FUNCTION_APP --resource-group $RESOURCE_GROUP --allowed-origins "*"
   az functionapp cors add --name $FUNCTION_APP --resource-group $RESOURCE_GROUP --allowed-origins "*"
   ```

4. **Authentication Problems**
   ```bash
   # Get function keys
   az functionapp function keys list --name $FUNCTION_APP --resource-group $RESOURCE_GROUP --function-name mcp
   
   # Generate new key if needed
   az functionapp function keys set --name $FUNCTION_APP --resource-group $RESOURCE_GROUP --function-name mcp --key-name default --key-value "your-new-key"
   ```

### Debug Mode

Enable detailed logging for troubleshooting:

```json
// host.json
{
  "version": "2.0",
  "logging": {
    "logLevel": {
      "default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

## 💰 Cost Optimization

### Consumption Plan Benefits

- **No idle costs**: Pay only for executions
- **Automatic scaling**: Scales to zero when not in use
- **Built-in load balancing**: Handles traffic spikes

### Cost Monitoring

```bash
# Set up budget alerts
az consumption budget create \
  --budget-name "MCP-Functions-Budget" \
  --amount 50 \
  --category Cost \
  --time-grain Monthly \
  --start-date "2024-01-01T00:00:00Z" \
  --end-date "2024-12-31T23:59:59Z" \
  --resource-group $RESOURCE_GROUP
```

## 🔄 Updates and Maintenance

### Updating the Function App

1. **Update code locally**
2. **Test locally**: `func start`
3. **Deploy updates**: `func azure functionapp publish $FUNCTION_APP`

### Scaling Considerations

For high-traffic scenarios:
- Consider **Premium Plan** for consistent performance
- Use **Application Insights** to monitor performance
- Implement **connection pooling** for external services

## 🔒 Security Best Practices

1. **Use Azure Key Vault** for sensitive configuration
2. **Enable Azure AD authentication** for production
3. **Configure network restrictions** if needed
4. **Regularly rotate function keys**
5. **Monitor for security threats** via Security Center

## 📚 Next Steps

After successful deployment:

1. **Set up monitoring alerts** in Application Insights
2. **Configure CI/CD pipeline** for automated deployments
3. **Implement comprehensive logging** for debugging
4. **Set up backup and disaster recovery**
5. **Review and optimize costs** regularly

For more advanced scenarios, consider the [ASP.NET Core implementation](../aspnetapisse/) which provides more control over hosting and routing.