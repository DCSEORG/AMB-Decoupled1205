#!/bin/bash
# =============================================================================
# deploy.sh - Main deployment script for Expense Management System
# Deploys: Resource Group, App Service, SQL Database, App Code
# Does NOT deploy GenAI resources (use deploy-with-chat.sh for that)
# =============================================================================
set -e

# ── Configuration ─────────────────────────────────────────────────────────────
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-expensemgmt-demo}"
LOCATION="${LOCATION:-uksouth}"

# Entra ID admin for SQL Server (REQUIRED - set these before running)
ADMIN_OBJECT_ID="${ADMIN_OBJECT_ID:-}"      # Your Entra ID Object ID (az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN="${ADMIN_LOGIN:-}"              # Your UPN e.g. user@company.com

# ── Pre-flight checks ─────────────────────────────────────────────────────────
echo "==========================================="
echo " Expense Management - Deploy Script"
echo "==========================================="

if [ -z "$ADMIN_OBJECT_ID" ] || [ -z "$ADMIN_LOGIN" ]; then
    echo ""
    echo "ERROR: ADMIN_OBJECT_ID and ADMIN_LOGIN must be set."
    echo ""
    echo "Run these commands first:"
    echo "  export ADMIN_OBJECT_ID=\$(az ad signed-in-user show --query id -o tsv)"
    echo "  export ADMIN_LOGIN=\$(az ad signed-in-user show --query userPrincipalName -o tsv)"
    echo ""
    exit 1
fi

echo "Resource Group : $RESOURCE_GROUP"
echo "Location       : $LOCATION"
echo "Admin Login    : $ADMIN_LOGIN"
echo ""

# ── Step 1: Create Resource Group ─────────────────────────────────────────────
echo "Step 1: Creating resource group..."
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output none
echo "  ✓ Resource group ready"

# ── Step 2: Deploy Infrastructure (App Service + SQL) ─────────────────────────
echo "Step 2: Deploying infrastructure (App Service + SQL)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file infra/main.bicep \
    --parameters \
        adminObjectId="$ADMIN_OBJECT_ID" \
        adminLogin="$ADMIN_LOGIN" \
        deployGenAI=false \
    --query properties.outputs \
    --output json)

echo "  ✓ Infrastructure deployed"

# Extract outputs
APP_SERVICE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.appServiceUrl.value')
SQL_SERVER_FQDN=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.sqlServerFqdn.value')
SQL_SERVER_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.sqlServerName.value')
DATABASE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.databaseName.value')
MI_CLIENT_ID=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.managedIdentityClientId.value')
CONNECTION_STRING=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.connectionString.value')

echo "  App Service  : $APP_SERVICE_NAME"
echo "  SQL Server   : $SQL_SERVER_FQDN"
echo "  Database     : $DATABASE_NAME"
echo "  MI Client ID : $MI_CLIENT_ID"

# Update Python scripts with the actual SQL server name
echo "Updating Python scripts with deployment values..."
sed -i.bak "s|sql-expensemgmt-SUFFIX.database.windows.net|$SQL_SERVER_FQDN|g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s|sql-expensemgmt-SUFFIX.database.windows.net|$SQL_SERVER_FQDN|g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s|sql-expensemgmt-SUFFIX.database.windows.net|$SQL_SERVER_FQDN|g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
echo "  ✓ Python scripts updated"

# ── Step 3: Configure App Service settings ─────────────────────────────────────
echo "Step 3: Configuring App Service settings..."
az webapp config appsettings set \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_SERVICE_NAME" \
    --settings \
        "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
        "ManagedIdentityClientId=$MI_CLIENT_ID" \
        "AZURE_CLIENT_ID=$MI_CLIENT_ID" \
        "ASPNETCORE_ENVIRONMENT=Production" \
    --output none
echo "  ✓ App Service settings configured"

# ── Step 4: Wait for SQL Server to be fully ready ─────────────────────────────
echo "Step 4: Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30
echo "  ✓ Wait complete"

# ── Step 5: Add IP to SQL Firewall ────────────────────────────────────────────
echo "Step 5: Configuring SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)

# Allow Azure services access
az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER_NAME" \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

# Add deployment IP
az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER_NAME" \
    --name "AllowDeploymentIP" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none

echo "  ✓ Firewall rules added (Azure services + $MY_IP)"
echo "  Waiting 15 seconds for firewall rules to propagate..."
sleep 15

# ── Step 6: Install Python dependencies ───────────────────────────────────────
echo "Step 6: Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity
echo "  ✓ Python dependencies installed"

# ── Step 7: Import database schema ────────────────────────────────────────────
echo "Step 7: Importing database schema..."
python3 run-sql.py
echo "  ✓ Database schema imported"

# ── Step 8: Configure database roles for managed identity ─────────────────────
echo "Step 8: Configuring database roles for managed identity..."
python3 run-sql-dbrole.py
echo "  ✓ Database roles configured"

# Deploy stored procedures
echo "Step 8b: Deploying stored procedures..."
python3 run-sql-stored-procs.py
echo "  ✓ Stored procedures deployed"

# ── Step 9: Build and deploy application code ─────────────────────────────────
echo "Step 9: Building and deploying application..."
cd app

# Update appsettings with actual values
sed -i.bak "s|sql-expensemgmt-SUFFIX.database.windows.net|$SQL_SERVER_FQDN|g" appsettings.json && rm -f appsettings.json.bak
sed -i.bak "s|MANAGED_IDENTITY_CLIENT_ID|$MI_CLIENT_ID|g" appsettings.json && rm -f appsettings.json.bak

# Restore and publish
dotnet restore
dotnet publish -c Release -o ./publish

# Create zip with files at root (not in subdirectory)
cd publish
zip -r ../../app.zip . -x "*.pdb"
cd ../..

echo "  ✓ App built and zipped"

# Deploy to Azure
az webapp deploy \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_SERVICE_NAME" \
    --src-path ./app.zip \
    --type zip \
    --output none

echo "  ✓ App deployed"

# ── Complete ──────────────────────────────────────────────────────────────────
echo ""
echo "==========================================="
echo " Deployment Complete!"
echo "==========================================="
echo ""
echo "  🌐 App URL  : $APP_SERVICE_URL/Index"
echo "  📖 API Docs : $APP_SERVICE_URL/swagger"
echo ""
echo "  NOTE: Navigate to $APP_SERVICE_URL/Index (not just the root URL)"
echo ""
echo "  To deploy with GenAI/Chat features, run: ./deploy-with-chat.sh"
echo ""
