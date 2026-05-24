# Package Delivery System - Microservices Backend

This solution contains a starter microservices backend for a package delivery platform using ASP.NET.

## Services

- `ApiGateway` (YARP reverse proxy) - `http://localhost:5000`
- `IdentityService` - `http://localhost:5001`
- `PackageService` - `http://localhost:5002`
- `ShipmentService` - `http://localhost:5003`
- `TrackingService` - `http://localhost:5004`
- `DriverService` - `http://localhost:5005`

## Project Structure

- `src/ApiGateway/ApiGateway` - API Gateway routes
- `src/Services/Identity/IdentityService` - Identity endpoints
- `src/Services/Package/PackageService` - Package endpoints
- `src/Services/Shipment/ShipmentService` - Shipment endpoints
- `src/Services/Tracking/TrackingService` - Tracking endpoints
- `src/Services/Driver/DriverService` - Driver endpoints
- `src/BuildingBlocks/BuildingBlocks` - shared service defaults

## Run

Build all projects:

```bash
dotnet build PackageDeliverySystem.sln
```

Run each service in separate terminals:

```bash
dotnet run --project src/Services/Identity/IdentityService
dotnet run --project src/Services/Package/PackageService
dotnet run --project src/Services/Shipment/ShipmentService
dotnet run --project src/Services/Tracking/TrackingService
dotnet run --project src/Services/Driver/DriverService
dotnet run --project src/ApiGateway/ApiGateway
```

## Gateway Route Prefixes

- `/identity/*` -> Identity Service
- `/package/*` -> Package Service
- `/shipment/*` -> Shipment Service
- `/tracking/*` -> Tracking Service
- `/driver/*` -> Driver Service

Example:

- `GET http://localhost:5000/package/api/packages`

## Notes About ASP.NET 10

Your machine currently has .NET 8 SDK installed. This starter is built on `net8.0` so it runs now.
When .NET 10 SDK is installed, you can migrate by changing each project `TargetFramework` to `net10.0`.
