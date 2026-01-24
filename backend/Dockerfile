# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Change to project directory and restore dependencies
WORKDIR /src/TenderTracker.API
RUN dotnet restore

# Build and publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 80

# Install curl for health checks (optional)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# List files for debugging
RUN ls -la /app && echo "Checking for DLL..." && find /app -name "*.dll" -type f

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

# Debug: show .NET info
RUN dotnet --info

ENTRYPOINT ["dotnet", "TenderTracker.API.dll"]
