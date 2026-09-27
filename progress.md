i now have a laptop to work with, but using only visual studio code.

# C# ASP.NET Core MVC Project — Continuation Note

## 1. Project Overview

I am building a school project titled:

**Student Organization Membership and Activity Management System**

The original proposal was for **VB.NET Windows Forms + MySQL**, but my version is different:

- **C#**
- **ASP.NET Core MVC**
- **Razor Views (.cshtml)**
- **Entity Framework Core**
- **MySQL**
- Web/browser-based instead of Windows Forms

I only have an **Android phone**, so I am creating/editing the project files using apps such as **Acode/Spck**. The final build, database testing, and debugging will be done later on my group leader's laptop.

I am responsible for the **C# project and database**. The leader will not create the database.

Do not add major features outside the original proposal unless I specifically request them.

---

# 2. Finalized Main Functions

The system will contain:

1. Login/User Accounts
2. Dashboard
3. Member Management
4. Activity Management
5. Attendance Management
6. Membership Management
7. Participation Monitoring
8. Reports
9. Search/Filtering

The C# version should have some implementation differences from the VB.NET version, but the overall project title and major functions remain similar.

Out-of-scope features:

- Mobile application
- Online payments
- Biometric attendance
- Advanced notifications
- Other major features not in the proposal

---

# 3. Database Design — FINALIZED

Database name:

`StudentOrganizationDB`

Tables:

### Roles

- RoleID PK
- RoleName

### Users

- UserID PK
- Username UNIQUE
- PasswordHash
- RoleID FK
- IsActive
- CreatedAt

### Members

- MemberID PK
- StudentID UNIQUE
- FullName
- Course
- YearLevel
- IsActive
- CreatedAt
- UpdatedAt

### Memberships

- MembershipID PK
- MemberID FK
- DateJoined
- Status
- UpdatedAt

### Activities

- ActivityID PK
- ActivityTitle
- ActivityDate
- Location
- Description
- IsActive
- CreatedAt
- UpdatedAt

### Attendance

- AttendanceID PK
- ActivityID FK
- MemberID FK
- Status
- RecordedAt

Unique constraint:

`(ActivityID, MemberID)`

### Participation

- ParticipationID PK
- ActivityID FK
- MemberID FK
- ParticipationType
- RecordedAt

Unique constraint:

`(ActivityID, MemberID)`

Attendance and Participation are intentionally **separate tables**.

`ParticipationType` is a simple VARCHAR/free-text field, not a separate table.

---

# 4. Database Relationships

- Roles 1 → many Users
- Members 1 → many Memberships
- Members 1 → many Attendance
- Activities 1 → many Attendance
- Members 1 → many Participation
- Activities 1 → many Participation

Foreign keys use restricted deletion so historical records are preserved.

Members and Activities are generally **deactivated instead of physically deleted**.

---

# 5. Current Project Structure

Current project name:

`StudentOrganizationSystem`

Structure:

StudentOrganizationSystem/

- Controllers/
  - HomeController.cs
  - AccountController.cs
  - MemberController.cs
  - ActivityController.cs
- Data/
  - ApplicationDbContext.cs
- Models/
  - Role.cs
  - User.cs
  - LoginViewModel.cs
  - DashboardViewModel.cs
  - Member.cs
  - Membership.cs
  - Activity.cs
  - Attendance.cs
  - Participation.cs
- Views/
  - Account/
    - Login.cshtml
    - AccessDenied.cshtml
  - Home/
    - Index.cshtml
  - Member/
    - Index.cshtml
    - Create.cshtml
    - Edit.cshtml
    - Details.cshtml
  - Activity/
    - Index.cshtml
    - Create.cshtml
    - Edit.cshtml
    - Details.cshtml
  - Shared/
    - \_Layout.cshtml
  - \_ViewImports.cshtml
  - \_ViewStart.cshtml
