FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base
USER $APP_UID
WORKDIR /app
# Install culture support for better performance
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies with better caching
COPY ["Directory.Build.props", "./"]
COPY ["OpenWorker.AuthServer/OpenWorker.AuthServer.csproj", "OpenWorker.AuthServer/"]
COPY ["OpenWorker.AuthServer.Gameplay/OpenWorker.AuthServer.Gameplay.csproj", "OpenWorker.AuthServer.Gameplay/"]
COPY ["OpenWorker.DependencyInjection/OpenWorker.DependencyInjection.csproj", "OpenWorker.DependencyInjection/"]
COPY ["OpenWorker.Hotspot/OpenWorker.Hotspot.csproj", "OpenWorker.Hotspot/"]
COPY ["OpenWorker.Domain/OpenWorker.Domain.csproj", "OpenWorker.Domain/"]
COPY ["OpenWorker.Persistence/OpenWorker.Persistence.csproj", "OpenWorker.Persistence/"]
COPY ["OpenWorker.Batch/OpenWorker.Batch.csproj", "OpenWorker.Batch/"]
COPY ["OpenWorker.Extensions/OpenWorker.Extensions.csproj", "OpenWorker.Extensions/"]

RUN dotnet restore "OpenWorker.AuthServer/OpenWorker.AuthServer.csproj" --runtime alpine-x64

COPY . .
WORKDIR "/src/OpenWorker.AuthServer"
RUN dotnet build "OpenWorker.AuthServer.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OpenWorker.AuthServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
    --no-restore \
    --runtime alpine-x64 \
    --self-contained false \
    /p:PublishTrimmed=true \
    /p:PublishReadyToRun=true \
    /p:PublishSingleFile=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenWorker.AuthServer.dll"]
