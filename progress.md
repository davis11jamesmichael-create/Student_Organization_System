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

---

# 18. Phase 1 - correctness pass (DONE - 2026-10-02)

All of `AUDIT.md` section 7 items **P1.1-P1.10** are closed. Each one carries a "Done in
Phase 1" note in the audit; this section records what changed and how it was verified.

## 18.1 Files added or changed

| File | Change |
|---|---|
| `Models/AttendanceStatus.cs` (NEW) | the only allowed `Attendance.Status` values (`Present`, `Absent`) + `IsValid()` - the server-side whitelist (P1.2) |
| `Models/Attendance.cs` | added `UpdatedAt` (nullable); `RecordedAt` is now written once (P1.8 / D4) |
| `Models/Member.cs` | `StudentID` gained `[RegularExpression(@"^\d{4}-\d{5}$")]` (P1.6) |
| `Services/LoginAttemptTracker.cs` (NEW) | cache-backed failure counter + 5-minute lockout per username + IP (P1.9) |
| `Program.cs` | `AddMemoryCache()` + `AddScoped<LoginAttemptTracker>()` |
| `Controllers/AttendanceController.cs` | `Record` (GET/POST) reworked: tri-state, whitelist, active-activity guard, one batched read + write, `DbUpdateException` catch, `RecordedAt` preserved (P1.1-P1.3, P1.4, P1.7, P1.8) |
| `Controllers/MemberController.cs` | trim + re-validate; `DbUpdateException` guard; dead concurrency block and unused `MemberExists` removed (P1.4-P1.6) |
| `Controllers/ActivityController.cs` | trim + re-validate; `AsNoTracking()` on Index / Edit (GET) / Details (P1.6, P1.10) |
| `Controllers/HomeController.cs` | dashboard counts collapsed from six queries to two grouped + two counts (P1.10) |
| `Controllers/AccountController.cs` | `returnUrl` (cleaned with `Url.IsLocalUrl`) + lockout; `RedirectToHomeFor` honours it (P1.9) |
| `Views/Attendance/Record.cshtml` | unmarked radios, live "not yet marked" counter, "Mark all present/absent", warn-on-save |
| `Views/Attendance/Index.cshtml` | no Record button for a cancelled activity; "Cancelled" badge |
| `Views/Attendance/Details.cshtml` | Update button only when active; error alert; `Updated At` column |
| `Views/Account/Login.cshtml` | hidden `returnUrl` on the sign-in form |
| `wwwroot/css/attendance.css` | `.attendance-toolbar`, `.attendance-counter`, `.is-clear` |
| `Migrations/20261002130445_AddAttendanceUpdatedAt.*` (NEW) | `Attendances.UpdatedAt datetime(6) NULL` |

## 18.2 Decisions taken

- **D1 (taken):** tri-state attendance plus bulk "mark all" buttons. Radios start unmarked, a
  live counter warns before saving, and a member with no saved status simply gets no row - a
  status is never invented.
- **D4 (taken):** `Attendance.UpdatedAt` added through the migration above; `RecordedAt` keeps
  the first recording time and is never overwritten. No concurrency token was added.
- **D2 remains open** and still blocks 3G Membership Management.

## 18.3 How it was verified (2026-10-02, build + runtime)

1. `dotnet build` -> succeeded, 0 warnings, 0 errors.
2. `dotnet ef migrations add AddAttendanceUpdatedAt`, then `dotnet ef database update`
   -> `ALTER TABLE Attendances ADD UpdatedAt datetime(6) NULL`.
3. The built app was started on `http://127.0.0.1:5212` (Development) and driven over HTTP with
   a PowerShell session (real login, anti-forgery token, real form posts).
4. **26/26 checks passed.** The run covered: every management page (200), `returnUrl` honoured
   and an external one refused, the 6th wrong password blocked by the lockout, digest:
   - P1.1 - a freshly created activity rendered **0 checked radios** and "4 member(s) not yet
     marked"; after saving, all 4 came back pre-selected.
   - P1.2 - posting `Hacked` for every member was rejected with an error and **stored nothing**.
   - P1.3 - for a cancelled activity, GET and POST were both refused with the cancelled message
     and the index showed the "Cancelled" badge.
   - P1.6 - a bad `StudentID` showed the format field error; a padded `  2024-53372  ` was
     trimmed to `2024-53372` and matched the stored row (no fifth row created).
   - P1.4 - a duplicate Student ID returned the field error with HTTP 200, never a 500.
   - P1.8 - after flipping every status, all `UpdatedAt` cells were populated and `RecordedAt`
     stayed listed.

---

# 19. Phase 2 - finish the system (DONE - 2026-10-03)

Phase 2 (`AUDIT.md` section 7) built the four remaining proposal functions plus the member-history
view. `todo.md` now lists 3G-3J as done, and `AUDIT.md` section 4 shows every proposal function as
complete.

## 19.1 Decision D2 (taken first - it unblocked 3G)

