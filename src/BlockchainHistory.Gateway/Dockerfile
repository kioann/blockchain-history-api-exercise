# Build context is the repository root:  docker build -f src/BlockchainHistory.Gateway/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json nuget.config .editorconfig Directory.Build.props Directory.Packages.props ./
COPY src/BlockchainHistory.Gateway/BlockchainHistory.Gateway.csproj src/BlockchainHistory.Gateway/
RUN dotnet restore src/BlockchainHistory.Gateway/BlockchainHistory.Gateway.csproj

COPY src/BlockchainHistory.Gateway/ src/BlockchainHistory.Gateway/
RUN dotnet publish src/BlockchainHistory.Gateway/BlockchainHistory.Gateway.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "BlockchainHistory.Gateway.dll"]