- wwwroot/
  - css/
    - site.css
  - js/
    - site.js
- Program.cs
- appsettings.json
- StudentOrganizationSystem.csproj

---

# 6. Technology Configuration

Target framework:

`.NET 8`

Main NuGet packages currently planned/used:

- Microsoft.EntityFrameworkCore.Design 8.0.20
- Pomelo.EntityFrameworkCore.MySql 8.0.3
- Microsoft.Extensions.Identity.Core 8.0.20

The project uses:

- Entity Framework Core
- Pomelo MySQL provider
- Cookie Authentication
- PasswordHasher
- ASP.NET Core MVC

---

# 7. Authentication — COMPLETED

Login uses:

`AccountController.cs`

Features:

- Username
- Password
- Remember Me
- Active-user checking
- Password hashing/verification
- Role claims
- Cookie authentication
- Logout
- Access Denied page

Authentication configuration includes:

- Login path: `/Account/Login`
- Access denied: `/Account/AccessDenied`
- 8-hour expiration
- Sliding expiration

There is currently **no default plaintext admin account**.

Important: passwords should be properly hashed.

---

# 8. Dashboard — COMPLETED

`DashboardViewModel.cs` contains:

- TotalMembers
- ActiveMembers
- TotalActivities
- ActiveActivities
- TotalAttendance
- TotalParticipation

`HomeController` is protected by `[Authorize]`.

Dashboard displays summary cards and quick-access cards.

Members quick-access link is already connected to Member Management.

Activities quick-access link was just connected to Activity Management.

---

# 9. Member Management — COMPLETED

Controller:

`MemberController.cs`

Features:

- List members
- Search by Student ID
- Search by name
- Active/Inactive filtering
- Add member
- Edit member
- View details
- Deactivate member
- Activate member

StudentID must be unique.

Members are not physically deleted because historical attendance/participation records need to remain.

Views:

- `Views/Member/Index.cshtml`
- `Views/Member/Create.cshtml`
- `Views/Member/Edit.cshtml`
- `Views/Member/Details.cshtml`

Navigation already contains:

`Members`

---

# 10. Activity Management — COMPLETED JUST NOW

Controller:

`ActivityController.cs`

Features:

- List activities
- Search by activity title
- Search by exact activity date
- Active/Inactive filtering
- Add activity
- Edit activity
- View details
- Cancel/deactivate activity
- Activate activity

Views:

- `Views/Activity/Index.cshtml`
- `Views/Activity/Create.cshtml`
- `Views/Activity/Edit.cshtml`
- `Views/Activity/Details.cshtml`

Activity cancellation uses:

`IsActive = false`

instead of deleting the database row.

Navigation now contains:

`Activities`

The Dashboard Activities quick-access card points to:

`ActivityController / Index`

Activity fields:

- ActivityTitle
- ActivityDate
- Location
- Description
- IsActive
- CreatedAt
- UpdatedAt

---

# 11. IMPORTANT UNTESTED STATUS

Because I am working from a phone, the project has **not yet been fully built/run against MySQL**.

So do not assume everything compiles perfectly yet.

Final testing will be done later on the leader's laptop.

Known possible issue:

`Views/Account/Login.cshtml` references:

`_ValidationScriptsPartial`

but that partial has not been explicitly created yet.

Before final testing, check/fix this if necessary.

Also verify:

- NuGet package compatibility
- EF Core/MySQL connection
- MySQL database
- migrations/database schema
- Razor view compilation
- authentication
- HTTPS behavior
- form validation

---

# 12. Next Step — 3F

Continue with:

## 3F — Attendance Management

The Attendance module should connect the existing:

- Members
- Activities
- Attendance

tables.

Based on the proposal, it should allow the user to:

1. Select an activity
2. See/select organization members
3. Mark members Present/Absent
4. Save attendance
5. Search/view attendance
6. View attendance history
7. Prevent duplicate attendance for the same member/activity