- **One `Membership` row per member.** A unique index on `Memberships.MemberID` was added through
  migration `AddUniqueMembershipPerMember`. (MySQL refuses to drop an index a foreign key depends
  on, so that migration does the drop and the unique add in one `ALTER`.)
- **`Member.IsActive` stays the source of truth.** `Membership.Status` is **Active** or
  **Suspended** (the two an Admin may set) and is mirrored to **Inactive** automatically whenever
  the member is deactivated; reactivating returns it to **Active** while preserving a "Suspended"
  standing. The two fields therefore can never contradict each other.
- **"History"** is the membership's DateJoined / UpdatedAt plus the member's attendance and
  participation - not extra membership rows. `MembershipPolicy` (`Models/MembershipPolicy.cs`)
  holds the Ensure/SyncStanding helpers that keep the pair consistent.
- A membership row is created with the member everywhere a member is created: the Admin form
  (`MemberController.Create`), self-registration (`AccountController.Register`, starts "Inactive")
  and, defensively, on the first 3G save for a legacy member.

## 19.2 Files added or changed

| File | Change |
|---|---|
| `Models/MembershipStatus.cs` (NEW) | Active / Suspended / Inactive + `Selectable` / `IsValid` |
| `Models/MembershipPolicy.cs` (NEW) | `StandingFor` / `Ensure` / `SyncStanding` (D2 mirror) |
| `Models/MembershipViewModel.cs` (NEW) | 3G list row + standing form model |
| `Models/ParticipationTypes.cs` (NEW) | suggested free-text types + Normalize / MaxLength |
| `Models/ReportViewModels.cs` (NEW) | `ActivityAttendanceReportRow`, `MemberReportRow` |
| `Models/SearchResultsViewModel.cs` (NEW) | 3J result model |
| `Controllers/MembershipController.cs` (NEW) | 3G: Index (search + standing filter), Edit (Admin, upsert) |
| `Controllers/ParticipationController.cs` (NEW) | 3H: Index, Record (upsert/clear), Details |
| `Controllers/ReportsController.cs` (NEW) | 3I: Index, Attendance, Members |
| `Controllers/SearchController.cs` (NEW) | 3J: cross-module search (capped at 50 per type) |
| `Controllers/MemberController.cs` | creates a membership; Includes memberships on Edit/Activate/Deactivate/Details; Details loads the history |
| `Controllers/AccountController.cs` | self-registration creates an "Inactive" membership |
| `Controllers/UserController.cs` | approving a registration mirrors the standing to Active |
| `Data/ApplicationDbContext.cs` | unique index on `Memberships.MemberID` |
| `Migrations/20261003063853_AddUniqueMembershipPerMember.*` (NEW) | the index change above |
| `Views/Membership/Index.cshtml`, `Edit.cshtml` (NEW) | 3G screens |
| `Views/Participation/Index.cshtml`, `Record.cshtml`, `Details.cshtml` (NEW) | 3H screens |
| `Views/Reports/Index.cshtml`, `Attendance.cshtml`, `Members.cshtml` (NEW) | 3I screens |
| `Views/Search/Index.cshtml` (NEW) | 3J results page |
| `Views/Member/Details.cshtml` | membership standing + counters + recent attendance/participation |
| `Views/Shared/_Layout.cshtml` | Membership / Participation / Reports / Search nav entries, topbar search, print.css + participation.css links |
| `Views/Home/Index.cshtml` | the three "Soon" cards became real links |
| `wwwroot/css/participation.css` (NEW) | 3H layout |
| `wwwroot/css/print.css` (NEW) | print-friendly reports |
| `wwwroot/css/components.css` | `.badge-suspended`, `.topbar-search` |

## 19.3 How it was verified (2026-10-03, build + runtime)

1. `dotnet build` -> succeeded, 0 warnings, 0 errors.
2. `dotnet ef migrations add AddUniqueMembershipPerMember`, then `dotnet ef database update`
   (the first attempt needed the single-`ALTER` fix above) -> `IX_Memberships_MemberID` is now
   `Non_unique = 0`.
3. The built app was started on `http://127.0.0.1:5216` (Development) and driven over HTTP.
4. **31/31 checks passed**, covering:
   - every new page returns 200 (Membership, Participation, Reports x3, Search) plus the old ones;
   - 3G: a new member was created and immediately showed an **Active** membership; an Admin set
     the standing to **Suspended**; a crafted `Status=Inactive` POST was refused with
     "Choose Active or Suspended" and left the standing unchanged;
   - D2: `Member/Deactivate` mirrored the standing to **Inactive** (no longer Suspended) and
     `Member/Activate` returned it to **Active**;
   - 3H: participation was recorded (trimmed of surrounding spaces), updated, cleared by posting a
     blank box, and a 120-character type was refused ("characters or fewer");
   - 3I: both reports rendered and the print-only header is in the markup;
   - 3J: the topbar/`Search` page found the member, and an empty query shows the prompt;
   - 3F: Member Details rendered the History sections (membership standing, counters, recent rows).
