# Dockerfile for running database migrations as an init container
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["UserManagement.Migrations/UserManagement.Migrations.csproj", "UserManagement.Migrations/"]
COPY ["UserManagement.Data/UserManagement.Data.csproj", "UserManagement.Data/"]
COPY ["UserManagement.Shared/UserManagement.Shared.csproj", "UserManagement.Shared/"]

# Restore dependencies
RUN dotnet restore "UserManagement.Migrations/UserManagement.Migrations.csproj"

# Copy source code
COPY UserManagement.Migrations/ UserManagement.Migrations/
COPY UserManagement.Data/ UserManagement.Data/
COPY UserManagement.Shared/ UserManagement.Shared/

# Build
WORKDIR "/src/UserManagement.Migrations"
RUN dotnet build "UserManagement.Migrations.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "UserManagement.Migrations.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "UserManagement.Migrations.dll"]
