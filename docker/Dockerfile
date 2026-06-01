# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /build

# Copy solution and project files
COPY ["FileService.sln", "./"]
COPY ["src/FileService.API/FileService.API.csproj", "src/FileService.API/"]
COPY ["src/FileService.Application/FileService.Application.csproj", "src/FileService.Application/"]
COPY ["src/FileService.Domain/FileService.Domain.csproj", "src/FileService.Domain/"]
COPY ["src/FileService.Infrastructure/FileService.Infrastructure.csproj", "src/FileService.Infrastructure/"]
COPY ["src/FileService.Contracts/FileService.Contracts.csproj", "src/FileService.Contracts/"]
COPY ["src/FileService.Shared/FileService.Shared.csproj", "src/FileService.Shared/"]
COPY ["Directory.Build.props", "./"]

# Restore
RUN dotnet restore "src/FileService.API/FileService.API.csproj"

# Copy remaining source
COPY ["src/", "src/"]

# Build
RUN dotnet build "src/FileService.API/FileService.API.csproj" -c Release --no-restore

# Publish
RUN dotnet publish "src/FileService.API/FileService.API.csproj" \
    -c Release \
    -o /publish \
    --no-restore \
    --no-build \
    /p:UseAppHost=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Copy published files
COPY --chown=appuser:appgroup --from=builder /publish .

USER appuser

# Expose default ASP.NET Core port
EXPOSE 8080

# Healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "FileService.API.dll"]
