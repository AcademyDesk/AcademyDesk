# AcademyDesk — Setup Checklist

Use this checklist in order. Do not create paid cloud services, payment accounts, or messaging accounts until the matching module is ready.

## A. Laptop readiness

### Hardware and Windows

- [ ] 64-bit Windows 11 (Windows 10 is workable, but Windows 11 is preferred)
- [ ] 16 GB RAM minimum; 32 GB recommended for Docker, SQL Server, API, and frontend running together
- [ ] At least 80 GB free SSD space; 150 GB preferred for Docker images, databases, packages, and backups
- [ ] Reliable broadband internet
- [ ] BIOS virtualization enabled, WSL 2 available, and Windows virtualization features enabled for Docker Desktop
- [ ] Windows Update completed and the laptop restarted if updates are pending
- [ ] A non-expiring, backed-up recovery method for the Windows account and BitLocker recovery key, if BitLocker is enabled

### Required developer software — install now

- [ ] Visual Studio Code
- [ ] Git for Windows
- [ ] .NET 10 SDK (x64 on a standard Intel/AMD Precision laptop; install the SDK, not runtime only)
- [ ] Current supported Node.js LTS, including npm
- [ ] Docker Desktop using WSL 2 integration
- [ ] SQL Server Developer Edition for local development, or SQL Server running locally through Docker
- [ ] SQL Server Management Studio (SSMS) or a supported SQL database extension/tool in VS Code
- [ ] PowerShell 7 is useful; Windows PowerShell is sufficient to begin

### Required VS Code extensions

- [ ] C# Dev Kit
- [ ] ESLint
- [ ] Prettier
- [ ] Docker
- [ ] GitHub Pull Requests and Issues (optional but recommended)
- [ ] SQL Server/MSSQL extension or your selected database client

### Install later, only if needed

- [ ] Bruno or Postman for API testing
- [ ] Azure CLI when we begin Azure provisioning/deployment
- [ ] GitHub CLI if command-line GitHub workflows are desired
- [ ] Node version manager only if multiple Node versions become necessary
- [ ] A design tool (for example, Figma) once we begin UI design

### Verify the laptop

Run these in a normal PowerShell window and provide the results before installing anything else:

```powershell
dotnet --version
node --version
npm --version
git --version
docker --version
docker compose version
code --version
wsl --status
```

Then, after SQL Server is installed, verify that the database service is running and that a local database connection succeeds.

## B. Local project foundation — create after laptop verification

- [ ] Private GitHub repository: `academydesk` (final name can change before first push)
- [ ] Main branch protection: pull request, successful checks, and review required before merge
- [ ] Repository folders: `apps/web`, `apps/api`, `database`, `infrastructure`, `docs`, `tests`
- [ ] `.gitignore`, `.editorconfig`, formatting rules, package-lock strategy, and secrets policy
- [ ] Local configuration template: `.env.example` and `appsettings.Development.example.json`, with no real secrets
- [ ] Docker Compose for local web, API, SQL Server, and development-only supporting services
- [ ] Database migrations, sample tenant data, and a reset-safe local development workflow
- [ ] Initial automated tests and CI pipeline before feature development

## C. Accounts to prepare now

- [ ] GitHub account with MFA enabled
- [ ] Microsoft/Azure account with an Azure subscription owner available
- [ ] A dedicated organization email address for AcademyDesk, or a documented temporary owner account
- [ ] Password manager for business credentials and recovery codes
- [ ] Azure cost budget owner and billing alert email address

Do **not** yet purchase a domain, activate a payment gateway, submit WhatsApp verification, or create production credentials.

## D. Azure — prepare now, keep costs controlled

- [ ] Azure subscription confirmed
- [ ] Resource naming and tagging convention agreed: environment, application, cost centre, owner
- [ ] Resource groups planned: `academydesk-dev`, `academydesk-staging`, `academydesk-prod`
- [ ] Monthly budget and alerts configured for the subscription and each environment
- [ ] Azure RBAC: least privilege; separate operational/deployment access from billing ownership
- [ ] Separate Development, Staging, and Production configuration strategy
- [ ] Infrastructure-as-code choice: Bicep is the recommended default for this Azure-first platform

### Azure resources for the first development deployment

- [ ] Azure Container Registry — builds/stores application images
- [ ] Azure Container Apps Environment and Container Apps — web and API hosting
- [ ] Azure SQL Database — production-compatible database validation
- [ ] Azure Storage Account / private Blob containers — secure file foundation
- [ ] Azure Key Vault — application secrets and provider credentials
- [ ] Application Insights and Log Analytics — errors, traces, metrics, alerts
- [ ] Managed identities — apps access Azure services without embedded passwords
- [ ] GitHub Actions deployment identity using a federated identity connection rather than long-lived Azure secrets where possible

### Azure resources for later modules

- [ ] Azure Functions or Container Apps Jobs — scheduled/background work
- [ ] Queue or service-bus technology only when job volume/reliability requirements justify it
- [ ] CDN/media delivery only when resource/video traffic requires it
- [ ] Dedicated tenant deployment stamps only for customers that require stronger isolation or generate sufficient revenue
- [ ] Web Application Firewall/front-door design when public scale and threat profile justify it

## E. External services — choose and configure when the matching feature is built

| Need | What will be required |
|---|---|
| Domain and public website | Final product name, domain purchase, DNS, HTTPS/custom-domain setup |
| Email | Transactional email provider, sender domain verification, templates, bounce handling |
| WhatsApp | WhatsApp Business Platform/provider, approved templates, recipient opt-in workflow, delivery handling |
| Academy student payments | Indian payment provider account, KYC/business details, orders, verified webhooks, reconciliation |
| AcademyDesk subscriptions | Separate product billing configuration, plans, taxes, invoices, dunning/cancellation rules |
| Customer support | Support inbox/ticketing process and data-access policy |
| Analytics | Privacy-respecting analytics plan, events catalogue, consent approach |

## F. Mandatory security and operational checklist before real customers

- [ ] Privacy policy, terms, data-retention schedule, and incident process reviewed with qualified Indian legal counsel
- [ ] Tenant-isolation test suite: a user from Academy A cannot access Academy B in any API, export, search, report, file, or background job
- [ ] Permission matrix implemented and tested
- [ ] MFA for platform and privileged academy accounts
- [ ] Secret rotation, backup, restore, and incident runbooks
- [ ] Private file access, malware scan route, file-size/type limits, and file-access audit logs
- [ ] Payment webhook signature verification, idempotency, reconciliation, and refund/adjustment controls
- [ ] Security headers, rate limits, input validation, dependency scans, and audit logging
- [ ] Automated unit, integration, and end-to-end tests for critical user journeys
- [ ] Monitoring dashboards and alerts for failed payments, failed notifications, job failures, API errors, and database capacity
- [ ] Staging user-acceptance testing with representative music and coaching academy data
- [ ] Backup-restore drill and launch/rollback checklist completed

## G. Immediate action

Start only with **A: Laptop readiness**. Send the verification-command output; then we will identify exactly what is already installed and fill only the gaps.

## Reference checks

- .NET 10 is the current recommended LTS SDK according to the [official .NET download page](https://dotnet.microsoft.com/en-us/download).
- SQL Server installation options and prerequisites are documented by [Microsoft Learn](https://learn.microsoft.com/en-us/sql/database-engine/install-windows/install-sql-server).
- Azure's guidance emphasizes tenant isolation as a core multitenant SaaS design requirement: [Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/overview).
- Online-payment server state must be verified through trusted provider signals such as webhooks, not browser-only confirmation; see [Razorpay’s webhook documentation](https://razorpay.com/docs/webhooks/).
