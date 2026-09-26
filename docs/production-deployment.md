# Production deployment

AcademyDesk is prepared for an Azure Static Web App for the public web portal and one Azure Container App for the API. The production database is Azure SQL.

## Required production settings

Set these as Azure Container App secrets or environment variables. Never commit them.

- API: `ConnectionStrings__DefaultConnection` — Azure SQL connection string.
- API: `Cors__AllowedOrigins__0` — exact HTTPS URL of the web app, for example `https://app.example.com`.
- Static web build value: `NEXT_PUBLIC_API_URL` — exact HTTPS URL of the API, for example `https://api.example.com`.

The API health probe is `GET /health`; it reports 200 only when the API can connect to its database.

## GitHub Actions configuration

Create a GitHub environment named `production`. Add these secrets:

- `AZURE_CREDENTIALS`
- `AZURE_STATIC_WEB_APPS_API_TOKEN`
- `AZURE_SQL_CONNECTION_STRING`
- `PLATFORM_OWNER_PASSWORD` — only used to create the first Platform Owner account; keep it private.

Add these environment variables:

- `AZURE_RESOURCE_GROUP`
- `AZURE_REGISTRY_NAME`
- `AZURE_REGISTRY_LOGIN_SERVER`
- `AZURE_API_CONTAINER_APP`
- `PRODUCTION_API_URL`
- `WEB_APP_ORIGIN`
- `PLATFORM_OWNER_EMAIL`
- `PLATFORM_OWNER_NAME`

The checked-in workflow at `.github/workflows/deploy-azure.yml` builds the API container, exports the static web portal, and deploys both after a push to `main`. It keeps one API replica ready, allows it to scale to three replicas, and configures its database connection as a Container App secret.

## Before first public release

1. Provision Azure SQL with automatic backups and restrict its network access to the API.
2. Create an Azure Container Registry, one Azure Container Apps environment/API, and one Azure Static Web App.
3. Configure the static web app’s custom domain and TLS certificate first, then set that domain in the API CORS setting.
4. Configure the API custom domain and TLS certificate, then set that URL as the web build argument.
5. Run database migrations against Azure SQL as a controlled release step.
6. Verify `/health`, Platform Owner, Academy Admin, Teacher, and Student sign-in in staging before promoting to production.
