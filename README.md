# Package Delivery System

A full-stack package delivery / courier platform: a .NET 8 microservices backend and an Angular 19 frontend.

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

## Running locally

### Backend + databases + broker (Docker)

```bash
docker compose up --build
```

This starts SQL Server, RabbitMQ, all five services, and the gateway. Copy `.env.example` to `.env` first and fill in real values for anything beyond local dev (`JWT_KEY` must be at least 32 characters — HS256's minimum). SMTP/Twilio/Stripe can all be left blank; each falls back to a safe no-op/logging mode.

### Backend only, without Docker

```bash
dotnet build PackageDeliverySystem.sln
dotnet run --project src/Services/Identity/IdentityService
dotnet run --project src/Services/Package/PackageService
dotnet run --project src/Services/Shipment/ShipmentService
dotnet run --project src/Services/Tracking/TrackingService
dotnet run --project src/Services/Driver/DriverService
dotnet run --project src/ApiGateway/ApiGateway
```

Each service needs its own SQL Server database reachable via its `ConnectionStrings` setting in `appsettings.Development.json`, and the same `Jwt:Key/Issuer/Audience` across all five (set via user-secrets or environment variables — the checked-in placeholder is intentionally not production-safe).

### Frontend

```bash
cd package-delivery-web
npm install
npm start          # dev server on http://localhost:4200, proxies nothing — calls the gateway directly at http://localhost:5000
```

Or as a container alongside everything else: `docker compose up --build frontend` (served via nginx on port 4200).

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

## Gateway route prefixes

| Prefix | Target |
|---|---|
| `/identity/*` | IdentityService |
| `/package/*` | PackageService |
| `/shipment/*` | ShipmentService |
| `/tracking/*` | TrackingService |
| `/driver/*` | DriverService |

Example: `GET http://localhost:5000/package/api/packages`. The frontend's `environment.apiBaseUrl` points at the gateway (`http://localhost:5000`); SignalR connects directly to `http://localhost:5000/tracking/hubs/tracking`.
