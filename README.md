# SwiftShip

SwiftShip is a full-stack package delivery / courier platform: a .NET 8 microservices backend and an Angular 19 frontend.

![SwiftShip landing page](package-delivery-web/public/screenshots/01-landing.png)

## Screenshots

The screenshots below show the app loaded with the [demo data](#4-optional-load-demo-data).

### Public tracking

| Guest tracking lookup | Sign in |
|---|---|
| ![Public tracking timeline](package-delivery-web/public/screenshots/02-public-tracking.png) | ![Login](package-delivery-web/public/screenshots/03-login.png) |

### Customer portal

| Dashboard | My shipments |
|---|---|
| ![Customer dashboard](package-delivery-web/public/screenshots/10-customer-dashboard.png) | ![Customer shipments](package-delivery-web/public/screenshots/11-customer-shipments.png) |
| **Shipment detail: live timeline, route map, invoice, rating** | **Create shipment** |
| ![Customer shipment detail](package-delivery-web/public/screenshots/12-customer-shipment-detail.png) | ![Create shipment](package-delivery-web/public/screenshots/13-customer-create-shipment.png) |
| **Invoices** | **Saved addresses** |
| ![Invoices](package-delivery-web/public/screenshots/14-customer-invoices.png) | ![Saved addresses](package-delivery-web/public/screenshots/15-customer-addresses.png) |

### Courier portal

| Dashboard | Assigned deliveries |
|---|---|
| ![Courier dashboard](package-delivery-web/public/screenshots/20-courier-dashboard.png) | ![Courier deliveries](package-delivery-web/public/screenshots/21-courier-deliveries.png) |
| **Delivery detail: status updates and delivery attempts** | **Route map** |
| ![Courier delivery detail](package-delivery-web/public/screenshots/22-courier-delivery-detail.png) | ![Courier route map](package-delivery-web/public/screenshots/23-courier-route-map.png) |

### Admin / dispatcher portal

| Dashboard | Dispatch board: manual and auto-assign |
|---|---|
| ![Admin dashboard](package-delivery-web/public/screenshots/30-admin-dashboard.png) | ![Dispatch board](package-delivery-web/public/screenshots/31-admin-dispatch-board.png) |
| **All shipments** | **Shipment detail** |
| ![Admin shipments](package-delivery-web/public/screenshots/32-admin-shipments.png) | ![Admin shipment detail](package-delivery-web/public/screenshots/33-admin-shipment-detail.png) |
| **Couriers** | **Customers** |
| ![Couriers](package-delivery-web/public/screenshots/34-admin-couriers.png) | ![Customers](package-delivery-web/public/screenshots/35-admin-customers.png) |
| **Analytics** | |
| ![Analytics](package-delivery-web/public/screenshots/36-admin-analytics.png) | |

## Architecture

Five independent microservices sit behind a YARP API Gateway, each with its own SQL Server database and its own Clean Architecture layering (`Domain` / `Application` / `Infrastructure` / API host). Shared cross-cutting code (Serilog logging, CORS, rate limiting, global exception handling, pagination, role constants, local file storage) lives in `BuildingBlocks`.

| Service | Port | Responsibility |
|---|---|---|
| `ApiGateway` (YARP) | 5000 | Single entry point; routes `/{service}/*` to each backend service |
| `IdentityService` | 5001 | Auth (JWT access + refresh tokens), users, roles, saved addresses |
| `PackageService` | 5002 (REST) / 5006 (gRPC) | Package records; exposes a gRPC validation endpoint consumed by ShipmentService |
| `ShipmentService` | 5003 | Shipment lifecycle, driver assignment/auto-assignment, delivery attempts + proof of delivery, invoicing, Stripe checkout, ratings, SLA breach monitoring, analytics |
| `TrackingService` | 5004 | Tracking events, public tracking lookup, SignalR live updates, email/SMS notifications |
| `DriverService` | 5005 | Driver profiles, availability, live location, internal nearest-driver lookup |
| `package-delivery-web` | 4200 | Angular frontend (Customer / Courier / Admin-Dispatcher portals) |

Roles: `Customer`, `Courier`, `Dispatcher`, `Admin` (see `BuildingBlocks/Authorization/Roles.cs`).

### Cross-service communication

- **Shipment → Package**: gRPC (`PackageValidationGrpc`) to validate a package and fetch its weight/delivery-type before creating a shipment.
- **Shipment → Driver**: internal REST call (shared-secret header) for nearest-available-driver lookup on auto-assign.
- **Shipment → Tracking**: RabbitMQ, via a transactional-outbox pattern (`OutboundIntegrationEvents` table written first, then published with Polly retry/circuit-breaker) — events: `ShipmentCreatedEvent`, `ShipmentStatusChangedEvent`, `ShipmentSlaBreachedEvent`.
- **Tracking → Identity**: internal REST call to resolve a customer's email/phone for notifications.
- Internal service-to-service endpoints are authenticated with a shared `X-Internal-Api-Key` header (`INTERNAL_API_KEY`), never a user JWT.
- Consumers dedupe incoming events by `(EventType, EventKey)` so redelivery from RabbitMQ never creates duplicate tracking rows.

### Notable backend features

- JWT auth with refresh tokens; role-based authorization on every endpoint.
- Idempotency-key support on shipment creation (safe client retries).
- Delivery attempts with proof-of-delivery photo upload (local disk today, behind a swappable `IFileStorageService`).
- Stripe Checkout integration for invoice payment, with a manual payment-status override endpoint as a fallback (falls back to a clear "Stripe not configured" error when unset, rather than blocking startup).
- Email (MailKit) and SMS (Twilio) notifications on status changes — both fall back to logging instead of sending when unconfigured, so the system runs fully in dev without either provider.
- SLA breach detection background job (per-delivery-type thresholds), published as an integration event once per shipment.
- Admin analytics endpoints: shipment status breakdown, delivered/failed-today counts, SLA breach counts, per-driver on-time performance.

## Frontend

`package-delivery-web/` — Angular 19, standalone components, Tailwind v4, Leaflet maps, SignalR live tracking.

- **Public**: landing page + guest tracking lookup (`/track/:trackingNumber`) with live SignalR updates, no login required.
- **Customer** (`/customer/**`): dashboard, create shipment (package + shipment in one flow), shipment list/detail with tracking timeline + map + Stripe payment + rating, saved addresses, invoices.
- **Courier** (`/courier/**`): dashboard with availability/location controls, assigned deliveries, delivery detail (status update, delivery attempt logging, proof-of-delivery upload), route map.
- **Admin/Dispatcher** (`/admin/**`): dispatch board (manual + auto-assign), all-shipments view, courier management (two-step account + driver-profile creation), customer list, analytics dashboard.

Core infrastructure (`src/app/core/`): typed HTTP services per backend, JWT interceptor with shared/deduplicated token-refresh-on-401, global error/loading interceptors, auth + role route guards, a SignalR wrapper, and models matching the backend DTOs exactly.

## Running locally with Docker (recommended)

The whole system — SQL Server, RabbitMQ, all five services, the gateway and the Angular frontend — runs from one `docker compose` command. No .NET SDK, Node or SQL Server install is needed on your machine.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows/macOS) or Docker Engine + Compose v2 (Linux), running.
- About 6 GB of free RAM for Docker (SQL Server alone needs ~2 GB).
- These ports free on your machine: `1433`, `4200`, `5000`–`5006`, `5672`, `15672`. If you already run SQL Server or RabbitMQ locally, stop them first.

