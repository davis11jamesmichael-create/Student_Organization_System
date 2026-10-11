# Technology Stack — Student Organization System

## Runtime & Framework

| Component | Version |
|-----------|---------|
| .NET | 8.0 |
| ASP.NET Core MVC | 8.0 |
| Target Framework | net8.0 |
| C# | 12 (implicit with .NET 8) |
| Nullable reference types | Enabled |
| Implicit usings | Enabled |

## Database

| Component | Detail |
|-----------|--------|
| Database | MySQL 8.0.36 (fixed server version) |
| ORM | Entity Framework Core 8 |
| EF Core MySQL driver | Pomelo.EntityFrameworkCore.MySql 8.0.3 |
| Database name | StudentOrganizationDB |
| Connection | localhost:3306, configured in appsettings.Development.json |

## NuGet Dependencies (main project)

| Package | Version | Purpose |
|---------|---------|---------|
| Pomelo.EntityFrameworkCore.MySql | 8.0.3 | MySQL provider for EF Core |
| Microsoft.EntityFrameworkCore.Design | 8.0.20 | EF Core migrations tooling (dev-only) |
| Microsoft.Extensions.Identity.Core | 8.0.20 | PasswordHasher<T> |

## Test Project (StudentOrganizationSystem.Tests)

- Framework: xUnit
- In-memory database: SQLite `:memory:` (for unique index enforcement tests)
- Test count: 35 tests, all green
- Test files:
  - `ModelRulesTests.cs` — model validation rules
  - `DatabaseConstraintTests.cs` — unique index enforcement via SQLite
  - `ListFilterTests.cs` — filter/search logic
  - `SecurityHardeningTests.cs` — security-related tests
  - `AnnouncementTests.cs` — announcement model tests
  - `AvatarUploadTests.cs` — avatar upload service tests
  - `TestDb.cs` — shared SQLite test database helper

## Frontend

| Component | Detail |
|-----------|--------|
| Templating | Razor (.cshtml) |
| CSS | Custom modular CSS (no framework like Bootstrap) |
| JavaScript | Vanilla JS + jQuery (vendored) |
| jQuery | Vendored in wwwroot/lib/jquery.min.js |
| jQuery Validation | Vendored in wwwroot/lib/jquery.validate.min.js |
| jQuery Validation Unobtrusive | Vendored in wwwroot/lib/jquery.validate.unobtrusive.min.js |
| Icons | CSS-based / emoji icons (no icon font CDN) |

## Authentication & Security

| Feature | Implementation |
|---------|---------------|
| Authentication | ASP.NET Core Cookie Authentication |
| Password hashing | `IPasswordHasher<User>` (ASP.NET Core Identity hasher, standalone) |
| Session duration | 8 hours, sliding expiration |
| Security stamp | Per-request validation in `OnValidatePrincipal` cookie event |
| Brute-force guard | `LoginAttemptTracker` using `IMemoryCache` |
| MustChangePassword | Middleware redirect enforced before any page except change-password |
| Authorization | `[Authorize]` + `[Authorize(Roles = "Admin")]` attributes |

## Development Commands

```bash
# Run the application
dotnet run

# Run with Development environment (loads appsettings.Development.json)
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Run tests
dotnet test

# Add a new EF Core migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

## Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Committed base config; placeholder password `YOUR_PASSWORD` |
| `appsettings.Development.json` | Git-ignored; holds real DB password and dev overrides |
| `Properties/launchSettings.json` | Local dev launch profiles |
| `.gitignore` | Excludes bin/, obj/, appsettings.Development.json, uploads/ |

## Environment Behavior

| Environment | Behavior |
|-------------|---------|
| Development | Developer exception page, sample data seeded, verbose logging |
| Production | `/Home/Error` exception handler, HSTS, no sample data seeded |

## Key Middleware Pipeline Order

1. Exception handling (Developer page or `/Home/Error`)
2. Status code pages (`UseStatusCodePagesWithReExecute`)
3. HTTPS redirection
4. Static files
5. Routing
6. Authentication
7. MustChangePassword redirect middleware (custom)
8. Authorization
9. MVC route mapping (`{controller=Home}/{action=Index}/{id?}`)
