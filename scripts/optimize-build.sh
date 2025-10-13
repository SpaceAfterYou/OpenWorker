#!/bin/bash

# OpenWorker Build Optimization Script
# This script optimizes the build process for better performance

set -e

echo "🚀 Starting OpenWorker build optimization..."

# Set environment variables for optimal build performance
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
export NUGET_XMLDOC_MODE=skip

# Clean previous builds
echo "🧹 Cleaning previous builds..."
dotnet clean --configuration Release --verbosity minimal

# Restore packages with optimizations
echo "📦 Restoring packages with optimizations..."
dotnet restore --locked-mode --use-lock-file --verbosity minimal

# Build with optimizations
echo "🔨 Building with Release optimizations..."
dotnet build \
    --configuration Release \
    --no-restore \
    --verbosity minimal \
    /p:TreatWarningsAsErrors=true \
    /p:WarningsAsErrors="" \
    /p:PublishTrimmed=true \
    /p:PublishReadyToRun=true

# Run tests if available
if [ -d "OpenWorker.AuthServer.Test" ] || [ -d "OpenWorker.Hotspot.Test" ]; then
    echo "🧪 Running tests..."
    dotnet test \
        --configuration Release \
        --no-build \
        --verbosity minimal \
        --logger "console;verbosity=minimal"
fi

# Publish optimized builds
echo "📦 Publishing optimized builds..."

# List of server projects to publish
SERVERS=(
    "OpenWorker.AuthServer"
    "OpenWorker.RelayServer"
    "OpenWorker.GateServer"
    "OpenWorker.DistrictServer"
    "OpenWorker.MazeServer"
)

for server in "${SERVERS[@]}"; do
    if [ -d "$server" ]; then
        echo "📦 Publishing $server..."
        dotnet publish "$server" \
            --configuration Release \
            --no-build \
            --output "publish/$server" \
            /p:PublishTrimmed=true \
            /p:PublishReadyToRun=true \
            /p:PublishSingleFile=false \
            /p:IncludeNativeLibrariesForSelfExtract=true
    fi
done

echo "✅ Build optimization complete!"
echo "📊 Build artifacts are available in the 'publish' directory"

# Display build summary
echo ""
echo "📈 Build Summary:"
echo "=================="
for server in "${SERVERS[@]}"; do
    if [ -d "publish/$server" ]; then
        size=$(du -sh "publish/$server" | cut -f1)
        echo "$server: $size"
    fi
done

echo ""
echo "🎯 Performance Tips:"
echo "==================="
echo "1. Use the appsettings.Performance.json for production deployments"
echo "2. Monitor memory usage with the built-in PerformanceMonitoringService"
echo "3. Consider using ReadyToRun images for faster startup times"
echo "4. Enable tiered compilation in production (already configured)"
echo "5. Use Alpine-based Docker images for smaller container sizes"