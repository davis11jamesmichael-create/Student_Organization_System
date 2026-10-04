# Student Organization System — To-Do List

## Audit (see AUDIT.md)

- [x] Read `AUDIT.md` - full dated audit: findings, evidence, phased plan, open decisions
- [x] **Phase 0 (protect the repo, make failures readable) - DONE and verified at runtime**
      Each item below has a "Done in Phase 0" note in `AUDIT.md`:
  - [x] P0.1 Real DB password: `appsettings.json` holds `YOUR_PASSWORD` again; the real login lives
        in the git-ignored `appsettings.Development.json`. It was never committed, so no password
        rotation is needed - it just must never go back into a tracked file
  - [x] P0.2 `.gitignore` added; `bin/`+`obj/` removed from the index (166 -> 61 tracked files)
  - [x] P0.3 Error endpoint added: `HomeController.Error` + `Views/Home/Error.cshtml` +
        `UseDeveloperExceptionPage` (Development) + `UseExceptionHandler` / status-code pages
        (Production). A bad URL now shows "Error 404" instead of an empty page
  - [x] P0.4 Fixed `MySqlServerVersion` + guarded startup: a stopped MySQL prints one readable
        message and stops with exit code 1 instead of a driver stack trace
  - [x] P0.5 `todo.md` kept honest (this list)
  - [x] P0.6 Roles enforced (decision D3): `Edit` / `Activate` / `Deactivate` are Admin-only;
        reading, registering and recording attendance stay open to any signed-in user
  - [x] P0.6 follow-up (2026-10-02): `Create` is Admin-only too, and every Admin-only control
        (Add, Edit, Activate/Deactivate) is hidden from Officers; Admins get an "Administration"
        nav group and an Admin badge. Verified at runtime as Admin and as a throw-away Officer.
- [x] **Phase 3 - polish and testing - DONE and verified (2026-10-03)**
      Each item has a "Done in Phase 3" note in `AUDIT.md` section 5; `progress.md` section 20
      holds the checks:
  - [x] Dead CSS removed: `site.css` deleted, `login.css` taken off `_Layout` (it now loads only
        on the standalone Login and Register pages)
  - [x] Views cleaned: no inline `style="..."`, no inline `onsubmit=`/`onchange=`/`onclick=`, no
        inline `<script>` blocks - the confirmations moved to `data-confirm` handled by `site.js`
  - [x] jQuery + jQuery Validation vendored into `wwwroot/lib` (no jsDelivr/CDN dependency)
  - [x] Pagination on Members / Activities / Attendance / Membership / Participation: shared
        `Data/Paging.cs` + `_Pager.cshtml`, 20 per page, filters carried in the links, wild pages
        such as `?page=999` clamped instead of rendering empty
  - [x] Accessibility: skip link to `<main id="main-content">`, `aria-current` omitted when
        inactive, `:focus-visible` ring on links/buttons/inputs/radios, labelled pager nav
  - [x] New `StudentOrganizationSystem.Tests` xUnit project - **35 tests, all green**
        (`dotnet test`); duplicate-key tests use SQLite `:memory:` so unique indexes are enforced
  - [x] Development-only seed data (`DbInitializer.SeedSampleDataAsync`): 5 members, 3 activities,
        attendance + participation history and an `officer` / `officer123` account; verified
        idempotent on a throw-away database and verified **not** to run when
        `ASPNETCORE_ENVIRONMENT=Production`

---

## Phase 1 - correctness pass (see AUDIT.md section 7) — DONE and verified at runtime

- [x] P1.1 Attendance marks every member "Present" by default - tri-state + "mark all" buttons (decision D1)
- [x] P1.2 Attendance status is unvalidated free text - whitelist it server-side
- [x] P1.3 Attendance can still be recorded for a cancelled activity
- [x] P1.4 Duplicate keys (`StudentID`, attendance) throw an unhandled 500 - catch `DbUpdateException`
- [x] P1.5 Dead concurrency handling in Member `Edit` (no concurrency token exists)
- [x] P1.6 Inputs are never trimmed or format-checked (`StudentID`, names, dates)
- [x] P1.7 Attendance POST runs one query per member (N+1) - load in one query
- [x] P1.8 Updating attendance overwrites the original `RecordedAt` (decision D4)
- [x] P1.9 Login: rehash old hashes, honour `returnUrl`, limit attempts, change password
- [x] P1.10 Query tightening (dashboard counters, `AsNoTracking()` on read-only queries)
      Each item has a "Done in Phase 1" note in `AUDIT.md`, and `progress.md` section 18
      holds the verification script and the 26/26 result. Decisions D1 and D4 were taken.