5. The test member (and its membership), the participation row and the temp files were removed
   afterwards, so the database is back to its pre-run state (`Members = 1`, `Memberships = 0`,
   `Participations = 0`, `Activities = 1`). Legacy members with no membership row simply show
   "Not set up" until a standing is saved.

## 19.4 What is next

**Phase 3 - polish and testing** (`AUDIT.md` section 7): `site.css` cleanup, `login.css`, inline
styles/scripts, pagination on the longer lists, an xUnit test project for the high-risk logic,
an accessibility pass, and richer dev-only seed data (P1.12). All four decisions (D1-D4) are now
taken, so nothing blocks that work.


5. The two runtime questions `AUDIT.md` section 0 had left open were answered: the
   `attendance[<MemberID>]` form binding works with `[FromForm] Dictionary<int,string>`, and
   `<select asp-for="IsActive">` pre-selects the saved value on both Edit pages.
6. The test members, activities and attendance rows were deleted afterwards, so the database is
   back to its pre-run state (1 member / 1 activity / 1 attendance record).

## 18.4 What is next

**Phase 2 - finish the system** (`AUDIT.md` section 7): 3G Membership Management (after decision
**D2**), 3H Participation Monitoring, 3I Reports, 3J unified search, and member history on
Member Details.

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

---

# 17. Admin / Officer role split made visible (DONE - 2026-10-02)

The role policy already existed (Phase 0, decision D3): an **Admin** can change or (de)activate an
existing record, any signed-in user can read. This change makes that split **obvious and enforced**,
and extends it so the Admin is the one who *adds* records too.

## What changed

| File | Change |
|---|---|
| `Controllers/MemberController.cs` | `Create` (GET + POST) is now `[Authorize(Roles = AppRoles.Admin)]` (the class comment was updated) |
| `Controllers/ActivityController.cs` | `Create` (GET + POST) is now `[Authorize(Roles = AppRoles.Admin)]` (the class comment was updated) |
| `Views/Member/Index.cshtml` | `isAdmin` computed; `+ Add Member`, `Edit` and the `Activate`/`Deactivate` forms wrapped in `@if (isAdmin)`; Officers see a read-only note |
| `Views/Member/Details.cshtml` | `Edit` button wrapped in `@if (isAdmin)` |
| `Views/Activity/Index.cshtml` | `isAdmin` computed; `+ Add Activity`, `Edit` and the `Cancel`/`Activate` forms wrapped in `@if (isAdmin)`; Officers see a read-only note |
| `Views/Activity/Details.cshtml` | `Edit` button wrapped in `@if (isAdmin)` |
| `Views/Home/Index.cshtml` | the dashboard `+ Add Member` hero button is Admin-only (the `Record Attendance` button stays for everyone) |
| `Views/Shared/_Layout.cshtml` | `isAdmin` computed; an extra "Administration" sidebar group and an `Admin` badge in the top bar are rendered only for Admins |
| `wwwroot/css/components.css` | `.badge-admin` and `.admin-note` styles |

`_ViewImports.cshtml` already imports `StudentOrganizationSystem.Models`, so `AppRoles.Admin` and
`User.IsInRole(...)` are available in every view without an extra `@using`. The role claim was
already written into the auth cookie by `AccountController`; only the views were missing the check.

## What an Officer still can do

Read every Member / Activity / Attendance list and details page, and record attendance. Only the
four write controls they cannot use (Add, Edit, Activate, Deactivate) disappear.

## How it was verified (2026-10-02, build + runtime)

1. `dotnet build` -> succeeded, 0 warnings, 0 errors (the Razor views compile, so the view edits are
   valid).
2. Started the built app on a spare port (`--urls http://127.0.0.1:5211`).
3. **As Admin**: `>Edit<`, `>Deactivate<`, `+ Add Member`, `+ Add Activity` and the `badge-admin`
   are all present; the Officer `admin-note` is absent. `GET /Member/Create` -> 200.
4. **As a throw-away Officer** (created by copying the seeded password hash, then deleted):
   none of the admin controls are rendered (case-sensitive checks all `False`), `>View<` is still
   present, and `GET /Member/Create`, `GET /Member/Edit/1` and `POST /Member/Deactivate/1` all land
   on `/Account/AccessDenied`.
5. `Members.IsActive` for member 1 stayed at 1, and the throw-away account was deleted afterwards.

## Still open

The user/role management screen (Phase 2, item 4) - without it there is no way to create a real
Officer account from inside the app, so the seeded `admin` remains the only account.

**Superseded by section 18.** The screen now exists.

---

# 18. Roles: Admin / Officer / Member + self-service registration (DONE - 2026-10-02)

This is the largest change since the audit. It answers the question "is there RBAC, and can the
admin create officers?" - before this change the system had only **two** roles (Admin, Officer),
**no Member login at all**, and **no screen to create an account**, so the seeded `admin` was the
only way in.

> **Scope note:** the three-role model and the public registration page are an **extension beyond
> the finalized proposal** (section 2 says not to add major features unless requested). They were
> requested by the project owner on 2026-10-02 and are recorded here rather than left untracked.