### 1. Create your `.env` file

Copy the example file in the repo root:

```bash
# macOS / Linux / Git Bash
cp .env.example .env

# Windows PowerShell
Copy-Item .env.example .env
```

For local use the defaults work as-is. Values worth checking:

| Variable | Notes |
|---|---|
| `SA_PASSWORD` | SQL Server `sa` password. Must meet SQL Server's complexity rules (8+ chars with upper, lower, digit and symbol), or the database container won't start. |
| `JWT_KEY` | Must be at least 32 characters. |
| `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` | The admin account created on first start. You log in with these. |
| `SMTP_*`, `TWILIO_*`, `STRIPE_*` | Optional. Leave blank: emails and SMS are written to the logs instead of sent, and online payment is turned off. |

`.env` is git-ignored, so don't commit it.

### 2. Build and start

```bash
docker compose up --build -d
```

The first run builds seven images and takes 5–15 minutes. Later runs start in under a minute.

On startup, each service creates its own tables automatically (EF Core migrations run because docker-compose sets `Database__MigrateOnStartup=true`), and Identity creates the admin account from `.env`.

### 3. Check that it's running

```bash
docker compose ps
```

Wait until `sqlserver` and `rabbitmq` show `healthy` and `db-init` shows `Exited (0)`. Then open:

| What | URL |
|---|---|
| **Frontend** | http://localhost:4200 |
| API gateway | http://localhost:5000 |
| Swagger for each service | http://localhost:5001/swagger (Identity) up to http://localhost:5005/swagger (Driver) |
| RabbitMQ management UI | http://localhost:15672 (log in with `RABBITMQ_USERNAME` / `RABBITMQ_PASSWORD`) |
| SQL Server | `localhost,1433`, user `sa`, password `SA_PASSWORD` (e.g. from SSMS or Azure Data Studio) |

Log in to the frontend with `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD`. From the admin portal you can create courier accounts, and customers can sign up from the register page.

### 4. (Optional) Load demo data

To explore the app with realistic data instead of empty screens, run the seed script while the stack is up (needs Node 22+):

```bash
node scripts/seed-demo-data.mjs
```

It calls the app's own APIs through the gateway, so the data is consistent across all five databases, and tracking events arrive through RabbitMQ just as they do in normal use. It creates:

