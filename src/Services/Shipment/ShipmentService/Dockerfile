FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip

COPY nuget.config Directory.Build.props ./

# Restore layer (cached when only source changes)
COPY src/BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj src/BuildingBlocks/BuildingBlocks/
COPY src/Services/Shipment/ShipmentService.Domain/ShipmentService.Domain.csproj src/Services/Shipment/ShipmentService.Domain/
COPY src/Services/Shipment/ShipmentService.Application/ShipmentService.Application.csproj src/Services/Shipment/ShipmentService.Application/
COPY src/Services/Shipment/ShipmentService.Infrastructure/ShipmentService.Infrastructure.csproj src/Services/Shipment/ShipmentService.Infrastructure/
COPY src/Services/Package/PackageService/Protos/package_validation.proto src/Services/Package/PackageService/Protos/
COPY src/Services/Shipment/ShipmentService/ShipmentService.csproj src/Services/Shipment/ShipmentService/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "src/Services/Shipment/ShipmentService/ShipmentService.csproj" --configfile nuget.config || \
    sleep 15 && dotnet restore "src/Services/Shipment/ShipmentService/ShipmentService.csproj" --configfile nuget.config || \
    sleep 30 && dotnet restore "src/Services/Shipment/ShipmentService/ShipmentService.csproj" --configfile nuget.config || \
    sleep 45 && dotnet restore "src/Services/Shipment/ShipmentService/ShipmentService.csproj" --configfile nuget.config || \
    sleep 60 && dotnet restore "src/Services/Shipment/ShipmentService/ShipmentService.csproj" --configfile nuget.config

COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "src/Services/Shipment/ShipmentService/ShipmentService.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5003
EXPOSE 5003
ENTRYPOINT ["dotnet", "ShipmentService.dll"]
