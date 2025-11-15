# User Management Technical Exercise

[![codecov](https://codecov.io/gh/londospark/InfloTechTest/branch/main/graph/badge.svg)](https://codecov.io/gh/londospark/InfloTechTest)

A modern full-stack user management application built with .NET 10, featuring a Blazor Web App UI, ASP.NET Core Web API, Entity Framework Core, SignalR for real-time updates, and comprehensive field-level audit logging.

## ✨ Features

- 🎨 **Modern Blazor Web App** with interactive server and client components
- 🔌 **RESTful API** with Scalar (OpenAPI) documentation
- 📊 **Real-time updates** using SignalR for activity logs
- 📝 **Detailed audit logging** with field-level change tracking
- ✅ **Comprehensive test coverage** with unit and integration tests
- 🐳 **Containerized** with Docker support via .NET Aspire
- 🗄️ **SQL Server database** with EF Core migrations

## 📋 Requirements

Before running the application, ensure you have the following installed:

### Required
- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** - Latest version
- **[Docker Desktop](https://www.docker.com/products/docker-desktop)** - For running SQL Server and other dependencies via .NET Aspire
- **WASM Build Tools** - Install the WebAssembly workload:
  ```bash
  dotnet workload install wasm-tools
  ```

### Recommended
- **[Visual Studio 2025](https://visualstudio.microsoft.com/downloads)** (17.13 or later) with the following workloads:
  - ASP.NET and web development
  - .NET Aspire
  - Azure development (optional)
- **OR [Visual Studio Code](https://code.visualstudio.com/)** with:
  - C# Dev Kit extension
  - .NET Aspire extension

### Optional
- **[Git](https://git-scm.com/downloads)** - For source control
- **[SQL Server Management Studio](https://aka.ms/ssmsfullsetup)** or **[Azure Data Studio](https://aka.ms/azuredatastudio)** - For database management

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/londospark/InfloTechTest.git
cd InfloTechTest
```

### 2. Ensure Docker is Running

Make sure Docker Desktop is running before starting the application. .NET Aspire uses Docker to manage dependencies like SQL Server.

### 3. Run the Application

#### Option A: Using Visual Studio 2025

1. Open `UserManagement.sln` in Visual Studio 2025
2. Set `UserManagement.AppHost` as the startup project
3. Press **F5** or click the **Run** button
4. The .NET Aspire dashboard will open automatically

#### Option B: Using Command Line

```bash
cd UserManagement.AppHost
dotnet run
```

### 4. Access the Application

Once the AppHost is running, the .NET Aspire dashboard will open. From there:

- **📱 Blazor Web App**: Click the **"Home"** link under the `Blazor` resource
  - Default URL: `https://localhost:7183` (or the port shown in the dashboard)
  - This is the main Blazor application UI for user management

- **📚 API Reference (Scalar)**: Click the **"Scalar API Reference"** link under the `API` resource  
  - Default URL: `https://localhost:7084/scalar` (or the port shown in the dashboard)
  - Interactive API documentation with testing capabilities

- **🌐 Web API**: Click the **"Home"** link under the `API` resource
  - Default URL: `https://localhost:7084` (or the port shown in the dashboard)
  - Direct access to the RESTful API endpoints

- **📊 .NET Aspire Dashboard**: Monitor logs, traces, and metrics
  - Default URL: `https://localhost:17241` (or the port shown)
  - Real-time application telemetry and diagnostics

> **Note**: .NET Aspire automatically assigns ports and manages service discovery. The actual ports may differ from the defaults shown above. Always check the Aspire dashboard for the current port assignments.

## ⚡ Performance Note

> **Important**: Performance in **Debug** mode is significantly slower due to additional diagnostics and lack of optimizations. For the best performance experience, run the application in **Release** mode:

### Visual Studio
- Change the build configuration from **Debug** to **Release** in the toolbar dropdown
- Press **F5** or click **Run**

### Command Line
```bash
dotnet run --configuration Release
```

**Release mode benefits**:
- ~3-5x faster page load times
- Optimized JavaScript/WASM bundles
- Reduced memory footprint
- Better overall responsiveness

## 🏗️ Project Structure

```
UserManagement/
├── UserManagement.AppHost/          # .NET Aspire orchestration (AppHost)
├── UserManagement.ServiceDefaults/  # Shared service configuration
├── UserManagement.Blazor/           # Blazor Web App (UI frontend)
├── UserManagement.Web/              # ASP.NET Core Web API (backend)
├── UserManagement.Data/             # Entity Framework Core data layer
├── UserManagement.Services/         # Business logic layer
├── UserManagement.Shared/           # Shared DTOs and models
├── UserManagement.Migrations/       # EF Core migration runner
└── *.Tests/                         # Unit and integration tests
```

### Resource Names in .NET Aspire

When viewing the Aspire dashboard, you'll see these resources:

| Resource Name | Description | Key Endpoints |
|--------------|-------------|---------------|
| **Blazor** | Blazor Web App frontend | Home (UI) |
| **API** | ASP.NET Core Web API | Home (API), Scalar API Reference |
| **Migrations** | Database migration runner | (Runs once on startup) |
| **SQL-Server** | SQL Server 2022 container | (Internal use) |
| **UserManagement** | SQL Server database | (Accessed via connection string) |

## 🧪 Running Tests

### All Tests
```bash
dotnet test
```

### Specific Test Project
```bash
dotnet test UserManagement.Web.Tests
dotnet test UserManagement.Blazor.Tests
dotnet test UserManagement.Services.Tests
dotnet test UserManagement.Data.Tests
```

### With Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```
- 
### API Documentation

The API is fully documented using OpenAPI/Swagger with Scalar UI:

- **Interactive Documentation**: Access via the **"Scalar API Reference"** link in the Aspire dashboard
  - Direct URL: `https://localhost:7084/scalar` (port may vary)
  - Modern, interactive API documentation interface
- **OpenAPI JSON**: Available at `https://localhost:7084/openapi/v1.json`
- **Swagger UI**: Alternative interface at `https://localhost:7084/swagger`

**Features**:
- All endpoints with request/response schemas
- Try-it-out functionality for testing APIs
- Example requests and responses
- Authentication requirements
- Real-time API testing

## 🔍 Database

The application uses **SQL Server** running in Docker via .NET Aspire orchestration:

- **Provider**: Microsoft SQL Server 2022
- **Migrations**: Automatic on startup via `UserManagement.Migrations` project
- **Connection String**: Configured via .NET Aspire service discovery
- **Persistence**: Data is persisted in a Docker volume

### Database Migrations

```bash
# Add a new migration
cd UserManagement.Migrations
dotnet ef migrations add MigrationName --startup-project ../UserManagement.AppHost

# Update database manually (optional - auto-applied on startup)
dotnet ef database update --startup-project ../UserManagement.AppHost
```

## 🐛 Troubleshooting

### Docker Issues
- Ensure Docker Desktop is running
- Check Docker has sufficient resources (4GB+ RAM recommended)
- Restart Docker Desktop if containers fail to start

### Port Conflicts
- .NET Aspire will automatically assign available ports
- Check the .NET Aspire dashboard for actual port assignments

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build --no-incremental

# Restore workloads
dotnet workload restore
```

### WASM Build Issues
```bash
# Reinstall WASM tools
dotnet workload install wasm-tools --skip-manifest-update
```
## 📝 Original Exercise Requirements

<details>
<summary>Click to expand original exercise tasks</summary>

### The Exercise
Complete as many of the tasks below as you feel comfortable with. These are split into 4 levels of difficulty 
* **Standard** - Functionality that is common when working as a web developer
* **Advanced** - Slightly more technical tasks and problem solving
* **Expert** - Tasks with a higher level of problem solving and architecture needed
* **Platform** - Tasks with a focus on infrastructure and scaleability, rather than application development.

### 1. Filters Section (Standard) ✅

The users page contains 3 buttons below the user listing - **Show All**, **Active Only** and **Non Active**. Show All has already been implemented. Implement the remaining buttons using the following logic:
* Active Only – This should show only users where their `IsActive` property is set to `true`
* Non Active – This should show only users where their `IsActive` property is set to `false`

**Status**: ✅ Completed - Implemented with API endpoints and UI filters

### 2. User Model Properties (Standard) ✅

Add a new property to the `User` class in the system called `DateOfBirth` which is to be used and displayed in relevant sections of the app.

**Status**: ✅ Completed - Added with validation and database migration

### 3. Actions Section (Standard) ✅

Create the code and UI flows for the following actions
* **Add** – A screen that allows you to create a new user and return to the list
* **View** - A screen that displays the information about a user
* **Edit** – A screen that allows you to edit a selected user from the list  
* **Delete** – A screen that allows you to delete a selected user from the list

**Status**: ✅ Completed - Full CRUD operations with validation

### 4. Data Logging (Advanced) ✅

Extend the system to capture log information regarding primary actions performed on each user in the app.
* In the **View** screen there should be a list of all actions that have been performed against that user. 
* There should be a new **Logs** page, containing a list of log entries across the application.
* In the Logs page, the user should be able to click into each entry to see more detail about it.
* In the Logs page, think about how you can provide a good user experience - even when there are many log entries.

**Status**: ✅ Completed with enhancements:
- Field-level change tracking (shows old → new values)
- Real-time log updates via SignalR
- Paginated log display
- Activity logs component with live updates

### 5. Extend the Application (Expert) ✅

Make a significant architectural change that improves the application.

**Status**: ✅ Completed - Multiple improvements:
- ✅ Blazor Web App with interactive server + WASM
- ✅ Async operations throughout
- ✅ SQL Server with EF Core migrations
- ✅ Comprehensive test coverage (66+ tests)
- ✅ Real-time updates with SignalR
- ✅ OpenAPI/Scalar documentation

### 6. Future-Proof the Application (Platform) ⏳

Add additional layers to the application that will ensure that it is scaleable with many users or developers.

**Status**: ⏳ Partially completed:
- ✅ .NET Aspire for orchestration
- ✅ Docker containerization
- ✅ Service discovery and configuration
- ✅ Structured logging and telemetry
- ⏳ CI/CD pipelines (GitHub Actions template ready)
- ⏳ IaC for cloud deployment (consider Azure Bicep)

</details>




## 🙏 Acknowledgments

Built with:
- [.NET 10](https://dot.net)
- [Blazor](https://blazor.net)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire)
- [SignalR](https://learn.microsoft.com/aspnet/signalr)
- [Scalar](https://github.com/scalar/scalar)
- [FluentAssertions](https://fluentassertions.com)
- [xUnit](https://xunit.net)
