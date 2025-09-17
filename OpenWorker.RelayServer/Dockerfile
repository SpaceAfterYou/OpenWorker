FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
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
