FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenWorker.UpdateContent/OpenWorker.UpdateContent.csproj", "OpenWorker.UpdateContent/"]
RUN dotnet restore "OpenWorker.UpdateContent/OpenWorker.UpdateContent.csproj"
COPY . .
WORKDIR "/src/OpenWorker.UpdateContent"
RUN dotnet build "OpenWorker.UpdateContent.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OpenWorker.UpdateContent.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenWorker.UpdateContent.dll"]
