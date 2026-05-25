FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip

COPY nuget.config Directory.Build.props ./
COPY . .

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "src/Services/Driver/DriverService/DriverService.csproj" --configfile nuget.config || \
    sleep 15 && dotnet restore "src/Services/Driver/DriverService/DriverService.csproj" --configfile nuget.config || \
    sleep 30 && dotnet restore "src/Services/Driver/DriverService/DriverService.csproj" --configfile nuget.config || \
    sleep 45 && dotnet restore "src/Services/Driver/DriverService/DriverService.csproj" --configfile nuget.config || \
    sleep 60 && dotnet restore "src/Services/Driver/DriverService/DriverService.csproj" --configfile nuget.config

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "src/Services/Driver/DriverService/DriverService.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5005
EXPOSE 5005
ENTRYPOINT ["dotnet", "DriverService.dll"]