## The role model

| Role | Can do | Cannot do |
|---|---|---|
| **Admin** | Everything: manage members, activities, attendance, **and** users/roles; approve or reject registrations; add members/activities | - |
| **Officer** | Read every list/details page; record attendance | Add, edit, activate or deactivate a member or activity; open the Users screen |
| **Member** | Sign in to the **Portal** and read **only their own** profile, attendance and participation | Open any management screen; see any other member's data |

The first two are "staff" (`AppRoles.AdminOrOfficer`); Member is confined to `PortalController`.

## Schema change (one migration: `AddUserMemberLink`)

A Member login has to point at a `Members` row, which the schema could not express before.

- `Users.MemberID` (int, **nullable**) -> `Members.MemberID`
- one-to-one, `OnDelete(DeleteBehavior.Restrict)`, giving a **unique index** (`IX_Users_MemberID`),
  so one member can never have two accounts
- nullable and `NULL` for every existing row, so the migration is safe on live data; MySQL allows
  many `NULL`s in a unique index
- applied automatically on start by `DbInitializer` (`MigrateAsync`)

`Models/AppClaims.cs` was added so the claim name (`MemberID`) is written and read from one place.

## Self-registration + approval (decision: create both rows inactive)

`Account/Register` is public. It creates **both** the `Member` and the `User` with `IsActive = false`
and links them; nothing is active until an Admin approves.

- duplicates are rejected up front (username, Student ID) **and** again via `DbUpdateException`, so a
  double submission cannot produce a 500 (P1.4)
- inputs are trimmed before saving, because MySQL 8's default collation is NO PAD (P1.6)
- login looks the user up **before** checking `IsActive`, so a pending account is told
  *"awaiting administrator approval"* instead of the generic invalid-credentials message
- the Admin approves on the Users screen, which activates the `User` **and** its `Member` together

## Files

| File | Change |
|---|---|
| `Models/AppRoles.cs` | added `Member` and `AdminOrOfficer` |
| `Models/AppClaims.cs` (NEW) | the `MemberID` claim name |
| `Models/User.cs`, `Models/Member.cs` | the `MemberID` link + inverse navigation |
| `Data/ApplicationDbContext.cs` | the one-to-one relationship |
| `Migrations/*_AddUserMemberLink.*` (NEW) | the schema change above |
| `Data/DbInitializer.cs` | also seeds the `Member` role |
| `Controllers/AccountController.cs` | approval gate, rehash on `SuccessRehashNeeded` (P1.9), `MemberID` claim, role-aware redirect, `Register`, `RegisterPending`, `ChangePassword` |
| `Controllers/PortalController.cs` (NEW) | the read-only Member portal |
| `Controllers/UserController.cs` (NEW) | user CRUD, approve/reject, activate/deactivate, reset password, last-admin guard |
| `Controllers/Home|Member|Activity|AttendanceController.cs` | `[Authorize(Roles = AppRoles.AdminOrOfficer)]` |
| `Views/Portal/*` (NEW) | Index, Attendance, Participation, NotLinked |
| `Views/User/*` (NEW) | Index (with pending approvals), Create, Edit |
| `Views/Account/Register|RegisterPending|ChangePassword.cshtml` (NEW) | - |
| `Views/Shared/_Layout.cshtml` | role-aware sidebar (Member sees only the Portal), role badge, Change password link |
| `Views/Account/AccessDenied.cshtml` | the button is role-aware (a Member was sent to a page they cannot open) |
| `wwwroot/css/components.css` | `.badge-role` + `.badge-admin` / `.badge-officer` / `.badge-member` |

## How it was verified (2026-10-02, build + runtime, end to end)

`dotnet build` -> 0 warnings, 0 errors. The app was started on a spare port (5212) and the whole
flow was driven over HTTP with a throw-away account, then everything was removed.

| Step | Result |
|---|---|
| `Users.MemberID` in the database | `int, nullable, Key = UNI` - migration applied |
| `__EFMigrationsHistory` | `InitialCreate` + `AddUserMemberLink` |
| `Roles` table | Admin (1), Officer (2), Member (3) |
| `GET /Account/Register` | 200 |
| `POST /Account/Register` | 200, "Registration received" |
| Sign in while pending | refused - "awaiting administrator approval" |
| Admin `GET /User/Index` | 200, "Pending Registrations" shown, the new user listed |
| `POST /User/Approve/<id>` | 200, approved; the pending block disappears |
| Member sign-in after approval | lands on the Portal ("My Records") |
| Member `GET /Portal/Attendance`, `/Portal/Participation` | 200 |
| Member `GET /Member|Activity|Attendance|User|Home/...` | all re-rendered as **Access Denied** |
| Member `GET /Portal/Attendance?memberId=1` | 200 - the query string is ignored, the claim wins |
| Admin `POST /User/Create` with Role = Officer | 200, account created |
| Officer sign-in | lands on the Dashboard; `/Member/Index` hides Edit and "+ Add Member" |
| Officer `GET /User/Index` | Access Denied |
| Admin `POST /User/Deactivate` on **its own** account | blocked ("cannot deactivate the account you are signed in with") |
| Cleanup | throw-away rows deleted (`users_left = 0`, `members_left = 0`), 0 stray processes |

