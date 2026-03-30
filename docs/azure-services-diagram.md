# Azure Services Architecture Diagram

## Expense Management System - Azure Services

```mermaid
graph TD
    User((👤 User)) --> AppService
    User --> ChatUI
    
    subgraph Azure_UK_South["Azure (UK South)"]
        subgraph App_Service_Plan["App Service Plan (S1)"]
            AppService["🌐 App Service\napp-expensemgmt-*\n(Razor Pages + API)"]
            ChatUI["💬 Chat UI\napp-expensemgmt-chat-*\n(AI Assistant)"]
        end
        
        ManagedIdentity["🔑 User-Assigned\nManaged Identity\nmid-AppModAssist-30-11-30"]
        
        subgraph SQL["Azure SQL"]
            SQLServer["🗄️ SQL Server\nsql-expensemgmt-*"]
            DB["📦 Database\nNorthwind"]
            SQLServer --> DB
        end
    end
    
    subgraph Azure_Sweden_Central["Azure (Sweden Central)"]
        AOAI["🤖 Azure OpenAI\naoai-expensemgmt-*\nGPT-4o model"]
        AISearch["🔍 AI Search\nsrch-expensemgmt-*\n(S0 SKU)"]
    end
    
    AppService --> |"Managed Identity Auth\n(Active Directory)"| SQLServer
    ChatUI --> |"REST API calls"| AppService
    ChatUI --> |"ManagedIdentityCredential"| AOAI
    ChatUI --> |"ManagedIdentityCredential"| AISearch
    
    ManagedIdentity --> |"Assigned to"| AppService
    ManagedIdentity --> |"Assigned to"| ChatUI
    ManagedIdentity --> |"Cognitive Services\nOpenAI User role"| AOAI
    ManagedIdentity --> |"Search Index\nData Reader role"| AISearch
    
    style Azure_UK_South fill:#dbeafe,stroke:#3b82f6
    style Azure_Sweden_Central fill:#d1fae5,stroke:#10b981
    style App_Service_Plan fill:#ede9fe,stroke:#8b5cf6
    style SQL fill:#fef3c7,stroke:#f59e0b
```

## Service Connections Summary

| From | To | Auth Method | Purpose |
|------|----|-------------|---------|
| App Service | Azure SQL (Northwind) | Managed Identity (Active Directory) | CRUD operations via stored procedures |
| Chat UI | App Service APIs | Internal HTTP | Function calling for expense operations |
| Chat UI | Azure OpenAI (GPT-4o) | ManagedIdentityCredential | Natural language processing |
| Chat UI | AI Search | ManagedIdentityCredential | RAG document retrieval |

## Deployment Scripts

| Script | What it deploys |
|--------|----------------|
| `deploy.sh` | Resource Group → App Service → SQL → Schema → App Code |
| `deploy-with-chat.sh` | Everything in deploy.sh + Azure OpenAI + AI Search |

## Key Resources

| Resource | SKU | Region |
|----------|-----|--------|
| App Service Plan | S1 Standard | UK South |
| Azure SQL Database | Basic | UK South |
| Azure OpenAI | S0 | Sweden Central |
| AI Search | Basic | Sweden Central |
| Managed Identity | N/A | UK South |
