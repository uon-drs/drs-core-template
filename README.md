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
                       ├── ci-backend.yml        — build & test .NET
                       ├── ci-frontend.yml       — build Next.js
                       ├── cd-infrastructure.yml — deploy Bicep
                       ├── cd-backend.yml        — deploy API (dev→qa→uat→prod)
                       ├── cd-frontend.yml       — deploy frontend (dev→qa→uat→prod)
                       └── azure-pipelines.yml   — manual orchestrator
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

**frontend/** — Next.js App Router with NextAuth.js handling the Keycloak Authorization Code + PKCE flow. The session stores the Keycloak access token, which is forwarded as a Bearer JWT to the backend.

**backend/** — ASP.NET Core Web API with JWT Bearer middleware that validates Keycloak tokens. Uses EF Core + Npgsql for PostgreSQL access and pulls secrets from Azure Key Vault via Managed Identity.

**infra/** — Bicep modules split into `components/` (create resources), `config/` (configure App Service settings), and `utils/` (shared types and helpers).

**pipelines/** — Azure DevOps YAML pipelines for CI (build + test) and CD (infrastructure + app deployment) across four environments.

### Azure Resource Naming Convention

All resource names are derived from `appBaseName` and `environment` in each `.bicepparam` file:

| Resource                | Pattern                                | Example (`appBaseName=myapp`, `environment=dev`) |
| ----------------------- | -------------------------------------- | ------------------------------------------------ |
| App Service Plan        | `{appBaseName}-{environment}-asp`      | `myapp-dev-asp`                                  |
| Frontend App Service    | `{appBaseName}-{environment}-frontend` | `myapp-dev-frontend`                             |
| Backend App Service     | `{appBaseName}-{environment}-api`      | `myapp-dev-api`                                  |
| Key Vault               | `{appBaseName}-{environment}-kv`       | `myapp-dev-kv`                                   |
| Log Analytics Workspace | `{appBaseName}-shared-law`             | `myapp-shared-law`                               |
| App Insights            | `{appBaseName}-{environment}-ai`       | `myapp-dev-ai`                                   |
| PostgreSQL Server       | `{appBaseName}-{environment}-postgres` | `myapp-dev-postgres`                             |
| Storage Account         | `{appBaseName}{environment}storage`    | `myappdevstorage`                                |
| VNet                    | `{appBaseName}-{environment}-vnet`     | `myapp-dev-vnet`                                 |

---

## Local Development

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

## Adapting This Template

### 1 — Rename

Do a **case-sensitive** global find-and-replace across the whole repo:

| Find                   | Replace with                     | Notes                                                                               |
| ---------------------- | -------------------------------- | ----------------------------------------------------------------------------------- |
| `TemplateApp`          | `MyProject`                      | PascalCase — used in .NET solution/project names and C# namespaces                  |
| `templateapp`          | `myproject`                      | lowercase, no spaces — used in Bicep `appBaseName`, npm name, ADO environment names |
| `templateapp-azure-sc` | your ADO service connection name |                                                                                     |
| `keycloak.example.com` | your Keycloak hostname           | appears in `infra/main.*.bicepparam` and pipeline variable files                    |
| `australiaeast`        | your Azure region                | appears in `pipelines/variables/*.yml`                                              |
| `templateapp-*-rg`     | your resource group names        | appears in `pipelines/variables/*.yml`                                              |

Then:

- [ ] Rename solution and project files from `TemplateApp.*` to `<YourName>.*`
- [ ] Update `backend/TemplateApp.sln` project references after renaming

### 2 — Configure Keycloak

Create two clients in your Keycloak realm:

**`templateapp-frontend`** (public client, Authorization Code + PKCE)

- [ ] Valid redirect URIs: `http://localhost:3000/api/auth/callback/keycloak`, `https://<frontend-app-service-url>/api/auth/callback/keycloak`
- [ ] Web origins: `http://localhost:3000`, `https://<frontend-app-service-url>`
- [ ] Note the client secret (if confidential) for the pipeline variable group

**`templateapp-api`** (confidential client, Bearer-only or standard)

- [ ] Used as the JWT audience for backend API validation
- [ ] Update Keycloak URLs in `infra/main.*.bicepparam` files
- [ ] Update `KEYCLOAK_ISSUER` in frontend `.env.example` and App Service config

### 3 — Provision Azure Infrastructure

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

- [ ] Create resource groups for each environment (dev, qa, uat, prod)
- [ ] Run validate + deploy for dev first; repeat for other environments
- [ ] Store secrets in Key Vault: `nextauth-secret`, `keycloak-frontend-client-secret`, `postgres-connection-string`
- [ ] Verify App Service Managed Identities have the `Key Vault Secrets User` role on the vault

### 4 — Set up Azure DevOps

- [ ] Create service connection `templateapp-azure-sc` (Azure Resource Manager, scoped to subscription)
- [ ] Create four ADO Environments:
  - [ ] `templateapp-dev` (no approvals)
  - [ ] `templateapp-qa` (no approvals)
  - [ ] `templateapp-uat` (1 approver required)
  - [ ] `templateapp-prod` (2 approvers required)
- [ ] Create variable groups in the ADO Library:
  - [ ] `templateapp-common` — non-secret shared variables
  - [ ] `templateapp-dev-secrets` — `postgresAdminPassword` (secret), `keycloakClientSecret` (secret), `nextauthSecret` (secret)
  - [ ] Repeat for `qa`, `uat`, `prod`
- [ ] Import pipeline YAML files into ADO:
  - [ ] `pipelines/ci-backend.yml` → name: "CI - Backend"
  - [ ] `pipelines/ci-frontend.yml` → name: "CI - Frontend"
  - [ ] `pipelines/cd-infrastructure.yml` → name: "CD - Infrastructure"
  - [ ] `pipelines/cd-backend.yml` → name: "CD - Backend"
  - [ ] `pipelines/cd-frontend.yml` → name: "CD - Frontend"
  - [ ] `pipelines/azure-pipelines.yml` → name: "CD - Orchestrator"
- [ ] Link `templateapp-common` variable group to each pipeline
- [ ] Link `templateapp-{env}-secrets` variable groups to `cd-infrastructure`, `cd-backend`, `cd-frontend`

### 5 — First Deployment

- [ ] Trigger `CI - Backend` on a feature branch to verify build and tests pass
- [ ] Trigger `CI - Frontend` on a feature branch to verify Next.js build passes
- [ ] Run `CD - Infrastructure` targeting `dev` to provision all Azure resources
- [ ] Run EF Core migrations against the dev PostgreSQL instance:
  ```bash
  dotnet ef database update --project backend/src/TemplateApp.Api
  ```
- [ ] Merge to `main` to trigger `CD - Backend` and `CD - Frontend` for dev deployment
- [ ] Verify end-to-end: frontend → Keycloak login → JWT forwarded to backend → 200 OK

### 6 — Smoke Testing

- [ ] `GET https://{backend-dev-url}/api/health` → 200 (unauthenticated)
- [ ] `GET https://{backend-dev-url}/api/health/auth` with valid JWT → 200
- [ ] Frontend sign-in flow completes without errors
- [ ] Application Insights receives telemetry from both frontend and backend

---

## Key Design Decisions

### RBAC on Key Vault (not Access Policies)

The Key Vault uses `enableRbacAuthorization: true`. App Service Managed Identities are granted the `Key Vault Secrets User` role via `infra/modules/config/keyvault-access.bicep`. This is the current Microsoft recommendation and is auditable via Azure Policy.

### Sensitive config via Key Vault references

Connection strings and client secrets are stored as Key Vault secrets and referenced in App Service settings as `@Microsoft.KeyVault(SecretUri=...)`. The plain-text values never appear in app settings or source control.

### `postgresAdminPassword` never in source

The PostgreSQL admin password is a `@secure()` Bicep parameter supplied only at deployment time via an Azure DevOps secret variable. It does not appear in any `.bicepparam` file.

### Next.js `output: 'standalone'`

Required for deployment to Azure App Service on Linux. The deployment pipeline copies the `public/` and `.next/static/` directories into the standalone output before packaging, as Next.js does not do this automatically.

### Progressive deployment (dev → qa → uat → prod)

The CD pipelines use Azure DevOps `deployment` jobs linked to ADO Environments. Approval gates are configured in the ADO UI — not in YAML — keeping pipeline code clean and approval policy in one place.

### Single `appBaseName` seeds all resource names

Every Azure resource name is derived from `appBaseName` + `environment` variables defined in `main.bicep`. This ensures consistent naming and means renaming the project only requires changing the parameter files.
