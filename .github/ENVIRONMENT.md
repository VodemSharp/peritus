# Environment Variables and Secrets

This document lists all configuration required for GitHub Actions and Dokploy.

## Naming Convention

All secrets and variables follow the pattern: `{ENV}_{RESOURCE}_{PURPOSE}`

| Env      | Examples                          |
|----------|-----------------------------------|
| `DEV_*`  | Development / staging environment |
| `PROD_*` | Production environment            |

---

## GitHub Variables

Configure at **Settings > Secrets and variables > Actions > Variables**.

| Name             | Used In       | Description                                   |
|------------------|---------------|-----------------------------------------------|
| `ASPIRE_VERSION` | All workflows | Aspire CLI version to install (e.g. `13.3.0`) |

---

## GitHub Secrets

Configure at **Settings > Secrets and variables > Actions > Secrets**.

### Repository Secrets

| Name                              | Used In        | Description                                               |
|-----------------------------------|----------------|-----------------------------------------------------------|
| `DEV_DOKPLOY_WEBHOOK_URL`         | `stage.yaml`   | Dokploy webhook to trigger dev redeploy                   |
| `PROD_DOKPLOY_WEBHOOK_URL`        | `release.yaml` | Dokploy webhook to trigger prod redeploy                  |
| `DEV_DATABASE_CONNECTION_STRING`  | `stage.yaml`   | External PostgreSQL connection string for dev migrations  |
| `PROD_DATABASE_CONNECTION_STRING` | `release.yaml` | External PostgreSQL connection string for prod migrations |

> **Note:** The GitHub database connection string secrets must match the `ConnectionStrings__db` value configured in
> Dokploy, since both the CI migration step and the deployed services connect to the same external database.

### Built-in Secrets

| Name           | Used In                      | Description                                          |
|----------------|------------------------------|------------------------------------------------------|
| `GITHUB_TOKEN` | `stage.yaml`, `release.yaml` | Auto-provided token for GHCR login and image cleanup |

---

## Workflow Environment Variables

These are hard-coded in the workflow files and do not need manual configuration.

| Name                  | Value                             | Description                        |
|-----------------------|-----------------------------------|------------------------------------|
| `REGISTRY_ENDPOINT`   | `ghcr.io`                         | Container registry endpoint        |
| `REGISTRY_REPOSITORY` | `${{ github.repository }}`        | Repository path in the registry    |
| `APP_VERSION`         | `${{ github.sha }}` / release tag | Image tag used by `aspire do push` |

---

## Dokploy Environment Variables

Set these in your Dokploy project **Environment** tab.

The Docker Compose output contains only the **API** and **Migrator** services. All infrastructure (PostgreSQL, Valkey,
and any future resources) is external and must be configured below.

| Name                       | Required | Description                                                      |
|----------------------------|----------|------------------------------------------------------------------|
| `ConnectionStrings__db`    | Yes      | External PostgreSQL connection string. Used by API and Migrator. |
| `ConnectionStrings__cache` | Yes      | External Valkey/Redis connection string. Used by API.            |

### Local Development

For `aspire run`, connection strings are auto-generated from the containerized Postgres and Valkey. The `dbUsername` and
`dbPassword` values are read from `appsettings.Development.json` in the AppHost project.
