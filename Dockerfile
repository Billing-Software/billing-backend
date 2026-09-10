# Build stage — pinned minor for reproducible, patched builds
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["BillingBackend/BillingBackend.csproj", "BillingBackend/"]
COPY ["BillingBackend.Data/BillingBackend.Data.csproj", "BillingBackend.Data/"]
RUN dotnet restore "BillingBackend/BillingBackend.csproj"

# Copy all source files and publish
COPY . .
WORKDIR "/src/BillingBackend"
RUN dotnet publish "BillingBackend.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Wallet is NOT baked into the image. Mount at runtime: -v ./wallet:/app/wallet:ro
# and set TNS_ADMIN=/app/wallet + ConnectionStrings__DefaultConnection via env.
ENV TNS_ADMIN=/app/wallet
ENV ASPNETCORE_URLS=http://+:8080

# Run as non-root (SDK base images ship an 'app' user)
USER app

# Expose container port (ASP.NET default for .NET 8+)
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "BillingBackend.dll"]
