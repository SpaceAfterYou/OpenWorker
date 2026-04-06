FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenWorker.AuthServer/OpenWorker.AuthServer.csproj", "OpenWorker.AuthServer/"]
RUN dotnet restore "OpenWorker.AuthServer/OpenWorker.AuthServer.csproj"
COPY . .
WORKDIR "/src/OpenWorker.AuthServer"
RUN dotnet build "OpenWorker.AuthServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OpenWorker.AuthServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenWorker.AuthServer.dll"]
