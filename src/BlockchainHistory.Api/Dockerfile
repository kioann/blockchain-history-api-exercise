# Build context is the repository root:  docker build -f src/BlockchainHistory.Api/Dockerfile .

# ---- build: full SDK image, used only to compile ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from project files only, so this layer is cached until a dependency changes.
COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/BlockchainHistory.Domain/BlockchainHistory.Domain.csproj src/BlockchainHistory.Domain/
COPY src/BlockchainHistory.Application/BlockchainHistory.Application.csproj src/BlockchainHistory.Application/
COPY src/BlockchainHistory.Infrastructure/BlockchainHistory.Infrastructure.csproj src/BlockchainHistory.Infrastructure/
COPY src/BlockchainHistory.Api/BlockchainHistory.Api.csproj src/BlockchainHistory.Api/
RUN dotnet restore src/BlockchainHistory.Api/BlockchainHistory.Api.csproj

COPY src/ src/
RUN dotnet publish src/BlockchainHistory.Api/BlockchainHistory.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime: small ASP.NET image, no SDK, runs as a non-root user ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The SQLite file lives on a volume so history survives container restarts.
RUN mkdir -p /app/data && chown "$APP_UID" /app/data
VOLUME /app/data

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__BlockchainHistory="Data Source=/app/data/blockchain-history.db"
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "BlockchainHistory.Api.dll"]