- 1 dispatcher, 5 couriers with driver profiles and live locations (4 on duty, 1 off duty), and 6 customers with saved addresses, all around Colombo.
- 20 shipments across Sri Lanka that together cover every outcome: unassigned, assigned, picked up, in transit, out for delivery, delivered (plus one delivered on a second attempt), failed delivery, returned, and cancelled. The data also includes invoices in every payment state, customer ratings, delivery attempts and full tracking timelines.

All demo accounts use the password `Demo_Passw0rd!`:

| Role | Email |
|---|---|
| Dispatcher | `dispatcher@swiftship.local` |
| Courier | `kasun.courier@swiftship.local` (also `tharindu`, `dilan`, `ruwan`, `isuru`) |
| Customer | `amaya.customer@swiftship.local` (also `chamod`, `sanduni`, `pasindu`, `hiruni`, `yasiru`) |

Re-running the script is safe: existing accounts are reused, and shipments are skipped if any already exist. Pass `--force` to add another batch. The script takes a minute or two, because it waits out the services' rate limit of 100 requests per minute.

### Everyday commands

```bash
docker compose logs -f                    # follow logs from every container
docker compose logs -f shipment-service   # follow one service
docker compose up --build -d frontend     # rebuild and restart one service after a code change
docker compose down                       # stop everything (your data is kept)
docker compose down -v                    # stop and DELETE all data (database, uploads) for a clean start
```

### Troubleshooting

- **`sqlserver` never becomes healthy.** Usually `SA_PASSWORD` is too weak. Fix it in `.env`, then run `docker compose down -v` and `docker compose up -d`. The `-v` matters: SQL Server keeps the first password it was started with in its data volume.
- **"port is already allocated".** Something else is using that port. Stop it, or change the left-hand number of that `ports:` entry in `docker-compose.yml`.
- **A service exits right after starting.** Run `docker compose logs <service-name>`. If the database was still starting, `docker compose up -d` starts it again, and migrations retry for about 50 seconds.
- **The frontend loads but every request fails.** Check that `api-gateway` is running (`docker compose ps`). The browser talks to the gateway at `http://localhost:5000`.

## Running without Docker (for development)

### Backend

```bash
dotnet build PackageDeliverySystem.sln
dotnet run --project src/Services/Identity/IdentityService
dotnet run --project src/Services/Package/PackageService
dotnet run --project src/Services/Shipment/ShipmentService
dotnet run --project src/Services/Tracking/TrackingService
dotnet run --project src/Services/Driver/DriverService
dotnet run --project src/ApiGateway/ApiGateway
```

Each service needs its own SQL Server database, reachable through the `ConnectionStrings` setting in `appsettings.Development.json`. All five services need the same `Jwt:Key`, `Jwt:Issuer` and `Jwt:Audience`, set with user-secrets or environment variables; the checked-in placeholder key is deliberately not safe for production. To have a service create its own tables, set `Database__MigrateOnStartup=true`, or run `dotnet ef database update` per service.

A handy middle ground is to run only the infrastructure in Docker: `docker compose up -d sqlserver db-init rabbitmq`.

### Frontend

```bash
cd package-delivery-web
npm install
npm start          # dev server on http://localhost:4200; calls the gateway directly at http://localhost:5000 (no proxy)
```

## Testing

```bash
# Backend — 118+ tests across all 5 services (xUnit + Moq, WebApplicationFactory + EF Core InMemory for integration coverage)
dotnet test PackageDeliverySystem.sln

# Frontend — Karma/Jasmine unit tests
cd package-delivery-web
npm test
```

## CI

`.github/workflows/ci.yml` builds and tests the backend, builds and unit-tests the frontend, and (on push to `main`/`master`) builds all Docker images.

## Deployment (Azure)

`infra/main.bicep` provisions everything the system needs on Azure Container Apps (consumption plan, scale-to-zero) + Azure SQL (serverless, auto-pause) + Azure Static Web Apps (frontend, free tier). `.github/workflows/deploy.yml` builds and pushes all 6 images to ACR, applies EF Core migrations, re-applies the Bicep template with the new image tags, and deploys the frontend — triggered on every push to `main` via GitHub OIDC (no stored Azure credentials).

First-time setup (provisioning the resource group, wiring GitHub OIDC, and the secrets `deploy.yml` expects) is a manual one-off — see the project's deployment notes for the exact `az` commands.

## Gateway route prefixes

| Prefix | Target |
|---|---|
| `/identity/*` | IdentityService |
| `/package/*` | PackageService |
| `/shipment/*` | ShipmentService |
| `/tracking/*` | TrackingService |
| `/driver/*` | DriverService |

Example: `GET http://localhost:5000/package/api/packages`. The frontend's `environment.apiBaseUrl` points at the gateway (`http://localhost:5000`); SignalR connects directly to `http://localhost:5000/tracking/hubs/tracking`.


-----------login credentials-----------

Email: admin@packagedelivery.local
Password: Dev_Admin_Passw0rd!