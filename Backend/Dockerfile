# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY ["RTSErp.sln", "."]
COPY ["src/RTSErp.Api/RTSErp.Api.csproj",              "src/RTSErp.Api/"]
COPY ["src/RTSErp.Application/RTSErp.Application.csproj", "src/RTSErp.Application/"]
COPY ["src/RTSErp.Domain/RTSErp.Domain.csproj",         "src/RTSErp.Domain/"]
COPY ["src/RTSErp.Infrastructure/RTSErp.Infrastructure.csproj", "src/RTSErp.Infrastructure/"]
COPY ["src/RTSErp.Shared/RTSErp.Shared.csproj",         "src/RTSErp.Shared/"]

RUN dotnet restore "src/RTSErp.Api/RTSErp.Api.csproj"

# Copy everything else and publish
COPY . .
WORKDIR /src/src/RTSErp.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

# Port 8080 is the Railway/Render/Azure default
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RTSErp.Api.dll"]