---

## Security Fixes (from code review)

- [x] **Hardcoded DB password** in `appsettings.json` (FIXED in Phase 0 - see AUDIT.md P0.1)
  - The committed `appsettings.json` now holds `password=YOUR_PASSWORD`
  - The real login for this machine lives in `appsettings.Development.json`, which is git-ignored
  - The copy inside `bin/Debug/` is no longer tracked (Phase 0 P0.2), so it cannot be committed
- [x] **Mass Assignment** in `AccountController`, `MemberController`, `ActivityController`
  - Add `[Bind("field1,field2,...")]` to all POST action parameters
  - This prevents attackers from setting fields like `IsActive`, `CreatedAt` via crafted POST requests
- [x] **SQL Injection warnings** in `MemberController`, `ActivityController`
  - These are false positives from EF Core LINQ queries (EF Core parameterizes automatically)
  - No raw SQL strings used — no action needed beyond confirming no `FromSqlRaw()` with user input

---

## UI Upgrade

- [x] Redesign `site.css` — modern, clean, professional look
- [x] Redesign `_Layout.cshtml` — better navbar, consistent layout
- [x] Upgrade `Views/Home/Index.cshtml` — dashboard with icons, better cards
- [x] Upgrade `Views/Account/Login.cshtml` — polished login page
- [x] Upgrade `Views/Member/Index.cshtml` — consistent with new UI
- [x] Upgrade `Views/Member/Create.cshtml` — consistent with new UI
- [x] Upgrade `Views/Member/Edit.cshtml` — consistent with new UI
- [x] Upgrade `Views/Member/Details.cshtml` — consistent with new UI
- [x] Upgrade `Views/Activity/Index.cshtml` — consistent with new UI
- [x] Upgrade remaining Activity views (Create, Edit, Details)

---

## UI Redesign — Sidebar App Shell (login → main screen)

- [x] New `wwwroot/css/sidebar.css` — fixed dark sidebar, section groups, active item, sign out
- [x] `wwwroot/css/navbar.css` — rewritten as the top bar (page title, date, user chip, mobile toggle)
- [x] `wwwroot/css/base.css` — added app-shell tokens + `.app-shell` / `.app-main` / `.app-content`
- [x] `wwwroot/css/dashboard.css` — gradient hero banner, icon stat cards, quick-access cards
- [x] `wwwroot/css/login.css` — split-screen sign in (brand panel + form card)
- [x] `wwwroot/css/responsive.css` — sidebar becomes an off-canvas drawer under 1024px
- [x] `Views/Shared/_Layout.cshtml` — sidebar + topbar shell, active nav highlighting
- [x] `Views/Account/Login.cshtml` — redesigned sign in page
- [x] `Views/Home/Index.cshtml` — redesigned dashboard
- [x] `Views/Account/AccessDenied.cshtml` — fixed classes (`.access-denied-page`, `btn btn-primary`)
- [x] `wwwroot/js/site.js` — sidebar drawer toggle, backdrop/Esc close, alert auto-hide
- [x] Sidebar shows Membership / Participation / Reports as disabled "Soon" items (modules not built yet)

### Bugs fixed during the redesign

- [x] `_ValidationScriptsPartial.cshtml` pointed at `~/lib/...` but `wwwroot/lib` does not exist
  - jQuery + jQuery Validation now load from jsDelivr CDN instead
- [x] `Login.cshtml` had `Layout = null` **and** `@section Scripts` — the section was never rendered,
  so client-side validation never loaded. The partial is now included directly in the page.
- [x] `.icon-btn { display: inline-flex }` in `components.css` loaded after `navbar.css` and would
  override the hidden mobile toggle — fixed by using `.topbar .topbar-toggle`


---

## Module 3F — Attendance Management

