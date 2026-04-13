# TemplateApp — Setup Checklist & Architecture Reference

This document is for teams adopting this template. Work through the checklist below after completing the steps in README.md.

---

## Architecture Overview

```
frontend/          Next.js 15 App Router
                   ├── NextAuth.js → Keycloak (Authorization Code + PKCE)
                   ├── Session stores Keycloak access token
                   └── Passes Bearer JWT to backend

backend/           ASP.NET Core 9 Web API
                   ├── JWT Bearer middleware (validates Keycloak token)
                   ├── EF Core 9 + Npgsql → PostgreSQL
                   └── Azure Key Vault config provider (Managed Identity)

infra/             Bicep modules
                   ├── components/ — create Azure resources
                   ├── config/     — configure App Service settings
                   └── utils/      — shared types and helpers

pipelines/         Azure DevOps YAML
                   ├── ci-backend.yml     — build & test .NET
                   ├── ci-frontend.yml    — build Next.js
                   ├── cd-infrastructure.yml — deploy Bicep
                   ├── cd-backend.yml     — deploy API (dev→qa→uat→prod)
                   ├── cd-frontend.yml    — deploy frontend (dev→qa→uat→prod)
                   └── azure-pipelines.yml — manual orchestrator
```

### Resource Naming Convention

All Azure resource names are derived from two values set in each `.bicepparam` file:

| Resource | Pattern | Example (`appBaseName=myapp`, `environment=dev`) |
|---------|---------|-------|
| App Service Plan | `{appBaseName}-{environment}-asp` | `myapp-dev-asp` |
| Frontend App Service | `{appBaseName}-{environment}-frontend` | `myapp-dev-frontend` |
| Backend App Service | `{appBaseName}-{environment}-api` | `myapp-dev-api` |
| Key Vault | `{appBaseName}-{environment}-kv` | `myapp-dev-kv` |
| Log Analytics Workspace | `{appBaseName}-shared-law` | `myapp-shared-law` |
| App Insights | `{appBaseName}-{environment}-ai` | `myapp-dev-ai` |
| PostgreSQL Server | `{appBaseName}-{environment}-postgres` | `myapp-dev-postgres` |
| Storage Account | `{appBaseName}{environment}storage` | `myappdevstorage` |
| VNet | `{appBaseName}-{environment}-vnet` | `myapp-dev-vnet` |

---

## Setup Checklist

### Phase 1 — Rename the template

- [ ] Find-and-replace `TemplateApp` → your PascalCase project name (e.g. `AcmePlatform`)
- [ ] Find-and-replace `templateapp` → your lowercase project name (e.g. `acmeplatform`)
- [ ] Find-and-replace `keycloak.example.com` → your Keycloak hostname
- [ ] Find-and-replace `australiaeast` → your Azure region
- [ ] Update resource group names in `pipelines/variables/*.yml`
- [ ] Rename solution and project files from `TemplateApp.*` to `<YourName>.*`
- [ ] Update `backend/TemplateApp.sln` project references after renaming

### Phase 2 — Keycloak setup

- [ ] Create a Keycloak realm for the project
- [ ] Create client `templateapp-frontend` (public, Authorization Code + PKCE)
  - [ ] Add redirect URI: `https://{frontend-url}/api/auth/callback/keycloak`
  - [ ] Add web origin: `https://{frontend-url}`
  - [ ] Note the client secret (if confidential) for pipeline variable group
- [ ] Create client `templateapp-api` (audience for JWT Bearer validation)
- [ ] Update Keycloak URLs in `infra/main.*.bicepparam` files
- [ ] Update `KEYCLOAK_ISSUER` in frontend `.env.example` / App Service config

### Phase 3 — Azure infrastructure

- [ ] Create resource groups (one per environment)
  ```bash
  az group create --name templateapp-dev-rg --location australiaeast
  ```
- [ ] Run Bicep validate for each environment:
  ```bash
  az deployment group validate \
    --resource-group templateapp-dev-rg \
    --template-file infra/main.bicep \
    --parameters infra/main.dev.bicepparam \
    --parameters postgresAdminPassword=<secret>
  ```
- [ ] Deploy dev environment first:
  ```bash
  az deployment group create \
    --resource-group templateapp-dev-rg \
    --template-file infra/main.bicep \
    --parameters infra/main.dev.bicepparam \
    --parameters postgresAdminPassword=<secret>
  ```
- [ ] Store secrets in Key Vault (`nextauth-secret`, `keycloak-frontend-client-secret`, `postgres-connection-string`)
- [ ] Verify App Service Managed Identities have `Key Vault Secrets User` on the vault

### Phase 4 — Azure DevOps

- [ ] Create service connection `templateapp-azure-sc` (Azure Resource Manager, scoped to subscription)
- [ ] Create ADO Environments:
  - [ ] `templateapp-dev` (no approvals)
  - [ ] `templateapp-qa` (no approvals)
  - [ ] `templateapp-uat` (1 approver required)
  - [ ] `templateapp-prod` (2 approvers required)
- [ ] Create variable groups in ADO Library:
  - [ ] `templateapp-common` — non-secret shared variables
  - [ ] `templateapp-dev-secrets` — `postgresAdminPassword` (secret), `keycloakClientSecret` (secret), `nextauthSecret` (secret)
  - [ ] Repeat for `qa`, `uat`, `prod`
- [ ] Import pipeline YAML files:
  - [ ] `pipelines/ci-backend.yml` → name: "CI - Backend"
  - [ ] `pipelines/ci-frontend.yml` → name: "CI - Frontend"
  - [ ] `pipelines/cd-infrastructure.yml` → name: "CD - Infrastructure"
  - [ ] `pipelines/cd-backend.yml` → name: "CD - Backend"
  - [ ] `pipelines/cd-frontend.yml` → name: "CD - Frontend"
  - [ ] `pipelines/azure-pipelines.yml` → name: "CD - Orchestrator"
- [ ] Link `templateapp-common` variable group to each pipeline
- [ ] Link `templateapp-{env}-secrets` variable groups to `cd-infrastructure`, `cd-backend`, `cd-frontend`

### Phase 5 — First deployment

- [ ] Trigger `CI - Backend` on a feature branch to verify build and tests pass
- [ ] Trigger `CI - Frontend` on a feature branch to verify Next.js build passes
- [ ] Run `CD - Infrastructure` targeting `dev` to provision all Azure resources
- [ ] Run EF Core migrations against the dev PostgreSQL instance
  ```bash
  dotnet ef database update --project backend/src/TemplateApp.Api
  ```
- [ ] Merge to `main` to trigger `CD - Backend` and `CD - Frontend` for dev deployment
- [ ] Verify end-to-end: frontend → Keycloak login → JWT forwarded to backend → 200 OK

### Phase 6 — Smoke testing

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
