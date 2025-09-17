FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenWorker.DistrictServer/OpenWorker.DistrictServer.csproj", "OpenWorker.DistrictServer/"]
RUN dotnet restore "OpenWorker.DistrictServer/OpenWorker.DistrictServer.csproj"
COPY . .
WORKDIR "/src/OpenWorker.DistrictServer"
RUN dotnet build "OpenWorker.DistrictServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OpenWorker.DistrictServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenWorker.DistrictServer.dll"]
