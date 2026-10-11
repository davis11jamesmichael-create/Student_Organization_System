# Project Structure — Student Organization System

## Directory Layout

```
StudentOrganizationSystem/
├── Controllers/          # MVC controllers (one per feature module)
├── Data/                 # EF Core DbContext, seeder, paging helper
├── Migrations/           # EF Core migration history
├── Models/               # Domain entities + view models + enums
├── Properties/           # launchSettings.json
├── Services/             # Application services (auth, security, avatar)
├── StudentOrganizationSystem.Tests/  # xUnit test project (co-located)
├── Views/                # Razor views organized by controller
│   └── Shared/           # Layout, partials (_Layout, _Pager, _BackButton)
├── wwwroot/              # Static assets
│   ├── css/              # Modular CSS files
│   ├── js/               # site.js (single JS file)
│   ├── lib/              # Vendored jQuery + jQuery Validation
│   └── uploads/avatars/  # User-uploaded profile pictures
├── appsettings.json           # Committed config (no real secrets)
├── appsettings.Development.json  # Git-ignored; holds real DB password
├── Program.cs            # App entry point, DI, middleware pipeline
└── StudentOrganizationSystem.csproj
```

## Controllers

| Controller | Responsibility |
|-----------|---------------|
| HomeController | Dashboard, error page |
| AccountController | Login, logout, register, change password, access denied |
| MemberController | Member CRUD, activate/deactivate |
| ActivityController | Activity CRUD |
| AttendanceController | Record and view attendance per activity |
| ParticipationController | Record and view participation per activity |
| MembershipController | View and edit membership standing |
| AnnouncementController | CRUD for announcements |
| ReportsController | Attendance and member reports |
| UserController | Admin user management |
| ProfileController | Avatar upload, profile view |
| PortalController | Member self-service portal |
| SearchController | Global search |

## Models

### Domain Entities
- `Member` — student record (StudentID, FullName, Course, YearLevel, IsActive)
- `User` — login account (Username, PasswordHash, RoleID, MemberID, SecurityStamp, MustChangePassword, AvatarPath)
- `Role` — Admin / Officer / Member
- `Activity` — organization event (Title, Date, Status, Schedule, Instructor)
- `Attendance` — per-member per-activity attendance record (Status: Present/Absent/Excused)
- `Participation` — per-member per-activity participation record (Type free-text)
- `Membership` — one-per-member standing record (Active/Suspended)
- `Announcement` — system announcement with audience targeting

### View Models
- `LoginViewModel`, `RegisterViewModel`, `ChangePasswordViewModel`
- `DashboardViewModel` — aggregated counters for the home page
- `MembershipViewModel` — membership edit form
- `UserFormViewModel` — admin user create/edit form
- `ReportViewModels` — attendance and member report projections
- `SearchResultsViewModel` — combined search results
- `MemberRecords` — member portal history
- `Pagination<T>` — generic paging wrapper

### Enums / Constants
- `ActivityStatus` — Upcoming, Ongoing, Completed, Cancelled
- `AttendanceStatus` — Present, Absent, Excused
- `MembershipStatus` — Active, Suspended
- `AppRoles` — string constants: Admin, Officer, Member
- `AppClaims` — claim type constants (SecurityStamp, MustChangePassword, AvatarPath)
- `ParticipationTypes` — datalist suggestions for participation type
- `MembershipPolicy` — business rules for membership standing
- `AnnouncementAudience` — targeting options

## Data Layer

### ApplicationDbContext
- Inherits `DbContext`
- Configures all unique indexes in `OnModelCreating`:
  - `User.Username` unique
  - `Member.StudentID` unique
  - `Attendance(ActivityID, MemberID)` unique
  - `Participation(ActivityID, MemberID)` unique
  - `Membership.MemberID` unique (one per member)
- All FK relationships use `DeleteBehavior.Restrict`

### DbInitializer
- `InitializeAsync`: applies migrations, seeds roles, seeds first Admin account
- `SeedSampleDataAsync`: dev-only seed (5 members, 3 activities, attendance/participation history, officer account) — never runs in Production
- Returns `InitOutcome` with generated password if auto-generated

### Paging.cs
- `PagedList<T>` — wraps IQueryable with page/size/total metadata
- `_Pager.cshtml` partial renders pagination controls

## Services

| Service | Purpose |
|---------|---------|
| `LoginAttemptTracker` | Brute-force guard; counts failures in IMemoryCache |
| `SecurityStampGuard` | Per-request cookie validation against live DB stamp/role/active flag |
| `MustChangePasswordPolicy` | Defines which paths are accessible before password change |
| `UserClaims` | Builds ClaimsPrincipal from a User entity |
| `AvatarUpload` | Handles profile picture file upload and path management |

## Views Structure

```
Views/
├── Account/     Login, Register, RegisterPending, ChangePassword, AccessDenied
├── Activity/    Index, Create, Edit, Details
├── Announcement/ Index, Create, Edit, Details
├── Attendance/  Index, Record, Details
├── Home/        Index (dashboard), Error
├── Member/      Index, Create, Edit, Details
├── Membership/  Index, Edit
├── Participation/ Index, Record, Details
├── Portal/      Index, History, NotLinked
├── Profile/     Index
├── Reports/     Index, Attendance, Members
├── Search/      Index
├── User/        Index, Create, Edit
└── Shared/      _Layout.cshtml, _Pager.cshtml, _BackButton.cshtml,
                 _ValidationScriptsPartial.cshtml
```

## CSS Architecture (wwwroot/css/)

Modular CSS split by concern:
- `base.css` — CSS custom properties (tokens), app-shell layout (`.app-shell`, `.app-main`, `.app-content`)
- `sidebar.css` — fixed dark sidebar, section groups, active item
- `navbar.css` — top bar (page title, date, user chip, mobile toggle)
- `dashboard.css` — gradient hero banner, stat cards, quick-access cards
- `login.css` — split-screen sign-in layout
- `components.css` — buttons, badges, alerts, icon buttons
- `forms.css` — form controls and layout
- `tables.css` — data tables
- `attendance.css` — attendance-specific styles
- `participation.css` — participation-specific styles
- `print.css` — print media styles
- `responsive.css` — sidebar off-canvas drawer under 1024px

## Architectural Patterns

- **MVC** — standard ASP.NET Core MVC with Razor views
- **Repository-less** — controllers query `ApplicationDbContext` directly via EF Core LINQ
- **Cookie authentication** — custom cookie auth with per-request security stamp validation
- **Soft delete** — `IsActive` flag on Member and User; no hard deletes
- **Unique index enforcement** — database-level constraints catch duplicates; controllers catch `DbUpdateException`
- **Pagination** — shared `PagedList<T>` + `_Pager.cshtml` partial used across all list views
- **Role-based authorization** — `[Authorize(Roles = AppRoles.Admin)]` on admin-only actions
- **Bind attribute** — `[Bind("...")]` on all POST parameters to prevent mass assignment
- **AsNoTracking** — applied on all read-only queries for performance