- [x] Create `Controllers/AttendanceController.cs`
- [x] Create `Views/Attendance/Index.cshtml` — list/search attendance records
- [x] Create `Views/Attendance/Record.cshtml` — select activity, mark members present/absent
- [x] Create `Views/Attendance/Details.cshtml` — view attendance for a specific activity
- [x] Update `_Layout.cshtml` — connect Attendance nav link
- [x] Update `Views/Home/Index.cshtml` — connect Attendance quick-access card

---

## Modules 3G-3J (DONE - 2026-10-03)

- [x] **3F — Attendance Management** ✅ DONE

---

- [x] **3G — Membership Management** ✅ DONE
  - `MembershipController` + `Membership/Index` and `Membership/Edit`
  - One membership row per member (unique index, migration `AddUniqueMembershipPerMember`)
  - Standing Active/Suspended (decision D2); "Inactive" mirrors `Member.IsActive`
  - History shown on Member Details (standing, counters, recent records)

- [x] **3H — Participation Monitoring** ✅ DONE
  - `ParticipationController` + `Participation/Index`, `Record`, `Details`
  - Upsert per the `(ActivityID, MemberID)` unique index; blank clears the row
  - Free-text type with a datalist of suggestions, trimmed and length-checked

- [x] **3I — Reports** ✅ DONE
  - `ReportsController` + `Reports/Index`, `Attendance`, `Members`
  - Per-activity summary (present/absent/rate) and per-member summary
  - Print-friendly view via `wwwroot/css/print.css` and a Print button

- [x] **3J — Search/Filtering (cross-module)** ✅ DONE
  - Topbar search box + `SearchController`/`Search/Index` results page
  - Members (ID/name/course) and Activities (title/location); each list keeps its own filters

## Module 3K — Users, Roles & Member Portal (DONE - 2026-10-02)

- [x] Third role: **Member** (`AppRoles.Member`, plus `AdminOrOfficer`)
- [x] `Users.MemberID` link + migration `AddUserMemberLink` (unique index, Restrict FK)
- [x] Public self-registration (`Account/Register`) - creates Member + User **inactive**
- [x] Approval gate: pending accounts cannot sign in; `User/Approve` activates both rows
- [x] `UserController` - create, edit, approve, reject, activate, deactivate, reset password
- [x] Last-admin guard (cannot deactivate/demote the only admin, or deactivate yourself)
- [x] Read-only Member **Portal** (own profile, attendance, participation)
- [x] Management controllers restricted to `AppRoles.AdminOrOfficer`
- [x] Role-aware sidebar, role badges, role-aware AccessDenied button
- [x] `Account/ChangePassword` (also closes the `SuccessRehashNeeded` half of P1.9)
- [x] Verified end to end at runtime (register -> pending -> approve -> portal -> role limits)

---
---

## Pre-Launch Checklist (do on leader's laptop)

- [ ] Install .NET 8 SDK
- [ ] Install MySQL Server
- [ ] Create database: `StudentOrganizationDB`
- [ ] Update `appsettings.json` with correct MySQL credentials
- [ ] Run EF Core migrations: `dotnet ef database update`
- [ ] Verify `DbInitializer` seeds the admin account correctly
- [ ] Test login with seeded admin credentials
- [ ] Test all completed modules end-to-end
- [ ] Check `_ValidationScriptsPartial.cshtml` is present (it is)
- [ ] Verify HTTPS redirect behavior (may need to disable for local HTTP testing)
- [ ] Check NuGet package restore: `dotnet restore`
- [ ] Build: `dotnet build`

---

## Known Issues / Notes

- `appsettings.json` is kept free of secrets now; put your real MySQL login in the git-ignored
  `appsettings.Development.json` (see progress.md section 16 and AUDIT.md P0.1)
- `bin/` and `obj/` are in `.gitignore` and were removed from the index in Phase 0 (AUDIT.md P0.2)
- Activity views use `btn btn-primary` Bootstrap classes but Bootstrap is NOT included — these need to use custom CSS classes (fixed in UI upgrade)
- Inconsistent CSS class naming between Member views and Activity views (fixed in UI upgrade)
- `TempData["Success"]` vs `TempData["SuccessMessage"]` — inconsistent key names across controllers (standardized to `TempData["Success"]`)
