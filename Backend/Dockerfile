# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/RTSErp.Api/RTSErp.Api.csproj",                             "src/RTSErp.Api/"]
COPY ["src/RTSErp.Application/RTSErp.Application.csproj",             "src/RTSErp.Application/"]
COPY ["src/RTSErp.Domain/RTSErp.Domain.csproj",                       "src/RTSErp.Domain/"]
COPY ["src/RTSErp.Infrastructure/RTSErp.Infrastructure.csproj",       "src/RTSErp.Infrastructure/"]
COPY ["src/RTSErp.Shared/RTSErp.Shared.csproj",                       "src/RTSErp.Shared/"]
COPY ["tests/",                                                         "tests/"]

RUN dotnet restore "src/RTSErp.Api/RTSErp.Api.csproj"

COPY . .
RUN dotnet publish "src/RTSErp.Api/RTSErp.Api.csproj" -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

# Railway and Render use PORT env var; default to 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "RTSErp.Api.dll"]
