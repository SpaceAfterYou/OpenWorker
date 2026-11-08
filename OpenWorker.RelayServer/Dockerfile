# ASP.NET Core, not the plain runtime: Kestrel serves the health endpoint (/healthz, /readyz).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# curl is what the health check in compose.yaml runs: the image ships no HTTP client, and a probe that starts the
# application a second time every few seconds would cost far more than the few megabytes of curl.
USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["OpenWorker.RelayServer/OpenWorker.RelayServer.csproj", "OpenWorker.RelayServer/"]
RUN dotnet restore "OpenWorker.RelayServer/OpenWorker.RelayServer.csproj"
COPY . .
WORKDIR "/src/OpenWorker.RelayServer"
RUN dotnet build "OpenWorker.RelayServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OpenWorker.RelayServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenWorker.RelayServer.dll"]
