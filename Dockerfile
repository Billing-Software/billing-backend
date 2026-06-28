# Build stage
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

# Copy Oracle wallet files and rewrite sqlnet.ora directory path dynamically for Linux
COPY wallet/ ./wallet/
RUN sed -i 's|DIRECTORY=".*"|DIRECTORY="/app/wallet"|g' ./wallet/sqlnet.ora

# Expose container port (ASP.NET default for .NET 8+)
EXPOSE 8080

ENTRYPOINT ["dotnet", "BillingBackend.dll"]
