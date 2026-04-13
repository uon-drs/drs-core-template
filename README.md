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
                       ├── cd-backend.yml        — deploy API (uat→prod)
                       ├── cd-frontend.yml       — deploy frontend (uat→prod)
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

## Using AI Assistants

If you use Claude or an OpenAI Codex-compatible agent, ask it to run the **Project Setup** from `CLAUDE.md` / `AGENTS.md` before doing any other work. It will collect your project details and perform all the renaming and file restructuring in one go.

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

**pipelines/** — Azure DevOps YAML pipelines for CI (build + test) and CD (infrastructure + app deployment) across two cloud environments: uat and prod.

### Azure Resource Naming Convention

All resource names are derived from `appBaseName` and `environment` in each `.bicepparam` file:

| Resource                | Pattern                                | Example (`appBaseName=myapp`, `environment=uat`) |
| ----------------------- | -------------------------------------- | ------------------------------------------------ |
| App Service Plan        | `{appBaseName}-{environment}-asp`      | `myapp-uat-asp`                                  |
| Frontend App Service    | `{appBaseName}-{environment}-frontend` | `myapp-uat-frontend`                             |
| Backend App Service     | `{appBaseName}-{environment}-api`      | `myapp-uat-api`                                  |
| Key Vault               | `{appBaseName}-{environment}-kv`       | `myapp-uat-kv`                                   |
| Log Analytics Workspace | `{appBaseName}-shared-law`             | `myapp-shared-law`                               |
| App Insights            | `{appBaseName}-{environment}-ai`       | `myapp-uat-ai`                                   |
| PostgreSQL Server       | `{appBaseName}-{environment}-postgres` | `myapp-uat-postgres`                             |
| Storage Account         | `{appBaseName}{environment}storage`    | `myappuatstorage`                                |
| VNet                    | `{appBaseName}-{environment}-vnet`     | `myapp-uat-vnet`                                 |

---

## Local Development

Development runs entirely on local machines. Each developer runs the frontend and backend locally against a local PostgreSQL instance and a shared (or local) Keycloak instance.

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

Resource groups are provisioned by IT. Update the names in `pipelines/variables/*.yml`.

```bash
# Validate the Bicep template
az deployment group validate \
  --resource-group templateapp-rg \
  --template-file infra/main.bicep \
  --parameters infra/main.uat.bicepparam \
  --parameters postgresAdminPassword=<password>

# Deploy
az deployment group create \
  --resource-group templateapp-rg \
  --template-file infra/main.bicep \
  --parameters infra/main.uat.bicepparam \
  --parameters postgresAdminPassword=<password>
```

- [ ] Confirm resource groups exist for uat and prod
- [ ] Run validate + deploy for uat first, then prod
- [ ] Store secrets in Key Vault: `nextauth-secret`, `keycloak-frontend-client-secret`, `postgres-connection-string`
- [ ] Verify App Service Managed Identities have the `Key Vault Secrets User` role on the vault

### 4 — Set up Azure DevOps

- [ ] Create service connection `templateapp-azure-sc` (Azure Resource Manager, scoped to subscription)
- [ ] Create two ADO Environments:
  - [ ] `templateapp-uat` (1 approver required)
  - [ ] `templateapp-prod` (2 approvers required)
- [ ] Create variable groups in the ADO Library:
  - [ ] `templateapp-common` — non-secret shared variables
  - [ ] `templateapp-uat-secrets` — `postgresAdminPassword` (secret), `keycloakClientSecret` (secret), `nextauthSecret` (secret)
  - [ ] `templateapp-prod-secrets` — same structure
- [ ] Import pipeline YAML files into ADO:
  - [ ] `pipelines/ci-backend.yml` → name: "CI - Backend"
  - [ ] `pipelines/ci-frontend.yml` → name: "CI - Frontend"
  - [ ] `pipelines/cd-infrastructure.yml` → name: "CD - Infrastructure"
  - [ ] `pipelines/cd-backend.yml` → name: "CD - Backend"
  - [ ] `pipelines/cd-frontend.yml` → name: "CD - Frontend"
  - [ ] `pipelines/azure-pipelines.yml` → name: "CD - Orchestrator"
- [ ] Link `templateapp-common` variable group to each pipeline
- [ ] Link `templateapp-uat-secrets` and `templateapp-prod-secrets` to `cd-infrastructure`, `cd-backend`, `cd-frontend`

### 5 — First Deployment

- [ ] Trigger `CI - Backend` on a feature branch to verify build and tests pass
- [ ] Trigger `CI - Frontend` on a feature branch to verify Next.js build passes
- [ ] Run `CD - Infrastructure` targeting `uat` to provision all Azure resources
- [ ] Run EF Core migrations against the uat PostgreSQL instance:
  ```bash
  dotnet ef database update --project backend/src/TemplateApp.Api
  ```
- [ ] Merge to `main` to trigger `CD - Backend` and `CD - Frontend` for uat deployment
- [ ] Verify end-to-end: frontend → Keycloak login → JWT forwarded to backend → 200 OK

### 6 — Smoke Testing

- [ ] `GET https://{backend-uat-url}/api/health` → 200 (unauthenticated)
- [ ] `GET https://{backend-uat-url}/api/health/auth` with valid JWT → 200
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

### Progressive deployment (uat → prod)

The CD pipelines use Azure DevOps `deployment` jobs linked to ADO Environments. Approval gates are configured in the ADO UI — not in YAML — keeping pipeline code clean and approval policy in one place.

### Single `appBaseName` seeds all resource names

Every Azure resource name is derived from `appBaseName` + `environment` variables defined in `main.bicep`. This ensures consistent naming and means renaming the project only requires changing the parameter files.
