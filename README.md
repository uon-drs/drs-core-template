# TemplateApp

A monorepo template for the standard application stack:
**Next.js · C#/.NET API · Keycloak · PostgreSQL · Azure · Azure DevOps**

---

## Repo Layout

```
/
├── frontend/          Next.js 16 App Router (TypeScript, NextAuth + Keycloak)
├── backend/           ASP.NET Core 10 Web API (EF Core + Npgsql, JWT Bearer)
├── infra/             Bicep modules for Azure provisioning
└── pipelines/         Azure DevOps YAML pipelines
```

---

## Prerequisites

| Tool              | Minimum version |
| ----------------- | --------------- |
| Node.js           | 20 LTS          |
| .NET SDK          | 10.0            |
| Azure CLI + Bicep | latest          |
| Azure DevOps      | any             |
| Keycloak          | 24+             |
| PostgreSQL        | 16+ (local dev) |

---

## Getting Started with This Template

### 1 — Replace placeholders

Do a **case-sensitive** global find-and-replace across the whole repo:

| Find                   | Replace with                     | Notes                                                                               |
| ---------------------- | -------------------------------- | ----------------------------------------------------------------------------------- |
| `TemplateApp`          | `MyProject`                      | PascalCase — used in .NET solution/project names and C# namespaces                  |
| `templateapp`          | `myproject`                      | lowercase, no spaces — used in Bicep `appBaseName`, npm name, ADO environment names |
| `templateapp-azure-sc` | your ADO service connection name |                                                                                     |
| `keycloak.example.com` | your Keycloak hostname           | appears in `infra/main.*.bicepparam` and pipeline variable files                    |
| `australiaeast`        | your Azure region                | appears in `pipelines/variables/*.yml`                                              |
| `templateapp-*-rg`     | your resource group names        | appears in `pipelines/variables/*.yml`                                              |

### 2 — Configure Keycloak

Create two clients in your Keycloak realm:

**`templateapp-frontend`** (public client)

- Valid redirect URIs: `http://localhost:3000/api/auth/callback/keycloak`, `https://<frontend-app-service-url>/api/auth/callback/keycloak`
- Web origins: `http://localhost:3000`, `https://<frontend-app-service-url>`

**`templateapp-api`** (confidential client, Bearer-only or standard)

- Used as the JWT audience for backend API validation

### 3 — Set up Azure resources

```bash
# Create a resource group per environment
az group create --name templateapp-dev-rg --location australiaeast

# Validate the Bicep template
az deployment group validate \
  --resource-group templateapp-dev-rg \
  --template-file infra/main.bicep \
  --parameters infra/main.dev.bicepparam \
  --parameters postgresAdminPassword=<password>

# Deploy
az deployment group create \
  --resource-group templateapp-dev-rg \
  --template-file infra/main.bicep \
  --parameters infra/main.dev.bicepparam \
  --parameters postgresAdminPassword=<password>
```

### 4 — Set up Azure DevOps

1. **Service connection**: Create an Azure Resource Manager service connection named `templateapp-azure-sc` (scoped to subscription or resource group).
2. **Environments**: Create four environments in ADO — `templateapp-dev`, `templateapp-qa`, `templateapp-uat`, `templateapp-prod`. Add approval gates to `templateapp-uat` (1 approver) and `templateapp-prod` (2 approvers).
3. **Variable groups**: Create these in the ADO Library:
   - `templateapp-common` — shared non-secret variables
   - `templateapp-dev-secrets` — `postgresAdminPassword`, `keycloakClientSecret`, `nextauthSecret` (mark as secret)
   - `templateapp-qa-secrets`, `templateapp-uat-secrets`, `templateapp-prod-secrets` — same structure
4. **Pipelines**: Import each YAML file from `pipelines/` into ADO. Link the relevant variable groups.

### 5 — Local development

**Frontend**

```bash
cd frontend
cp .env.example .env.local   # fill in values
npm install
npm run dev
```

**Backend**

```bash
cd backend
# Ensure a local PostgreSQL instance is running
dotnet restore
dotnet run --project src/TemplateApp.Api
```

---

## Architecture

```
                    ┌─────────────────┐
   Browser ────────▶│  Next.js (App   │
                    │  Router)        │
                    │  frontend/      │◀──── Keycloak (OIDC)
                    └────────┬────────┘           ▲
                             │ Bearer JWT          │
                             ▼                     │
                    ┌─────────────────┐            │
                    │  ASP.NET Core   │────────────┘
                    │  Web API        │  validates JWT
                    │  backend/       │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  PostgreSQL     │
                    │  (EF Core /     │
                    │  Npgsql)        │
                    └─────────────────┘

Azure resources:
  App Service Plan · Frontend App Service · Backend App Service
  Key Vault (RBAC) · Log Analytics Workspace · App Insights
  PostgreSQL Flexible Server · Storage Account · VNet (optional)
```

---

## Placeholder Reference

| Placeholder            | Scope                                                     |
| ---------------------- | --------------------------------------------------------- |
| `TemplateApp`          | .NET solution/project names, C# namespaces                |
| `templateapp`          | Bicep `appBaseName`, npm package name, ADO env names      |
| `templateapp-azure-sc` | ADO service connection name                               |
| `keycloak.example.com` | Keycloak host in Bicep param files and pipeline variables |
| `templateapp-api`      | Keycloak API audience (client ID)                         |
| `templateapp-frontend` | Keycloak frontend client ID                               |
| `australiaeast`        | Azure region                                              |
| `templateapp-{env}-rg` | Azure resource group names                                |