## Still open / worth knowing

- **Public registration is an attack surface.** It is open to the whole network. A
  `Registration:Enabled` switch and basic rate limiting are the recommended next hardening step
  (this also overlaps P1.9's "limit attempts" item).
- Pending registrations appear in the **Members** list as inactive rows. Consider a "Pending" badge
  on `Member/Index` so an Admin is not surprised by them.
- The Member portal shows attendance now; the Participation page stays empty until module **3H** is
  built (there is no way to write a `Participation` row yet).
- The `AccessDenied` button and the sidebar are role-aware, so a Member is never sent to a page they
  cannot open.

---

## 20. Phase 3 - polish and testing (2026-10-03)

Closes `AUDIT.md` section 5 (P3) plus the four resolvable self-critiques in section 6: dead CSS,
inline styles/scripts, CDN scripts, pagination, accessibility, a test project, and richer
Development-only seed data.

### 20.1 Files added or changed

| File | What changed |
|---|---|
| `wwwroot/css/site.css` | **deleted** - it was a comment stub and nothing linked it |
| `notes.txt` | **deleted** - one `cd` command, tracked for no reason |
| `Views/Shared/_Layout.cshtml` | `login.css` link removed (11 real sheets remain); skip link `<a class="skip-link" href="#main-content">` as the first focusable element; `navAria()` returns `"page"` or `null` so inactive items omit `aria-current` |
| `Views/Account/Login.cshtml`, `Views/Account/Register.cshtml` | these two standalone pages (`Layout = null`) keep their own `login.css` link - that is the "move it into the Login page only" step |
| `Views/**` (Member, Activity, Attendance) | inline `style="..."`, `onsubmit=`, `onchange=`, `onclick=` replaced with classes; Activity cancel and user-rejection now use `data-confirm` handled by `site.js` |
| `Views/Attendance/Record.cshtml` | inline `<script>` block removed; the sheet logic runs from `js/site.js` via `@section Scripts` |
| `wwwroot/js/site.js` | mark-all present/absent, live "not yet marked" counter, warn-before-save, and the generic `data-confirm` submit handler |
| `wwwroot/css/base.css`, `components.css`, `attendance.css` | classes that replaced the inline styles; `:focus-visible` ring for links, buttons, inputs, selects, textareas and `[tabindex]` (plus `.btn` / `.icon-btn`) |
| `wwwroot/lib/` (NEW) | jQuery 3.7.1, jquery-validation 1.20.0, jquery-validation-unobtrusive 4.0.0 - vendored, so no jsDelivr/CDN dependency |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | loads `~/lib/*.js` instead of the CDN |
| `Models/Pagination.cs` (NEW), `Data/Paging.cs` (NEW), `Views/Shared/_Pager.cshtml` (NEW) | shared pager: page size 20, `Math.Clamp(page, 1, totalPages)`, "Showing X-Y of Z" + Prev/Next that keep the query strings |
| the five list `Index` actions (Member, Activity, Attendance, Membership, Participation) | each runs through `Paging.ToPagedAsync` and passes its filters into the pager |
| `StudentOrganizationSystem.csproj` | `<Compile Remove="StudentOrganizationSystem.Tests\**">` (and Content/None) so the app project does not compile the tests |
| `Data/DbInitializer.cs` | new `SeedSampleDataAsync` + `seedSampleData` parameter on `InitializeAsync` |
| `Program.cs` | passes `seedSampleData: app.Environment.IsDevelopment()` |
| `StudentOrganizationSystem.Tests/` (NEW) | csproj (net8.0, xunit 2.9.2, `FrameworkReference` to `Microsoft.AspNetCore.App`, SQLite via EF), `TestDb.cs`, `DatabaseConstraintTests.cs`, `ModelRulesTests.cs`, `ListFilterTests.cs` |


### 20.2 The test project (35 tests, all green)

`StudentOrganizationSystem.Tests` references the app project and a `FrameworkReference` to
`Microsoft.AspNetCore.App` (controllers can be constructed directly). The database is **SQLite
`:memory:`** with `EnsureCreated()`, not the EF in-memory provider, because only a real provider
enforces the unique indexes the duplicate-key tests are about.

| File | Tests | Covers |
|---|---|---|
| `DatabaseConstraintTests.cs` | 5 | duplicate `Members.StudentID`, duplicate `(ActivityID, MemberID)` in `Attendances` and in `Participations`, a second `Memberships` row per member, duplicate `Users.Username` - each asserts `DbUpdateException` |
| `ModelRulesTests.cs` | 26 | `AttendanceStatus` / `MembershipStatus` whitelists (`Selectable` / `IsValid`), `ParticipationTypes.Normalize` (trim + cap at 120), `MembershipPolicy` (Ensure creates a row, SyncStanding mirrors `IsActive`, a hand-set Suspended survives while active, deactivation -> Inactive -> reactivation -> Active, second Ensure is a no-op), `LoginAttemptTracker` (lockout after the limit, reset after the window, key casing), report row math (present/absent/rate, "never recorded"), `PagedResult` window maths and clamping |
| `ListFilterTests.cs` | 4 | Member Index search + status filter, Activity Index search + status filter, and the `page=999` clamp (controllers are called directly; a small `Model<T>` helper casts the `ViewResult`) |

`dotnet test` from `StudentOrganizationSystem.Tests/` -> **Passed! Failed: 0, Passed: 35, Total: 35**.

One test was corrected while making it green: `SyncStanding` intentionally *loses* a suspension
when the member is deactivated (deactivation forces `Inactive`, and the next activation derives
`Active`), so the original expectation ("Suspended survives deactivate -> reactivate") was wrong
about the design. It was split into the two behaviours the policy actually documents: a
deactivated member comes back as `Active`, and a hand-set `Suspended` survives a sync while the
member stays active.

### 20.3 Development-only seed data (P1.12)

`DbInitializer.SeedSampleDataAsync` fills an **empty** database so the modules can be demoed
without hand-typing rows. It is guarded three ways:

1. `Program.cs` only passes `seedSampleData: app.Environment.IsDevelopment()`;
2. `InitializeAsync` calls it only when it is building/skipping past the first admin (the
   Production path still creates roles + admin exactly as before);
3. the method returns immediately when `context.Members.AnyAsync()` is true, so it never
   overwrites or duplicates anything on a database that already has data.

What it inserts: **5 members** (4 active, 1 inactive), **3 activities** (2 past, 1 upcoming),
**8 attendance rows** (the two past activities x the 4 active members, every 4th marked absent),
**4 participation rows** on the first activity, **5 memberships** (4 Active / 1 Inactive, derived
from `Member.IsActive`), and an **`officer` / `officer123`** account in the `Officer` role so the
Admin/Officer split can be demonstrated.


### 20.4 How it was verified (2026-10-03, build + tests + runtime)

1. `dotnet build` -> succeeded, **0 warnings, 0 errors** (the app project; the test folder is
   excluded from it by the csproj `Compile Remove` item).
2. `dotnet test` from `StudentOrganizationSystem.Tests` -> **35/35 green** (one expectation was
   corrected against the real `SyncStanding` semantics, see 20.2).
3. The built app was started on `http://127.0.0.1:5216` with `ASPNETCORE_ENVIRONMENT=Development`
   and driven over HTTP (antiforgery token fetched, admin `POST /Account/Login`, then every page
   fetched with the session cookie). All of these passed:
   - anonymous `GET /Member` lands on the login form and never sees the member table;
   - every module page returns 200: `/`, `/Portal`, `/Member`, `/Activity`, `/Attendance`,
     `/Membership`, `/Participation`, `/Reports`, `/Search`, `/User`, `/Account/ChangePassword`;
   - rendered-markup scans on `/Member`, `/Member/Create` and `/Attendance/Record/1`:
     `style="` = 0, inline ` on*=` handlers = 0, CDN refs = 0, `aria-current="false"` = 0,
     exactly one `aria-current="page"` (the active nav item), skip link and
     `id="main-content"` present;
   - every `href="/css/..."` and `src="/js/..."` on those pages resolves to a real file in
     `wwwroot` (11 stylesheets + `js/site.js`), and `/Member/Create` also loads
     `/lib/jquery.min.js`, `/lib/jquery.validate.min.js` and
     `/lib/jquery.validate.unobtrusive.min.js` - all present, no CDN;
   - `login.css` is linked by Login and Register only - the Portal does not link it;
   - `data-confirm` is in the markup (Activity cancel, user rejection) and no view still uses
     `onsubmit`.
4. Pagination, on a throw-away database with **25 members** (the 5 seeded + 20 inserted for the
   test): `/Member` = 20 rows and "Page 1 of 2" with `rel="next"`; `?page=2` = 5 rows with
   `rel="prev"`; `?page=999` clamps to page 2 instead of rendering empty; `?status=Active` keeps
   `status=Active` in both pager links and `?status=Active&page=2` = 4 rows (24 active);
   `?search=Load` shows the 20-of-20 single page with no Prev/Next; `?search=zzz` shows 0 rows and
   no pager at all. `/Activity`, `/Attendance`, `/Membership` and `/Participation` render the
   pager too.
5. Seed data, on a second throw-away database (`StudentOrganizationDB_Phase3`), with the
   connection string overridden through the `ConnectionStrings__DefaultConnection` environment
   variable so **no config file was edited**:
   - the first Development start created the database through `MigrateAsync` and seeded it:
     `Members=5, Activities=3, Attendances=8, Participations=4, Memberships=5, Users=2`
     (`admin`/Admin + `officer`/Officer), memberships 4 Active + 1 Inactive, members
     `2024-00001`..`2024-00005` (the last one inactive);
   - HTTP pass: admin sign-in 200; Member list 5 rows, Activity 3, Attendance 2, Membership 5,
     Users 2; dashboard shows 5; Reports renders; `officer` / `officer123` signs in and reaches
     the Portal; a wrong password is rejected with the sign-in error message;
   - **idempotency:** the app was restarted on the same database and every count was unchanged.
6. Production gate, on a third throw-away database (`StudentOrganizationDB_ProdCheck`) started
   with `ASPNETCORE_ENVIRONMENT=Production`: the database and the admin account were still
   created, but the sample data was **not** - `Members=0, Activities=0, Attendances=0,
   Participations=0, Memberships=0, Users=1`.
7. Cleanup: both throw-away databases were dropped, the app was restarted against the real
   `StudentOrganizationDB` (Development) and is still serving; that database still holds its
   pre-run rows (`Members=1, Activities=1, Attendances=1, Participations=0, Memberships=0,
   Users=1`); the scratch verification scripts and logs were deleted from the project root.

### 20.5 What is next

The audit's system work is finished (P0, P1, P2 and P3 are all closed). What remains is the
**Pre-Launch Checklist** in `todo.md`, run on the group leader's laptop: install the .NET 8 SDK
and MySQL Server, put the real connection string in the git-ignored
`appsettings.Development.json`, build, sign in as `admin` / `admin123` and change the password,
then walk every module. Two openings noted earlier still stand: public registration is an attack
surface (`Registration:Enabled` switch + rate limiting), and pending registrations could get a
"Pending" badge on the Members list.

## 21. `boltai.md` review - urgent security hardening (2026-10-11)

Source: `boltai.md` (an external security/account review), captured as Part C of
`planned_features.md`. Its two "most urgent" findings are now fixed. C3-C10 remain.

### 21.1 C1 - the seeded admin no longer has a published password

- `Data/DbInitializer.cs` no longer contains `"admin123"`. `InitializeAsync` takes the password
  from `Seed:AdminPassword` (config / env / user-secrets); when it is not set it generates a random
  16-character password with `RandomNumberGenerator.GetString` and returns it once in a
  `SeedOutcome`.
- `Program.cs` prints that password **once** on first run (a bordered console block); it is never
  written to a file or a log.
- New `User.MustChangePassword` (migration `AddUserSecurityFields`) is set on the seeded admin and
  carried in the cookie as `AppClaims.MustChangePassword`. `Services/MustChangePasswordPolicy`
  (pure) lists the only paths that stay open; a `Program.cs` middleware (registered right after
  `UseAuthentication`) redirects everything else to `Account/ChangePassword`. The form shows a red
  banner while the flag is set, and `AccountController.ChangePassword` clears it.
- The dev-only sample account `officer` / `officer123` is untouched (still gated by
  `seedSampleData`, Development only).

### 21.2 C2 - the auth cookie revalidates on every request

- New `User.SecurityStamp` (same migration) is minted into the cookie as
  `AppClaims.SecurityStamp` by `Services/UserClaims`.
- `Program.cs` adds `CookieAuthenticationEvents.OnValidatePrincipal`: it re-reads the account
  through `Services/SecurityStampGuard` (a ~1-minute `IMemoryCache` entry) and, on a stamp / role /
  active mismatch, calls `RejectPrincipal()` + `SignOutAsync()`. A demoted or deactivated user is
  signed out on their next click instead of keeping the old role for up to eight hours.
- The stamp is rotated (and the cache evicted) in `UserController.Edit` (only when the role, the
  active flag or the password actually changed), `Deactivate`, `Activate`, `ResetPassword` and
  `AccountController.ChangePassword`. `DbInitializer.BackfillSecurityStampsAsync` gives a stamp to
  any row that predates the column, so old accounts are not signed out permanently.

### 21.3 Files added or changed

| File | What changed |
|---|---|
| `Models/User.cs` | `SecurityStamp` (Required, 64) and `MustChangePassword` |
| `Models/AppClaims.cs` | `SecurityStamp` and `MustChangePassword` claim names |
| `Models/SecurityStamps.cs` (NEW) | `New()` -> a 32-char GUID stamp |
| `Services/SecurityStampGuard.cs` (NEW) | `UserSnapshot`, the pure `IsCurrent` rule, the cached `GetCurrentAsync`, and `Evict`. (Named "Guard" - `Microsoft.AspNetCore.Identity.SecurityStampValidator` owns the obvious name.) |
| `Services/MustChangePasswordPolicy.cs` (NEW) | the pure list of paths allowed during the forced change |
| `Services/UserClaims.cs` | mints the stamp and (when set) the must-change flag |
| `Data/DbInitializer.cs` | `SeedOutcome`, `EnsureFirstAdminAsync`, `GeneratePassword`, `BackfillSecurityStampsAsync`; the literal password is gone |
| `Program.cs` | registers `SecurityStampGuard`; adds `OnValidatePrincipal`; passes `Seed:AdminPassword`; prints the one-time password; adds the must-change middleware |
| `Controllers/UserController.cs` | `SecurityStampGuard` injected; `InvalidateSessions` on edit / deactivate / activate / reset |
| `Controllers/AccountController.cs` | `SecurityStampGuard` injected; `ChangePassword` GET sets `ViewData["MustChange"]`, POST clears the flag, rotates the stamp and re-issues the cookie |
| `Views/Account/ChangePassword.cshtml` | red "one-time password" banner when `ViewData["MustChange"]` is set |
| `appsettings.json` | documented `Seed:AdminPassword` (empty by default) |
| `Migrations/20261011022935_AddUserSecurityFields.*` (NEW) | adds both columns (defaults for existing rows) |
| `StudentOrganizationSystem.Tests/SecurityHardeningTests.cs` (NEW) | 21 tests for C1 + C2 |

### 21.4 How it was verified (2026-10-11, build + tests + migration)

1. `dotnet build` -> succeeded, **0 warnings, 0 errors**.
2. `dotnet ef migrations add AddUserSecurityFields` -> reviewed the generated `Up`
   (`MustChangePassword tinyint(1) NOT NULL DEFAULT FALSE`, `SecurityStamp varchar(64) NOT NULL
   DEFAULT ''`), then `dotnet ef database update` applied it to the local `StudentOrganizationDB`.
3. `dotnet test StudentOrganizationSystem.Tests/StudentOrganizationSystem.Tests.csproj` ->
   **Passed! Failed: 0, Passed: 73, Total: 73** (the previous 52 plus 21 new). The new tests cover:
   the stamp rule (match / stale / missing / deactivated / changed role / exact role compare), the
   cookie claims, `MustChangePasswordPolicy`, and `DbInitializer.EnsureFirstAdminAsync` (a generated
   password is stored and `admin123` is not; a configured password is used and not echoed back; no
   second admin is ever created).

### 21.5 What is next

`planned_features.md` step 2 onward: **Part A** (sidebar / Profile change-password tidy-up + a Back
button), then **B1 Announcements**, **B2 Notifications**, **B3 Activity comments**, then **C3-C10**
(audit logging, role validation, the `Edit` self-deactivation guard, super-admin tier, dormant
accounts, and the async / atomic-`Reject` / UTC bug fixes).

## 22. Part A - Profile / change-password UI tidy-up (2026-10-11)

`planned_features.md` step 2 (A1-A3). Small, low-risk UI changes; the recommended A2 approach was
taken (the standalone `Account/ChangePassword` page stays).

### 22.1 What changed

| File | What changed |
|---|---|
| `Views/Shared/_Layout.cshtml` | the `<a class="sidebar-signout" ... ChangePassword>` block was deleted from `.sidebar-footer` (A1); the Logout form stays |
| `Views/Profile/Index.cshtml` | an **Account** `form-card` with a `Change password` primary button linking to `Account/ChangePassword` (A2); the picture hint no longer says "use … to change your password"; a `.page-back` row with the new Back partial (A3) |
| `Views/Account/ChangePassword.cshtml` | a `.page-back` row with the Back partial; `ViewData["BackFallback"]` is set to the Profile page |
| `Views/Shared/_BackButton.cshtml` (NEW) | `<a class="btn-back" data-back href="...">← Back</a>`; fallback = `ViewData["BackFallback"]`, else Home for staff / Portal for a Member |
| `wwwroot/js/site.js` | a `[data-back]` handler that calls `history.back()` only when `document.referrer` is the same origin, otherwise follows the `href` (mirrors the `data-confirm` pattern; no inline JS) |
| `wwwroot/css/components.css` | `.btn-back` and `.page-back` |

### 22.2 A pitfall worth remembering

The first version of the partial used `@model string?` and was called as `<partial name="_BackButton" />`.
That 500'd: when a partial has no `model` attribute, ASP.NET Core passes the **parent** view's model,
so the partial received the Profile page's `User` and the `string?` cast failed. The fix was to drop
`@model` and read the fallback from `ViewData["BackFallback"]`, computing the role-aware default
inside the partial.

### 22.3 How it was verified (2026-10-11, build + tests + runtime)

1. `dotnet build` -> succeeded, **0 warnings, 0 errors**.
2. `dotnet test StudentOrganizationSystem.Tests/...` -> **Passed! Failed: 0, Passed: 73, Total: 73**
   (views are not unit-tested, so the count is unchanged).
3. Runtime walk on a throw-away database (`StudentOrganizationDB_C1C2Check`, dropped afterwards),
   app on `http://127.0.0.1:5220`; sign-in with the first-run one-time password, then:
   - `/Member/Index` before the change -> bounced to `/Account/ChangePassword` with the red banner;
   - after the change -> `/Member/Index` = 200;
   - `/Profile` = 200, contains a Back button (`data-back`) and exactly **one**
     `href="/Account/ChangePassword"` link;
   - the sidebar footer contains **no** "Change password" text but keeps "Logout";
   - `/Account/ChangePassword` = 200 with a Back link whose `href` is `/Profile`.

