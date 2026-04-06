FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenWorker.AuthServer.Test/OpenWorker.AuthServer.Test.csproj", "OpenWorker.AuthServer.Test/"]
COPY ["OpenWorker.AuthServer.Gameplay/OpenWorker.AuthServer.Gameplay.csproj", "OpenWorker.AuthServer.Gameplay/"]
COPY ["OpenWorker.Hotspot/OpenWorker.Hotspot.csproj", "OpenWorker.Hotspot/"]
COPY ["OpenWorker.Batch/OpenWorker.Batch.csproj", "OpenWorker.Batch/"]
COPY ["OpenWorker.Domain/OpenWorker.Domain.csproj", "OpenWorker.Domain/"]
COPY ["OpenWorker.Res/OpenWorker.Res.csproj", "OpenWorker.Res/"]
COPY ["OpenWorker.Extensions/OpenWorker.Extensions.csproj", "OpenWorker.Extensions/"]
COPY ["OpenWorker.Persistence/OpenWorker.Persistence.csproj", "OpenWorker.Persistence/"]
RUN dotnet restore "OpenWorker.AuthServer.Test/OpenWorker.AuthServer.Test.csproj"
COPY . .
WORKDIR "/src/OpenWorker.AuthServer.Test"
RUN dotnet build "./OpenWorker.AuthServer.Test.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OpenWorker.AuthServer.Test.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "test", "--logger:trx", "OpenWorker.AuthServer.Test.dll"]
