# Multi-stage build for ErpClink.Api (Easy Moon Laser Clinic)
# Build context MUST be the repository root.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/ ./src/

RUN dotnet restore src/Host/ErpClink.Api/ErpClink.Api.csproj
RUN dotnet publish src/Host/ErpClink.Api/ErpClink.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build /app/publish .

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

# Render injects PORT; bind to 0.0.0.0 so the service is reachable.
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}; exec dotnet ErpClink.Api.dll"]
