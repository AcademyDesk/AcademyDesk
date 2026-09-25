# Production deployment

AcademyDesk is prepared for two Azure Container Apps: one public web app and one API. The production database should be Azure SQL.

## Required production settings

Set these as Azure Container App secrets or environment variables. Never commit them.

- API: `ConnectionStrings__DefaultConnection` — Azure SQL connection string.
- API: `Cors__AllowedOrigins__0` — exact HTTPS URL of the web app, for example `https://app.example.com`.
- Web build argument: `NEXT_PUBLIC_API_URL` — exact HTTPS URL of the API, for example `https://api.example.com`.

The API health probe is `GET /health`; it reports 200 only when the API can connect to its database.

## GitHub Actions configuration

Create a GitHub environment named `production`. Add these secrets:

- `AZURE_CREDENTIALS`
- `AZURE_REGISTRY_LOGIN_SERVER`
- `AZURE_REGISTRY_USERNAME`
- `AZURE_REGISTRY_PASSWORD`

Add these environment variables:

- `AZURE_RESOURCE_GROUP`
- `AZURE_API_CONTAINER_APP`
- `AZURE_WEB_CONTAINER_APP`
- `PRODUCTION_API_URL`

The checked-in workflow at `.github/workflows/deploy-azure.yml` builds both containers and deploys them after a push to `main`.

## Before first public release

1. Provision Azure SQL with automatic backups and restrict its network access to the API.
2. Create an Azure Container Registry and two Azure Container Apps.
3. Configure the web app’s custom domain and TLS certificate first, then set that domain in the API CORS setting.
4. Configure the API custom domain and TLS certificate, then set that URL as the web build argument.
5. Run database migrations against Azure SQL as a controlled release step.
6. Verify `/health`, Platform Owner, Academy Admin, Teacher, and Student sign-in in staging before promoting to production.
