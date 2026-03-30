![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster
A project to show how GitHub coding agent can turn screenshots of a legacy app into a working proof-of-concept for a cloud native Azure replacement if the legacy database schema is also provided.

Steps to modernise an app:

1. Fork this repo 
2. In new repo replace the screenshots and sql schema (or keep the samples)
3. Open the coding agent and use app-mod-booster agent telling it "modernise my app"
4. When the app code is generated (can take up to 30 minutes) there will be a pull request to approve.
5. Now you can use codespaces to deploy the app to azure (or open VS Code and clone the repo locally - you will need to install some tools locally or use the devcontainer)
6. Open terminal and type "az login" to set subscription/context
7. Then type "bash deploy.sh" to deploy the app and db or "bash deploy-with-chat.sh" to deploy the app, db and chat UI.

Supporting slides for Microsoft Employees:
[Here](<https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs>)

---

## What was built (by GitHub Copilot)

This modernisation created the following:

| Component | Details |
|-----------|---------|
| **Infra** | `infra/main.bicep`, `app-service.bicep`, `azure-sql.bicep`, `genai.bicep` |
| **App** | ASP.NET Core Razor Pages (.NET 8), REST APIs with Swagger |
| **Chat UI** | Azure OpenAI GPT-4o function calling chat interface |
| **SQL** | Stored procedures for all CRUD, managed identity auth |
| **Scripts** | `deploy.sh` and `deploy-with-chat.sh` for full deployment |

### Quick start:
```bash
# Set your Entra ID details
export ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
export ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

# Deploy (without GenAI):
bash deploy.sh

# Deploy (with GenAI chat):
bash deploy-with-chat.sh
```

> 📌 After deployment, navigate to `<app-url>/Index` (not just the root URL)
> 📖 API docs available at `<app-url>/swagger`