The database already has the unique constraint:

`(ActivityID, MemberID)`

so the same member should not have two attendance records for one activity.

Keep the implementation consistent with the existing Member and Activity modules.

Use `[Authorize]` on protected controllers.

Do not physically delete attendance records unless there is a strong reason; preserve historical records.

---

# 13. Development Style

I want the code explained simply because I am a student.

When creating new modules:

1. Tell me which folder/file to create.
2. Give the complete code for that file.
3. Explain briefly what it does.
4. Tell me exactly what existing file needs to be updated.
5. Keep the UI professional and clean.
6. Make it usable on both desktop and smaller screens.
7. Do not assume I can run Visual Studio on my phone.
8. Keep the code compatible with ASP.NET Core MVC / .NET 8.
9. Do not redesign already-finalized database tables without explaining why.
10. Build the system one module at a time.

The current completed stage is:

**3E — Activity Management**

Next:

**3F — Attendance Management**

When I say **“Continue with 3F”**, start directly from Attendance Management using all the context above.

---

# 14. UI Redesign - Sidebar App Shell (DONE)

The UI was redesigned from the login page up to the main screen.

## Login page (Views/Account/Login.cshtml + wwwroot/css/login.css)

- Split screen: left brand panel (gradient + feature list), right sign in card
- The brand panel hides below 900px so the form stays usable on phones

## Main screen shell (Views/Shared/_Layout.cshtml)

- Left sidebar (fixed, 250px) with grouped links:
  - Overview: Dashboard
  - Management: Members, Activities, Membership (Soon)
  - Tracking: Attendance, Participation (Soon)
  - Insights: Reports (Soon)
- The active link is highlighted automatically from the current controller
- The sign out form is at the bottom of the sidebar
- Top bar: mobile menu button, page title, date, user chip (avatar + role)
- Pages that can be opened while signed out still use the old centered layout

## Dashboard (Views/Home/Index.cshtml)

- Gradient hero banner with greeting + quick action buttons
- Six stat cards with icons
- Quick access cards (real links) plus "Soon" cards for modules not built yet

## CSS / JS changes

- NEW wwwroot/css/sidebar.css: sidebar styles
- navbar.css: now the top bar
- base.css: added app shell layout (.app-shell, .app-main, .app-content)
- dashboard.css, login.css, responsive.css: rewritten
- components.css: added avatar / icon button helpers
- site.js: sidebar drawer open/close, backdrop + Esc close, alert auto-hide

## Bugs fixed along the way

- _ValidationScriptsPartial.cshtml used ~/lib/... which does not exist in wwwroot.
  It now loads jQuery + jQuery Validation from the jsDelivr CDN.
- Login.cshtml used "Layout = null" together with a @section Scripts block, so the
  validation scripts were never rendered. The partial is included in the page now.
- AccessDenied.cshtml used old class names; now uses .access-denied-page and btn btn-primary.

## Verified

- dotnet build succeeds with 0 errors / 0 warnings
- Login, Dashboard, Member list and Attendance list were rendered in a headless test run
  (HTTP 200); the sidebar, active states and top bar all render correctly

---

# 15. System Audit

A full, dated audit of the whole system (schema, controllers, models, views, git hygiene,
security and missing functions) is kept in:

**`AUDIT.md`** (project root)

It lists findings with status boxes, the evidence used to prove each one, a phased work plan
and the open decisions that block further work. Read `AUDIT.md` before starting a new session.

---

# 16. Phase 0 — Repo Protection and Readable Failures (DONE, verified at runtime)

This is step one of the phased plan in `AUDIT.md` (section 7). No new feature was built: the goals
were (a) to stop the project from leaking the MySQL password and from tracking build output, and
(b) to make failures readable instead of a raw stack trace.

## 16.1 Files added or changed

