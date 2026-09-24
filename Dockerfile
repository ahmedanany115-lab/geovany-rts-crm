# Root Dockerfile — builds the RTS ERP .NET 8 backend
# Railway uses this when the service root directory is the repo root.
# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Backend/src/RTSErp.Api/RTSErp.Api.csproj",                             "Backend/src/RTSErp.Api/"]
COPY ["Backend/src/RTSErp.Application/RTSErp.Application.csproj",             "Backend/src/RTSErp.Application/"]
COPY ["Backend/src/RTSErp.Domain/RTSErp.Domain.csproj",                       "Backend/src/RTSErp.Domain/"]
COPY ["Backend/src/RTSErp.Infrastructure/RTSErp.Infrastructure.csproj",       "Backend/src/RTSErp.Infrastructure/"]
COPY ["Backend/src/RTSErp.Shared/RTSErp.Shared.csproj",                       "Backend/src/RTSErp.Shared/"]

RUN dotnet restore "Backend/src/RTSErp.Api/RTSErp.Api.csproj"

COPY Backend/src/ Backend/src/
RUN dotnet publish "Backend/src/RTSErp.Api/RTSErp.Api.csproj" -c Release -o /app/publish --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "RTSErp.Api.dll"]
