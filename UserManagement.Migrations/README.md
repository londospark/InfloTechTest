# UserManagement.Migrations

Database migration tool for the UserManagement application.

## Overview

This project is a dedicated console application that applies Entity Framework Core migrations to the database. It's designed to run as a separate process before the API starts, ensuring the database schema is always up to date.

## Features

- ? Applies pending migrations automatically
- ? Includes retry logic for transient database failures
- ? Provides detailed console logging
- ? Verifies database connection after migration
- ? Returns proper exit codes (0 = success, 1 = failure)
- ? Masks sensitive connection string information in logs
- ? Configurable timeout (5 minutes default)

## Usage

### With .NET Aspire AppHost (Recommended)

The migration tool is automatically orchestrated by the AppHost:

```csharp
// In UserManagement.AppHost/Program.cs
var migrations = builder.AddProject<Projects.UserManagement_Migrations>("Migrations")
    .WithReference(database)
    .WaitFor(database);

var api = builder.AddProject<Projects.UserManagement_Web>("API")
    .WithReference(database)
    .WaitFor(migrations); // API waits for migrations to complete
```

Simply run the AppHost:

```bash
dotnet run --project UserManagement.AppHost
```

### Standalone Execution

You can also run the migration tool independently:

```bash
# Set connection string via environment variable
$env:ConnectionStrings__UserManagement="Server=...;Database=UserManagement;..."

# Run migrations
dotnet run --project UserManagement.Migrations
```

Or via appsettings.json:

```json
{
  "ConnectionStrings": {
    "UserManagement": "Server=...;Database=UserManagement;..."
  }
}
```

### Docker/Kubernetes Init Container

Build the Docker image:

```bash
docker build -f UserManagement.Migrations/Dockerfile -t usermanagement-migrations:latest .
```

Run as init container:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: api-pod
spec:
  initContainers:
  - name: db-migrations
    image: usermanagement-migrations:latest
    env:
    - name: ConnectionStrings__UserManagement
      valueFrom:
        secretKeyRef:
          name: db-secret
          key: connection-string
  containers:
  - name: api
    image: usermanagement-api:latest
```

## Configuration

The tool accepts configuration via:

1. **appsettings.json** (default)
2. **Environment variables** (overrides appsettings)
3. **Command-line arguments**

### Connection String

Required configuration key: `ConnectionStrings:UserManagement`

Environment variable: `ConnectionStrings__UserManagement` (note the double underscore)

## Exit Codes

- **0**: Success - migrations applied successfully
- **1**: Failure - error occurred during migration

## Logging

The tool outputs detailed logs to the console:

```
=== Database Migration Tool ===
Connection string: Server=localhost;Database=UserManagement;Password=****;...
Applying database migrations...
Found 2 pending migration(s):
  - 20240101000000_InitialCreate
  - 20240102000000_AddUserLogs
Applying migrations...
? Migrations applied successfully!
? Database connection verified.
=== Migration completed successfully ===
```

## Integration with CI/CD

### GitHub Actions

```yaml
- name: Run Database Migrations
  run: dotnet run --project UserManagement.Migrations
  env:
    ConnectionStrings__UserManagement: ${{ secrets.DB_CONNECTION_STRING }}
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Apply Database Migrations'
  inputs:
    command: 'run'
    projects: 'UserManagement.Migrations/UserManagement.Migrations.csproj'
  env:
    ConnectionStrings__UserManagement: $(DbConnectionString)
```

## Retry Logic

The tool includes automatic retry logic for transient failures:

- **Max retries**: 5 attempts
- **Max delay**: 30 seconds between retries
- **Command timeout**: 5 minutes (300 seconds)

## Security Considerations

- Connection strings with passwords are masked in console output
- Use Azure Key Vault or Kubernetes secrets for production
- The tool requires database owner/migration permissions
- Consider using managed identities in Azure for passwordless authentication

## Troubleshooting

### "Connection string not found"
Ensure the `ConnectionStrings:UserManagement` configuration key is set.

### "Cannot connect to database"
- Verify the connection string is correct
- Check network connectivity to the database server
- Ensure the database server is running
- Verify firewall rules allow connections

### "Migration failed"
- Check the console output for specific error messages
- Verify the migration files exist in the Data project
- Ensure the database user has sufficient permissions
- Review pending migrations with: `dotnet ef migrations list`

## Development

### Adding New Migrations

Migrations are created in the `UserManagement.Data` project:

```bash
dotnet ef migrations add YourMigrationName --project UserManagement.Data --startup-project UserManagement.Web
```

The migration tool will automatically apply new migrations on the next run.

### Testing Migrations

Test the migration tool locally:

```bash
# Start a local SQL Server instance
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# Run migrations
$env:ConnectionStrings__UserManagement="Server=localhost,1433;Database=UserManagement;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
dotnet run --project UserManagement.Migrations
```

## Dependencies

- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Design
- Microsoft.Extensions.Hosting
- UserManagement.Data (contains DbContext and migrations)

## License

Same as the parent UserManagement application.