| File | What changed |
|---|---|
| `.gitignore` (NEW) | ignores `bin/`, `obj/`, `appsettings.Development.json`, IDE/OS noise, logs |
| `appsettings.json` | committed file holds `password=YOUR_PASSWORD` again (plus a `"//"` hint) |
| `appsettings.Development.json` (NEW, git-ignored) | the real local connection string for this machine |
| `Properties/launchSettings.json` (NEW) | starts with `ASPNETCORE_ENVIRONMENT=Development` on `http://localhost:5080` |
| `Program.cs` | fixed `MySqlServerVersion`; guarded startup with one readable message; `UseDeveloperExceptionPage` (Development) + `UseExceptionHandler` + status-code pages (Production) |
| `Controllers/HomeController.cs` | new `Error` action (`[AllowAnonymous]`, request id, exception text only in Development) |
| `Models/ErrorViewModel.cs` (NEW) | the data the error page shows |
| `Views/Home/Error.cshtml` (NEW) | friendly error / not-found page |
| `Models/AppRoles.cs` (NEW) | the `Admin` / `Officer` role name constants |
| `Data/DbInitializer.cs` | also seeds the `Officer` role, through the new idempotent `EnsureRoleAsync` |
| `Controllers/MemberController.cs`, `Controllers/ActivityController.cs` | `Edit` (GET + POST), `Activate` and `Deactivate` are now `[Authorize(Roles = AppRoles.Admin)]` |
| `wwwroot/css/components.css` | `.error-page*` styles for the new error page |

## 16.2 Repo protection (P0.1, P0.2)

- 121 of the 166 tracked files were build output. They were removed from the index
  (`git reset -- bin obj`, the same result as `git rm -r --cached bin obj`); the files stay on disk,
  they are simply no longer tracked. `git ls-files` now lists 61 files.
- The real password now lives only in the git-ignored `appsettings.Development.json`.
  `git grep --cached -i '<password>'` and `git log --all -S '<password>'` both find nothing, so the
  password was never committed and does **not** need rotating — but never type it into
  `appsettings.json` again.
- Because `Properties/launchSettings.json` sets the environment to Development, `dotnet run` picks
  `appsettings.Development.json` up automatically. On another machine you create that file yourself
  (it is deliberately not in git).

## 16.3 Readable failures (P0.3, P0.4)

- Bad address -> `Error 404` page. Unhandled exception in Production -> `Error 500` page with a
  request id and no stack trace. Unhandled exception in Development -> the detailed developer page.
- Startup: when MySQL is not reachable the app prints one readable block ("MySQL is not reachable,
  so the system cannot start.", what to check, and the real cause) and stops with exit code 1
  instead of dumping ~30 lines of driver stack trace.

## 16.4 Roles (P0.6, decision D3)

Any signed-in user can read every list/details page, register members and activities, and record
attendance. Only an **Admin** can change an existing record: `Edit`, `Activate` and `Deactivate` in
Member and Activity. Refused requests land on `/Account/AccessDenied`.

## 16.5 How Phase 0 was verified (repeatable)

1. `dotnet build` -> succeeded, 0 warnings, 0 errors.
2. Start the built app on a spare port (`--urls http://127.0.0.1:5210`) and fetch pages over HTTP.
3. Sign in as `admin` / `admin123` -> every page returned 200.
4. Deactivate then Activate member 1 and compare `Members.IsActive` in the database (1 -> 0 -> 1).
5. Verify the error pages with a temporary throwing controller (removed afterwards).
6. Verify the role policy with a throw-away Officer account (removed afterwards).
7. Point the app at a dead port to verify the "MySQL is not reachable" message and exit code 1.

## 16.6 What is next

**Phase 1 — correctness pass** (`AUDIT.md` section 7, items P1.1-P1.10). The big one is P1.1:
attendance currently records every member as Present by default, and the attendance POST has never
been exercised end to end. Decision D1 (tri-state + "mark all present/absent" buttons) should be
confirmed before that work starts.

