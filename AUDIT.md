# System Audit - Student Organization Management System

- Audited: 2026-09-27
- Commit at audit time: 88b1daa ("added the folders and initial files", branch main)
- Auditor: AI code review (Cline) at the request of the project owner
- Project state at audit time: `dotnet build` succeeds with 0 errors; the UI has just been
  redesigned (sidebar app shell); 3 of the 9 proposed functions are not built yet
- Scope: everything under the project root except `bin/` and `obj/` - Program.cs,
  Controllers, Models, Data, Migrations, Views, wwwroot

Purpose of this file: a dated, citable record of what is correct, what is broken and what is
still missing. Every finding has a status box that gets ticked when it is fixed, so this file
doubles as a progress ledger. Update the "Audited" line whenever the audit is repeated.

---

## 0. How this audit was verified

Static evidence (re-run any of these to confirm a finding):

| Command | What it proved |
|---|---|
| `git show HEAD:appsettings.json` vs `git show ':appsettings.json'` | committed file has the placeholder, staged file has the real password |
| `(git ls-files | Measure-Object).Count` | 166 tracked files |
| `git ls-files | Select-String '^(bin|obj)/'` | 121 of those 166 files are build output |
| `Get-ChildItem .\Controllers -Filter *.cs | Select-String 'Authorize\(Roles|IsInRole'` | 0 matches - no role enforcement |
| `Select-String 'IActionResult Error'`, `Test-Path Views/Home/Error.cshtml` | 0 matches / False - no error endpoint |
| `git ls-files | Select-String Migration` | the 3 migration files are staged but not committed |

Runtime evidence (not just reading code):

- `dotnet build -p:EmitCompilerGeneratedFiles=true -t:Rebuild` -> Build succeeded, 0 errors,
  and the Razor source generator emitted 18 generated view files (proof the views compile).
- The built app was started on a spare port (5210) and pages were fetched over HTTP:
  `/Account/Login` = 200, POST login as `admin` = 200, `/Member/Index` = 200,
  `/Attendance/Index` = 200. The dashboard showed live data: 1 member, 1 activity,
  1 attendance record, 0 participation records.
- All temporary files and the extra process were removed afterwards.
- Phase 0 verification (2026-09-27): a fresh build was started on port 5210 (Development) and 5211
  (Production). Sign-in as `admin` returned 302, the dashboard, member, activity and attendance
  pages returned 200, `GET /no-such-page-here` rendered `Error 404`, an injected exception rendered
  the developer page in Development and the friendly page in Production (no stack trace), a
  Deactivate/Activate pair as Admin moved `Members.IsActive` 1 -> 0 -> 1, the same POST as a
  throw-away Officer account was refused without changing the row, and a startup against a dead port
  printed one readable message and exited with code 1. Full detail in the Phase 0 section of the work
  plan below.

Runtime behaviours that were open before Phase 1 and are now resolved:

- `attendance[<MemberID>]` binds into `Dictionary<int, string>` with `[FromForm]` - confirmed in
  Phase 1 by saving attendance through the real form and reading it back.
- `<select asp-for="IsActive">` pre-selects the saved value on both Edit pages - confirmed in
  Phase 1 (member 1 and activity 1 render `<option value="true" selected="selected">`).

---

## 1. What is solid (keep this)

1. The database design and the migration match the finalized proposal: unique indexes on
   `Users.Username`, `Members.StudentID`, and the composite `(ActivityID, MemberID)` on both
   `Attendances` and `Participations` (`Migrations/20260927023855_InitialCreate.cs`).
2. Every foreign key uses `ReferentialAction.Restrict`, so historical rows can never be
   orphaned or cascade-deleted.
3. Every controller follows the same pattern: `Index` (search + status filter) -> `Create` ->
   `Edit` -> `Details` -> `Activate`/`Deactivate`. Nothing is physically deleted, which matches
   the "deactivate instead of delete" rule in progress.md.
4. Security basics are present: all controllers are `[Authorize]`, every POST carries
   `[ValidateAntiForgeryToken]`, and POST actions use `[Bind("field1,field2")]` so a crafted
   form cannot set `IsActive` or `CreatedAt` (mass assignment).
5. `TempData["Success"]` is used consistently across every module - the old inconsistency
   recorded in todo.md is genuinely fixed.
6. Passwords are hashed with `PasswordHasher<User>`; no plaintext password is stored.
7. `DbInitializer` is idempotent: it checks for an existing Admin role and account before
   seeding, so restarting the app does not duplicate data.

---

## 2. P0 - fix before any further commit or push

### P0.1 - The MySQL root password is staged for commit  [x] fixed (Phase 0, 2026-09-27)

Done in Phase 0: the real login now lives only in the git-ignored `appsettings.Development.json`,
the committed `appsettings.json` shows `password=YOUR_PASSWORD`, and the index holds no
`appsettings` file under `bin/` (nothing under `bin/` at all). Verified with
`git grep --cached -i '<password>'` -> nothing, and `git log --all -S '<password>'` -> nothing,
so the secret never entered history and the MySQL password does **not** have to be rotated; it
simply must never be typed into `appsettings.json` again.

Evidence:

- `git show ':appsettings.json'` (the staged version) contained
  `server=localhost;port=3306;database=StudentOrganizationDB;user=root;password=<redacted>;`
  - the real password is deliberately not repeated in this document any more.
- `git show ':bin/Debug/net8.0/appsettings.json'` contains the same password, and that file is
  a NEW entry in the index (`git status` shows `A`), so it would be committed even if the root
  file is fixed.
- `git show HEAD:appsettings.json` still holds the safe placeholder
  (`password=YOUR_PASSWORD;`), so the secret is exactly one `git commit` away from entering
  history permanently and being pushed to GitHub.

Impact: every machine that uses that MySQL account is exposed. Rotating the password later does
not remove it from git history, and the repository is a school group project (shared remotes).

Fix: move the real connection string into a gitignored `appsettings.Development.json`, keep the
placeholder in the committed `appsettings.json`, remove the `bin` copy from the index, rotate
the MySQL password (treat the current one as burned) and never commit a real password again.

Re-verify: `git show :appsettings.json` shows only `YOUR_PASSWORD`, and
`git ls-files | Select-String 'appsettings'` lists nothing under `bin/`.

### P0.2 - Build output is tracked in git (121 of 166 files)  [x] fixed (Phase 0, 2026-09-27)

Done in Phase 0: `.gitignore` added (`bin/`, `obj/`, `appsettings.Development.json`, IDE and OS
noise) and the build output dropped from the index (`git reset -- bin obj`, the same result as
`git rm -r --cached bin obj`; the files stay on disk). `git ls-files` now lists 61 files instead of
166 and contains no `bin/` or `obj/` entry.

Evidence: 166 tracked files, 121 of them under `bin/` or `obj/`; the repository has no
`.gitignore` at all.

Impact: stale DLLs, `.pdb` files and MSBuild caches are committed and copied between machines,
the repository grows without limit, real diffs are buried in binary noise, and this is the
reason P0.1 exists (a password-bearing copy of `appsettings.json` inside `bin/`).

Fix: add `.gitignore` (`bin/`, `obj/`, `*.user`, `appsettings.Development.json`) and then
`git rm -r --cached bin obj`.

Re-verify: `git ls-files | Select-String '^(bin|obj)/'` returns nothing.

### P0.3 - The production error page does not exist  [x] fixed (Phase 0, 2026-09-27)

Done in Phase 0: `HomeController.Error` (`[AllowAnonymous]`, shows the request id, and the
exception text only while running in Development) plus `Models/ErrorViewModel`, the new
`Views/Home/Error.cshtml` with `.error-page` styles, `UseDeveloperExceptionPage` for Development,
`UseExceptionHandler("/Home/Error")` for Production,
`UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}")` for bare 404/403 answers, and
`Properties/launchSettings.json`, which pins `ASPNETCORE_ENVIRONMENT=Development` so the app runs
in the environment the audit expected.

Verified at runtime by injecting an exception (temporary controller, deleted afterwards):
Development rendered the real `InvalidOperationException`, Production rendered the friendly page
(`Error 500`, request id, no stack trace), and `GET /no-such-page-here` rendered `Error 404` in
both environments instead of a 404 with no explanation.

Evidence:

- `Program.cs`: `app.UseExceptionHandler("/Home/Error")` runs when not in development.
- No controller has an `Error` action (0 matches for `IActionResult Error`) and neither
  `Views/Home/Error.cshtml` nor `Views/Shared/Error.cshtml` exists.
- Nothing in the project sets `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT`, and there is no
  `launchSettings.json` (0 files), so the environment is not pinned to Development and falls
  back to the framework default, Production.
- `UseDeveloperExceptionPage` is also absent, so in Development an exception shows a bare 500.

Impact: any unhandled exception re-executes the pipeline to a route that does not exist, so the
user sees a 404 instead of a real error page - and during a defense or first launch the real
cause is much harder to diagnose.

Fix: add an `Error` action to `HomeController` with a matching view (keep the path in sync with
`UseExceptionHandler`), and add `UseDeveloperExceptionPage` for Development only.

Re-verify: throw temporarily inside an action and confirm a real error page renders in both
environments instead of a 404.

### P0.4 - Startup dies with an unclear error when MySQL is unavailable  [x] fixed (Phase 0, 2026-09-27)

Done in Phase 0: the DI registration now uses `MySqlServerVersion(new Version(8, 0, 36))`, so
startup no longer opens a database connection, and the initializer runs inside one `try/catch` that
logs a single line (innermost exception message only - no stack trace) and prints a readable
checklist before stopping with exit code 1.

Verified by pointing the app at a port with nothing listening (which is what a stopped MySQL looks
like): exit code 1, twelve lines of console output, the real cause
(`MySqlException: Unable to connect to any of the specified MySQL hosts.`) and the hint to check
`appsettings.Development.json`, with no stack trace anywhere in the output.

Evidence: `Program.cs` calls `ServerVersion.AutoDetect(connectionString)` and then
`DbInitializer.InitializeAsync(...)` (which runs `Database.MigrateAsync()`) before `app.Run()`,
with no try/catch anywhere in that path.

Impact: on another machine (for example the group leader's laptop) a stopped MySQL service or a
wrong password crashes the app with a raw MySqlConnector stack trace. Nothing explains that the
database is the problem, and the app cannot even start to display a friendly message.

Fix: use a fixed `new MySqlServerVersion(new Version(8, 0, 36))` so startup does not need a live
connection, and wrap the initializer in a try/catch that logs one clear, actionable line
("MySQL is not reachable - check DefaultConnection in appsettings.Development.json").

Re-verify: stop the MySQL service, start the app, confirm one readable message instead of a
stack trace.

### P0.5 - todo.md marked the hardcoded-password fix as done  [x] fixed (Phase 0, 2026-09-27)

Done in Phase 0: `todo.md` was brought in line with reality again. The password item is ticked only
because P0.1 is genuinely fixed now (placeholder committed, real login git-ignored), each Phase 0
item points at this file, and the unchecked items that remain are the real open ones.

Evidence: `todo.md` line 5 previously read
`- [x] Hardcoded DB password in appsettings.json ... Move password to environment variable or
use appsettings.Development.json (not committed)`.

Reality: the password is still in `appsettings.json` (see P0.1). A false "done" is worse than no
checkbox, because a future reader trusts it. The checkbox has been changed back to `[ ]` and now
points at P0.1; it must stay open until P0.1 is actually fixed.

The other two items in that section were checked and are genuinely done: `[Bind(...)]` is
present on every POST action (mass assignment) and there is no `FromSqlRaw`/`ExecuteSqlRaw`
anywhere (the SQL-injection item).

### P0.6 - Roles are decorative: no authorization is enforced  [x] fixed (Phase 0, 2026-09-27)

Done in Phase 0 (this is decision D3 resolved as "enforce Admin vs Officer on status changes"):
`Models/AppRoles.cs` holds the role names, `DbInitializer` seeds the `Officer` role through the new
`EnsureRoleAsync`, and `[Authorize(Roles = AppRoles.Admin)]` now guards `Edit` (GET and POST),
`Activate` and `Deactivate` in `MemberController` and `ActivityController`. Reading every list and
details page, registering members/activities and recording attendance stay open to any signed-in
user, because that is the Officer's day-to-day work.

Verified at runtime with a throw-away account that had the Officer role (created by copying the
seeded password hash, deleted afterwards): every read page returned 200, `GET /Member/Edit/1`,
`GET /Activity/Edit/1` and `POST /Member/Deactivate/1` were all refused with a redirect to
`/Account/AccessDenied`, and `Members.IsActive` stayed at 1.

**Closed 2026-10-02:** the user/role screen now exists (`UserController`), so a real Officer or
Member account can be created and a self-registered member can be approved. See P2.4 below and the
new "Roles: Admin / Officer / Member" section.

**Follow-up done 2026-10-02 - the admin-only controls are now hidden, and Create is Admin-only.**
The policy was extended (this is "option B"): `Create` (GET and POST) in `MemberController` and
`ActivityController` now also carries `[Authorize(Roles = AppRoles.Admin)]`, so registering a
member or adding an activity counts as administrative work. On the client side, every Admin-only
control is wrapped in `@if (isAdmin)`, where `isAdmin = User.IsInRole(AppRoles.Admin)`: the
`+ Add Member` / `+ Add Activity` buttons and the dashboard hero button, the Edit links, and the
Activate / Deactivate (Cancel) forms in `Views/Member/*` and `Views/Activity/*`. An Officer sees a
read-only note instead. `_Layout.cshtml` renders an extra "Administration" sidebar group and an
Admin badge in the top bar only for Admins, so the navigation itself communicates the role split.

Evidence (updated 2026-10-02): `Controllers/` has `[Authorize(Roles = AppRoles.Admin)]` on Create,
Edit, Activate and Deactivate in both controllers, and `IsInRole` is used in the four list/details
views and in `_Layout.cshtml`.

Runtime verification (2026-10-02, spare port 5211): signed in as Admin - `>Edit<`, `>Deactivate<`,
`+ Add Member`, `+ Add Activity` and the Admin badge are all present (`True`), and the Officer note
is absent. Signed in as a throw-away Officer - none of those controls are rendered (all `False`),
`>View<` is still present, and `GET /Member/Create`, `GET /Member/Edit/1` and
`POST /Member/Deactivate/1` all land on `/Account/AccessDenied`. `Members.IsActive` for member 1
stayed at 1, and the throw-away account was deleted afterwards.

Impact: the system looks role-aware but is not. Any authenticated account can create, edit and
deactivate every member and activity, and record attendance for anyone. There is also no screen
to create users or assign roles, so the seeded `admin` is the only account that can ever exist -
function #1 ("Login/User Accounts") is only half implemented.

Fix: decide the role policy first (D3 in section 8). Then apply
`[Authorize(Roles = "Admin")]` to destructive actions (Deactivate, and any Edit that can change
status) and add minimal user + role management.

Re-verify: create a non-admin user, log in, attempt a Deactivate POST and confirm it is rejected
(403) while read-only pages still work.

---

## 3. P1 - logic flaws inside modules that already exist

Ordered by how much damage they do, not by file order.

### P1.1 - Attendance quietly records every member as Present  [x] done (Phase 1)

Done in Phase 1: `Views/Attendance/Record.cshtml` no longer defaults to
"Present"; every radio group starts unmarked, a live counter shows how many
members are unmarked, and "Mark all present" / "Mark all absent" buttons were
added (decision D1). The POST writes a row only for a member that was marked.
Verified at runtime: a fresh activity rendered 0 checked radios and
"4 member(s) not yet marked"; after saving, all 4 came back pre-selected.

Evidence: `Views/Attendance/Record.cshtml` resolves the status as
`existing.ContainsKey(member.MemberID) ? existing[member.MemberID] : "Present"`, and the POST
action writes a row for every member that was submitted.

Impact: click Save on an untouched form and every absentee is recorded Present. The module's
core data is wrong by default, and every report or summary built on it inherits the error. This
is the single most damaging logic bug in the system.

Fix: make the state explicit - radio groups start unmarked, show a counter ("3 members not yet
marked"), and warn or block on save until every member is marked; or add "Mark all present" /
"Mark all absent" buttons (decision D1). Never invent a status.

Re-verify: open Record for an activity with no saved attendance, confirm nothing is
pre-selected, then confirm saving warns while members are unmarked.

### P1.2 - Attendance status is unvalidated free text  [x] done (Phase 1)

Done in Phase 1: new `Models/AttendanceStatus.cs` holds the only allowed values
(`Present`, `Absent`) plus `IsValid()`. `AttendanceController.Record` (POST)
drops anything that is not whitelisted, and only marks member ids that belong to
a real, active member. Verified: posting `Hacked` for every member was rejected
with an error and stored nothing.

Evidence: `AttendanceController.Record` (POST) stores `entry.Value` exactly as posted; nothing
checks it against "Present"/"Absent". `Attendance.Status` is `varchar(20)` with no check
constraint.

Impact: a typo, a stale form or a hand-crafted POST stores any string, silently breaking every
`status == "Present"` comparison used by badges, counters and future reports. The POST also
never verifies that the posted `MemberID` values are real, active members.

Fix: whitelist the allowed values server-side, reject or skip anything else, validate that each
member id belongs to an active member, and share the allowed values through one constants class
so the controller and the views cannot drift.

### P1.3 - Attendance can be recorded for a cancelled activity  [x] done (Phase 1)

Done in Phase 1: both `Record` (GET) and `Record` (POST) refuse an inactive
activity, show a readable message and send the user to `Details` so the saved
history stays visible. The Attendance `Index` shows a "Cancelled" badge and no
Record button, and `Details` hides its Update button. Verified at runtime for
GET and POST.

Evidence: neither `Record` (GET) nor `Record` (POST) checks `activity.IsActive`.

Fix: refuse to record when the activity is inactive (show a message) while keeping the saved
history readable.

### P1.4 - Duplicate-key inserts raise an unhandled 500  [x] done (Phase 1)

Done in Phase 1: `SaveChangesAsync()` in `MemberController.Create`,
`MemberController.Edit` and `AttendanceController.Record` is wrapped in a
try/catch for `DbUpdateException` and turned into a field/message error. The
friendly pre-checks stay. Verified: posting a duplicate Student ID returned the
field error with HTTP 200, and a padded duplicate created no second row.

Evidence: `MemberController.Create` and `ActivityController.Create` check for duplicates with a
SELECT and then INSERT. The unique index is the real guard, so two overlapping submissions throw
`DbUpdateException`, which nothing catches anywhere in the project.

Fix: keep the friendly pre-check, but also wrap `SaveChangesAsync()` in a try/catch for
`DbUpdateException` and convert it into the same field-level error message.

Re-verify: submit the same StudentID from two tabs at once and confirm a field error instead of
an error page.

### P1.5 - Dead concurrency handling in Member Edit  [x] done (Phase 1)

Done in Phase 1: the unreachable `DbUpdateConcurrencyException` block (and the
now-unused `MemberExists` helper) were deleted instead of faked, because no
model carries a concurrency token. The useful `DbUpdateException` guard replaces
it (see P1.4).

Evidence: `MemberController.Edit` catches `DbUpdateConcurrencyException`, but no model has a
concurrency token (`[Timestamp]`/rowversion), so that catch block can never run.

Fix: either add a real concurrency token (needs a migration, see D4) or delete the block. Dead
safety code is worse than none because it implies a guarantee that does not exist.

### P1.6 - Inputs are never trimmed or format-checked  [x] done (Phase 1)

Done in Phase 1: `MemberController.Create`/`Edit` and `ActivityController.Create`/
`Edit` trim every text field and re-validate the cleaned values
(`ModelState.Clear()` + `TryValidateModel`), so a stray space is accepted and
normalised. `Member.StudentID` gained the same `^\d{4}-\d{5}$` format rule the
registration form uses, which also rejects a padded lookalike outright. Verified:
`  2024-53372  ` was trimmed to `2024-53372` and matched the stored row.

Evidence: `StudentID`, `FullName`, `Course`, `YearLevel`, `ActivityTitle` and `Location` are
saved exactly as typed; only the `MemberController.Index` search trims its input.

Impact: `"2024-001 "` and `"2024-001"` can both exist if the server uses a NO PAD collation
(MySQL 8's default `utf8mb4_0900_ai_ci` is one), so the unique index does not catch lookalike
duplicates. Confirm with `SHOW VARIABLES LIKE 'collation_server'`.

Fix: `Trim()` before validating and saving, and add light format validation to `StudentID`
(for example `[RegularExpression(@"^\d{4}-\d{5}$")]`). Annotations do not change the schema.

### P1.7 - Attendance POST runs one query per member (N+1)  [x] done (Phase 1)

Done in Phase 1: `AttendanceController.Record` (POST) loads every existing row
for the activity with one `ToDictionaryAsync`, loads the valid, active member ids
with one query, then updates or adds from the dictionary. One read plus one
write, not one read per member. Verified at runtime with a 4-member save.

Evidence: the save loop calls `FirstOrDefaultAsync` inside `foreach (var entry in attendance)`.

Fix: load all existing rows for that activity in a single query into a
`Dictionary<int, Attendance>`, then update or add from it. Attendance for 50 members should be
one read plus one write, not 51 round trips.

### P1.8 - Updating attendance destroys the original timestamp  [x] done (Phase 1)

Done in Phase 1: `RecordedAt` is now written once and never overwritten.
`Attendance.UpdatedAt` was added (decision D4 - migration
`AddAttendanceUpdatedAt`) and is stamped only when a status actually changes.
`Views/Attendance/Details.cshtml` shows both columns. Verified: after flipping
every member's status, all `UpdatedAt` cells were populated while `RecordedAt`
stayed listed.

Evidence: `existing.RecordedAt = DateTime.Now;` overwrites the first recording time, and the
`Attendances` table has no `UpdatedAt` column.

Fix: stop overwriting `RecordedAt` (keep the first record time), or add `UpdatedAt` through a
migration (decision D4). Either way, the current behaviour permanently loses history.

### P1.9 - Login is minimal and has no recovery path  [x] done (Phase 1)

Done in Phase 1:

- `SuccessRehashNeeded` was already handled and `Account/ChangePassword` already
  exists (both landed with the 2026-10-02 user-management work).
- `returnUrl` is passed to the login view, re-cleaned on POST
  (`Url.IsLocalUrl`) and honoured after sign-in, so a user bounced off a page
  lands back on it. An external `returnUrl` is discarded (no open redirect).
- A cache-backed `Services/LoginAttemptTracker` locks a username + address pair
  out for 5 minutes after 5 failures and is reset on success.

Verified at runtime: `returnUrl=/Member/Index` ended on `/Member/Index`; an
external URL fell back to `/`; the 6th wrong attempt was blocked.

Evidence / gaps in `AccountController`:

- `SuccessRehashNeeded` from `VerifyHashedPassword` is treated as success but never re-hashed,
  so old password hashes never upgrade to the current algorithm.
- No `returnUrl` handling: a user bounced off a protected page always lands on the Dashboard.
- No failed-attempt counter, delay, lockout or rate limiting, so the form is brute-forceable.
- No change-password and no user management screen, so a lost password cannot be recovered from
  the app at all. The only working credentials are the seeded `admin` / `admin123`.

Fix: handle `SuccessRehashNeeded`, pass and honour `returnUrl`, add a simple attempt limiter,
and provide change-password plus minimal user management (shared with P0.6).

### P1.10 - Query tightening  [x] done (Phase 1)

Done in Phase 1: `HomeController.Index` was collapsed from six `COUNT` round
trips to two grouped queries (total + active per table) plus the two history
counts, all `AsNoTracking()`. `ActivityController.Index`, `Edit` (GET) and
`Details` now use `AsNoTracking()`, and the Attendance reads already did. All
pages still rendered (HTTP 200) at runtime.

Evidence: `HomeController.Index` issues 6 separate `COUNT` queries for one screen;
`ActivityController.Index` and most `Details`/`Edit` reads do not use `AsNoTracking()`.

Fix: collapse the dashboard into one projection (or 2-3 grouped queries), and add
`AsNoTracking()` to read-only queries.

### P1.11 - Two sources of truth for membership status  [x] done (Phase 2)

Done in Phase 2: decision **D2** settled the ownership. `Member.IsActive` stays
authoritative and `Membership.Status` now exists in practice:

- One `Membership` row per member (unique index on `Memberships.MemberID`, migration
  `AddUniqueMembershipPerMember`).
- A row is created automatically whenever a member is created (including
  self-registration, which starts it at "Inactive").
- `MembershipPolicy` mirrors the standing whenever `Member.IsActive` changes, so an inactive
  member can never keep a non-inactive standing.
- 3G lets an Admin set **Active** or **Suspended**; "Inactive" is system-managed.
- `Participations` is now written by 3H, so the dashboard's participation counter moves.

Evidence: `Member.IsActive` and `Membership.Status` both express "this person is a member", and
nothing ever inserts a `Membership` row (there is no Membership controller or view).
`Participations` is never written either, which is why the dashboard shows 0 participation
records.

Impact: `Memberships` and `Participations` are dead data today, and building 3G/3H on top of two
competing status fields will produce contradictions (an inactive member with an "Active"
membership, or the reverse), which then leaks into reports.

Fix: decide the ownership model before building 3G (decision D2) and write `Membership` rows
when a member is registered, or remove the table from the UI story entirely.

### P1.12 - Seed data is effectively empty  [ ] open

Evidence: `DbInitializer` creates only the Admin role and the `admin` / `admin123` account. The
running dashboard showed 1 member, 1 activity, 1 attendance record, 0 participation records.

Impact: the demo looks empty, and the new dashboard and future report screens cannot be judged
with one row of data.

Fix: add a development-only sample seeder (guarded by `app.Environment.IsDevelopment()` or an
explicit config flag) with roughly 20 members, 5 activities and some attendance. It must never
run against production data.

---

## 4. P2 - functions from the proposal that are not built

| Proposal # | Function | Status |
|---|---|---|
| 1 | Login / User Accounts | Done (2026-10-02) - login, roles, user CRUD, self-service registration + approval, and change password |
| 6 | Membership Management | Done (2026-10-03) - `MembershipController` + views; one row per member with standing Active/Suspended, mirrored to Inactive (decision D2) |
| 7 | Participation Monitoring | Done (2026-10-03) - `ParticipationController` + views; upsert per the `(ActivityID, MemberID)` unique index |
| 8 | Reports | Done (2026-10-03) - per-activity and per-member summaries with a print stylesheet |
| 9 | Search / Filtering | Done (2026-10-03) - topbar search + `SearchController` across members and activities; every list keeps its own filters |
| - | Member history (attendance + participation per member) | Done (2026-10-03) - Member Details shows the standing, counters and the 10 newest attendance/participation rows |
| - | Navigation from a member to their records | Done (2026-10-03) - sidebar entries, dashboard cards and cross-links between Members, Membership, Attendance, Participation and Reports |

Notes for whoever builds these:

- 3G Membership must follow the ownership decision in D2 so it cannot contradict
  `Member.IsActive`.
- 3H Participation must respect the unique `(ActivityID, MemberID)` index on `Participations`,
  so the screen has to be an upsert ("edit the participation type") rather than "add another
  row", or the second save throws a duplicate-key error (see P1.4).
- 3I Reports should read from existing tables only. Attendance rate = Present / recorded, and
  the report must state which members were never recorded (that distinction matters because of
  P1.1).
- 3J should extend the existing filter panels instead of adding a second, competing search UI.
- Every new module should follow the existing shape: `[Authorize]` controller, search/status
  filter, `TempData["Success"]` message, sidebar entry, no physical deletes.

---

## 5. P3 - consistency, polish and testing

- [x] `wwwroot/css/site.css` is dead code now: it is a comment stub, the layout links 11 real
      stylesheets and no longer includes it - delete it or make it the single documented index.
      **Done in Phase 3:** deleted (`git status` shows ` D wwwroot/css/site.css`). The layout links
      the 11 real sheets - base, sidebar, navbar, components, forms, tables, dashboard, attendance,
      participation, print, responsive - and every `href="/css/..."` in the rendered HTML was
      checked against `wwwroot/css`: all OK, nothing missing.
- [x] `login.css` is linked by `_Layout.cshtml`, so it loads on every page - move it into the
      Login page only. **Done in Phase 3:** the layout link is gone, so it no longer loads on every
      page. The file is linked only by the two standalone auth pages that actually use its
      split-screen classes: `Views/Account/Login.cshtml` and `Views/Account/Register.cshtml` (both
      render with `Layout = null`). Rendered check: the login page links it, the Portal does not.
- [x] Inline `style="..."` attributes remain in `Member/Create`, `Member/Details`,
      `Activity/Create`, `Activity/Edit`, `Activity/Details`, `Attendance/Details` and
      `Attendance/Record` - replace them with classes.
      **Done in Phase 3:** no view contains `style="`, `onsubmit=`, `onchange=` or `onclick=`.
      Scanning the rendered Member, Member/Create, Login and Attendance/Record pages counts
      `style="` = 0 and inline event handlers = 0. The two confirmations that used to be inline
      are now `data-confirm` attributes handled by `site.js`.
- [x] `Views/Attendance/Record.cshtml` contains an inline `<script>` block while other pages use
      `js/site.js` plus `@section Scripts` - move that behaviour into `site.js`.
      **Done in Phase 3:** no view contains a `<script>` block without a `src=`. The record-sheet
      behaviour (mark-all present/absent, live "not yet marked" counter, warn before saving) lives
      in `wwwroot/js/site.js`; the rendered Attendance/Record page loads only `/js/site.js`.
- [x] No pagination on the Members, Activities or Attendance lists. Fine at demo scale, but it
      should be documented as a known limit.
      **Done in Phase 3:** shared `Data/Paging.cs` (page size 20, clamped), `Models/Pagination.cs`
      and `Views/Shared/_Pager.cshtml` page the Members, Activities, Attendance, Membership and
      Participation lists, and the Prev/Next links carry the active search/status filters.
      Verified at runtime with 25 members: page 1 = 20 rows, page 2 = 5 rows, `?page=999` clamps
      to the last page instead of rendering empty, `?status=Active&page=2` = 4 rows with
      `status=Active` preserved in the pager links, and a search with no hits renders no pager.
- [x] There is no test project of any kind. The highest-value first tests (xUnit) would cover:
      duplicate StudentID, duplicate attendance, the attendance status whitelist, and the Index
      filters. These are exactly the areas where the P1 bugs live.
      **Done in Phase 3:** `StudentOrganizationSystem.Tests/` (xUnit 2.9.2, .NET 8) holds 35 tests
      covering exactly those areas plus the participation / membership / username unique indexes,
      the attendance and membership status whitelists, `ParticipationTypes.Normalize`,
      `MembershipPolicy` (Ensure / SyncStanding / idempotency), `LoginAttemptTracker`
      (lockout / reset / casing), the report row math, `PagedResult` windows and clamping, and the
      Members and Activities Index filters. The duplicate-key tests run against SQLite `:memory:`
      so the unique indexes are really enforced (the EF in-memory provider would not catch them).
      `dotnet test` -> **35 passed, 0 failed**.
- [x] Accessibility: no skip link; inactive nav items emit `aria-current="false"` (better to omit
      the attribute); verify focus visibility on the sidebar links and the radio toggles.
      **Done in Phase 3:** the skip link is the first focusable element and targets
      `<main id="main-content">`; `navAria()` returns `"page"` or `null`, so the rendered page
      contains exactly one `aria-current="page"` and never a `false` (scanned: 0 occurrences of
      `aria-current="false"`); `:focus-visible` rules cover links, buttons, inputs, selects,
      textareas and `[tabindex]` globally plus `.btn` / `.icon-btn`; the pager nav carries
      `aria-label="Pagination"` and `aria-current="page"` on the page number.
- [x] `notes.txt` only contains a `cd` command and is tracked in git - harmless noise.
      **Done in Phase 3:** deleted.

---

## 6. Self-critique of the sidebar UI redesign (this session's work)

Being honest about my own changes, not just the inherited code:

- [x] I added a CDN dependency (jsDelivr) for jQuery and jQuery Validation because `wwwroot/lib`
      was missing. If the demo laptop has no internet, client-side validation silently stops
      working (server-side validation still runs). Vendoring the files into `wwwroot/lib` is the
      safer long-term option. **Fixed in Phase 3:** `wwwroot/lib` now holds jQuery 3.7.1,
      jquery-validation 1.20.0 and jquery-validation-unobtrusive 4.0.0 locally;
      `_ValidationScriptsPartial.cshtml` loads them from `~/lib`, and a scan of the rendered pages
      finds 0 `cdn.jsdelivr` / `cdnjs` / `unpkg` references.
- [x] I left `wwwroot/css/site.css` in the project even though nothing links it any more.
      **Fixed in Phase 3:** the file was deleted (section 5).
- [x] `_Layout.cshtml` links `login.css` on every page (see section 5).
      **Fixed in Phase 3:** removed from the layout; only the standalone Login and Register pages
      link it.
- [x] The sidebar "Soon" items (Membership, Participation, Reports) are visible but inert. That
      communicates the roadmap honestly, but they must become real links as modules land, or they
      become permanent decoration. **Fixed in Phase 2:** 3G/3H/3I landed, and the sidebar now links
      `Membership/Index`, `Participation/Index` and `Reports/Index` for real.
- [x] I emit `aria-current="false"` on inactive nav items instead of omitting the attribute.
      **Fixed in Phase 3:** `navAria()` returns `"page"` or `null`, so inactive items omit the
      attribute entirely (rendered scan: one `aria-current="page"`, zero `false`).
- [ ] The login page's brand panel is decorative marketing copy. For a defense it would be better
      to show the organization/school identity and an adviser or contact line.
- [ ] I did not stop the running application, so `bin/` still holds the previous build - that is
      also why the final build reported the file-lock warnings MSB3026/MSB3027. Restart the app to
      see the new UI.
- [x] I deliberately did NOT exercise the attendance POST end to end, because it writes rows.
      It stays listed as unverified in section 0 and must be tested in Phase 1.

---

## 7. Work plan (phased; every phase ends with a build + runtime check)

### Phase 0 - protect the repo and make failures readable (no feature work)

| Step | Files |
|---|---|
| 0.1 `.gitignore` + `git rm -r --cached bin obj` | `.gitignore` (new), git index |
| 0.2 Move the connection string to a gitignored `appsettings.Development.json` (rotating the password turned out to be unnecessary - see P0.1) | `appsettings.json`, `appsettings.Development.json` |
| 0.3 Add the missing error endpoint | `Controllers/HomeController.cs`, `Views/Home/Error.cshtml` |
| 0.4 Fixed `MySqlServerVersion` + guarded `DbInitializer` with a clear message | `Program.cs` |
| 0.5 Correct `todo.md` (already done) and keep it pointing here | `todo.md` |
| 0.6 Apply the role policy decided in D3 | controllers + a user/role screen |

Acceptance: `git ls-files` shows no `bin/`/`obj/` entries and no real password; a stopped MySQL
produces one readable message; an injected exception renders the error page.

**Status: DONE - 2026-09-27.** Acceptance re-run at the end of the phase:

| Check | Command / action | Result |
|---|---|---|
| No build output tracked | `git ls-files \| Select-String '^(bin\|obj)/'` | empty (166 -> 61 tracked files) |
| No secret committed | `git grep --cached -i '<password>'` (and `git log --all -S`) | nothing; `AUDIT.md` itself was redacted |
| Real login still works locally | `appsettings.Development.json` (git-ignored) + `Properties/launchSettings.json` | sign-in as `admin` = 302, all pages 200 |
| Friendly error page | injected exception in Development and Production | Dev: real exception; Prod: `Error 500` + request id, no trace |
| Bad address | `GET /no-such-page-here` | `Error 404` page instead of a bare 404 |
| Stopped MySQL | app started against a dead port (3399) | exit code 1, one readable message, no stack trace |
| Role policy | throw-away Officer account | reads 200, `Edit`/`Deactivate` refused, row unchanged |
| Build | `dotnet build` on the final tree | succeeded, 0 warnings, 0 errors |

The temporary verification controller used to inject the exception was deleted, and the throw-away
Officer account and the extra app processes were removed afterwards.

### Phase 1 - correctness pass over the existing modules

| Step | Findings |
|---|---|
| 1.1 Attendance tri-state, status whitelist, active-activity guard | P1.1, P1.2, P1.3 |
| 1.2 Duplicate-key handling, Trim and format validation | P1.4, P1.6 |
| 1.3 Batched attendance load, preserve `RecordedAt` | P1.7, P1.8 |
| 1.4 Login: rehash, returnUrl, attempt limiting | P1.9 |
| 1.5 Query tightening (dashboard, AsNoTracking) | P1.10 |
| 1.6 Remove or replace the dead concurrency block | P1.5 |

Acceptance: attendance can no longer be saved with implicit or invalid values; duplicate input
produces a field error instead of a 500; the attendance POST is verified end to end (the one
untested write path); all pages still render.

Phase 1 executed and verified (2026-10-02): all ten items are done and each P1 entry above now
carries a "Done in Phase 1" note. A throw-away run against the built app exercised every affected
page and the attendance write path: every page returned 200, a fresh activity rendered no
pre-selected status, an unknown status was rejected without being stored, the cancelled-activity
guard refused both GET and POST, a padded duplicate Student ID produced a field error with no 500,
and `returnUrl` plus the sign-in lockout behaved as designed. The one thing not reproducible in a
sequential test is the `DbUpdateException` race itself - the pre-checks were verified, and the
catch is the safety net. `progress.md` section 18 records the script and the 26/26 result; the test
rows were deleted afterwards, so the database is back to its pre-run state.

### Phase 2 - finish the system

1. 3G Membership Management (+ write `Membership` rows on registration) - after D2
2. 3H Participation Monitoring (upsert per the unique index)
3. 3I Reports (per-activity summary, per-member summary, print stylesheet)
4. ~~Users and roles (user CRUD, role assignment, change password)~~ - **DONE 2026-10-02**
   (`UserController` with create/edit/approve/reject/activate/deactivate/reset-password, plus
   `Account/ChangePassword`). Roles were extended to Admin / Officer / Member, with a `Member`
   portal and a `Users.MemberID` link (migration `AddUserMemberLink`).
5. 3J unified search
6. Member history on Member Details

Acceptance: each new screen has search/filter, respects the unique indexes, follows the shared
module pattern, and moves the dashboard counters.

Phase 2 executed and verified (2026-10-03): 3G Membership, 3H Participation, 3I Reports, 3J
unified search and the member-history view are all built. Decision **D2** was taken first, which
is what unblocked 3G (and closed P1.11). A runtime pass over the built app drove every new page
and the two new write paths (31/31 checks): a new member received an "Active" membership, an
Admin set the standing to "Suspended", a crafted "Inactive" POST was refused, deactivating the
member mirrored the standing to "Inactive" and reactivating returned it to "Active";
participation was recorded (trimmed), updated, cleared by a blank box and rejected when too long;
both reports rendered with their print header; search found the member in both modules; and
Member Details showed the history. `progress.md` section 19 records the details, and the test
rows were deleted afterwards.

### Phase 3 - polish and testing

CSS cleanup (dead file, `login.css`, inline styles, inline script), pagination, an xUnit test
project for the high-risk logic, an accessibility pass, and richer dev-only seed data.

Acceptance: no dead or page-wide stylesheets, no inline styles/handlers/scripts in the views,
every long list pages with its filters preserved, `dotnet test` is green from a standing start,
the skip link and focus ring work, and a Development-only sample dataset makes an empty database
demo-able while a Production database stays clean.

Phase 3 executed and verified (2026-10-03): all eight items in section 5 are closed, each with its
own "Done in Phase 3" evidence note, and the four resolvable self-critiques in section 6 are
marked too. `site.css` and `notes.txt` are deleted, `login.css` is off the layout (Login/Register
only), every view is free of `style="` / inline handlers / inline `<script>` blocks, jQuery is
vendored in `wwwroot/lib`, and the five lists page through the shared `Data/Paging.cs` +
`_Pager.cshtml` (20 per page, filters carried). The new `StudentOrganizationSystem.Tests` project
runs **35 xUnit tests, all green**, including duplicate-key tests against a real SQLite in-memory
database. `DbInitializer.SeedSampleDataAsync` was verified on a throw-away database: Development
+ empty -> 5 members, 3 activities, 8 attendance rows, 4 participation rows, 5 memberships and an
`officer` / `officer123` account, a second start changed nothing (idempotent), and the same
installer under `ASPNETCORE_ENVIRONMENT=Production` created only the admin - **0 sample rows**.
The seeded and non-seeded databases were checked over HTTP (every page 200, officer sign-in works,
wrong password rejected) and both temporary databases were dropped afterwards; the real
`StudentOrganizationDB` still holds its pre-run rows. `progress.md` section 20 records the checks.

### Verification protocol to repeat after every phase

1. `dotnet build -o <temp folder>` so a running app cannot lock `bin`.
2. Start the built app on a spare port with `--urls http://127.0.0.1:<port>`.
3. Fetch the login page, POST a real login, then render every affected page.
4. Exercise one write path (for example an attendance POST) and re-read the page.
5. Stop the extra process and delete the temp output and logs.

---

## 8. Open decisions (these block implementation)

| ID | Decision | Recommendation |
|---|---|---|
| D1 | Attendance: allow a "not recorded" state (tri-state), or force a choice with bulk "all present / all absent" buttons? | **Decided (Phase 1).** Tri-state plus bulk buttons: radios start unmarked, a live counter warns before saving, and "Mark all present / all absent" fill the sheet. A member with no saved status simply gets no row. |
| D2 | Membership status: `Member.IsActive` as the single source of truth (mirrored into `Membership.Status`), or two independent values? | **Decided (Phase 2).** One `Membership` row per member (unique on `MembershipID` = per `MemberID`). `Member.IsActive` stays authoritative; `Membership.Status` is Active or Suspended and is mirrored to Inactive automatically, so the two can never contradict. "History" = the row's joined/updated dates plus the member's attendance and participation. |
| D3 | Roles: is everyone effectively an Admin, or are Admin vs Officer enforced? | Enforce at least on destructive actions (Deactivate, status changes) - the role claim and the UI already exist |
| D4 | `Attendance.RecordedAt` on update: accept the overwrite, or add an `UpdatedAt` column? | **Decided (Phase 1).** Added `Attendance.UpdatedAt` through migration `AddAttendanceUpdatedAt`; `RecordedAt` is written once and never overwritten. No concurrency token was added (there is no rowversion requirement yet). |

Decisions already taken:

- **D3 - decided (Phase 0).** Admin vs Officer *is* enforced, on status changes only: `Edit`
  (GET and POST), `Activate` and `Deactivate` in `MemberController` and `ActivityController` carry
  `[Authorize(Roles = AppRoles.Admin)]`. Reading, registering members/activities and recording
  attendance stay open to any signed-in user. Reversing this decision is a one-line change per
  action. The user/role screen that makes the policy usable stays in Phase 2 (item 4).

All four decisions (D1-D4) are now taken; nothing blocks Phase 3.

Unverified runtime behaviours worth checking first, because they change how much code must move:

- `attendance[<MemberID>]` binding into `Dictionary<int, string>` with `[FromForm]`
- `<select asp-for="IsActive">` pre-selecting the saved value on the Member and Activity Edit pages
- The actual server collation for the P1.6 assumption: **checked in Phase 0** -
  `SHOW VARIABLES LIKE 'collation_server'` returned `utf8mb4_0900_ai_ci`, which is a NO PAD
  collation, so `"2024-001 "` and `"2024-001"` really can both exist and the unique index will not
  catch that pair. **P1.6 stands** (trim the value before saving).

---

## 9. Pre-launch checklist (on the group leader's laptop)

- [ ] Install the .NET 8 SDK and MySQL Server
- [ ] Create the `StudentOrganizationDB` database (the app can also create it)
- [ ] Put the real connection string in `appsettings.Development.json` - never in the committed
      `appsettings.json`
- [ ] `dotnet restore` then `dotnet build`
- [ ] Start the app: `Program.cs` applies migrations automatically through `DbInitializer`, so
      `dotnet ef database update` is optional
- [ ] Sign in with the seeded account (`admin`) using the one-time password the app prints on
      the first run (or the configured `Seed:AdminPassword`), then change it when prompted
      (the account is held on the change-password page until it does)
- [ ] Walk through every module: Members, Activities, Attendance, then 3G/3H/3I as they land
- [ ] Confirm a bad URL or a thrown exception shows the error page, and that a stopped MySQL
      shows one clear message
- [ ] Confirm nothing sensitive is tracked:
      `git ls-files | Select-String 'appsettings|^(bin|obj)/'` must be empty

---

## Audit history

| Date | Change |
|---|---|
| 2026-09-27 | Initial audit (P0 x 6, P1 x 12, P2 gaps, P3 polish). `todo.md` corrected, `progress.md` section 15 added. |
| 2026-09-27 | Phase 0 executed and verified at runtime: P0.1-P0.6 fixed (each has a "Done in Phase 0" note), D3 decided, the password was redacted from this file, tracked files went 166 -> 61. `progress.md` section 16 added; Phase 1 is next. |
| 2026-10-02 | P0.6 follow-up: `Create` made Admin-only, and every Admin-only control hidden from Officers (plus an "Administration" nav group and an Admin badge for Admins). Verified at runtime as Admin and as a throw-away Officer. |
| 2026-10-02 | Third role added: **Member**. Self-service registration + admin approval, a read-only Member portal, a Users & Roles admin screen, and `Account/ChangePassword`. New migration `AddUserMemberLink` (`Users.MemberID`, unique, Restrict). Management controllers restricted to Admin/Officer. Verified end-to-end at runtime. |
| 2026-10-02 | **Phase 1 executed and verified** (26/26 runtime checks): P1.1-P1.10 closed. Tri-state attendance with bulk buttons and an `AttendanceStatus` whitelist, active-activity guard, `DbUpdateException` handling, dead concurrency block removed, trim + `StudentID` format validation, batched attendance write, `Attendance.UpdatedAt` via migration `AddAttendanceUpdatedAt`, `returnUrl` + sign-in lockout, tightened dashboard and `AsNoTracking()`. Decisions **D1** and **D4** taken (D2 still open). The two open runtime questions in section 0 are now answered. `progress.md` section 18 added. |
| 2026-10-03 | **Phase 2 executed and verified** (31/31 runtime checks): 3G Membership Management (one row per member, standing Active/Suspended mirrored to Inactive), 3H Participation Monitoring (upsert per the unique index), 3I Reports (per-activity + per-member summaries and a print stylesheet), 3J unified search (topbar + results page) and member history on Member Details. Decision **D2** taken; P1.11 closed. New migration `AddUniqueMembershipPerMember`. `progress.md` section 19 added. |
| 2026-10-03 | **Phase 3 executed and verified** (section 5 fully closed, 35/35 xUnit tests green): `site.css` and `notes.txt` deleted, `login.css` moved off the layout onto the Login/Register pages only, all inline styles/handlers/inline scripts removed from the views, jQuery vendored into `wwwroot/lib` (no CDN), pagination added to the five lists with filters preserved, skip link + `aria-current` + `:focus-visible` accessibility pass, a new `StudentOrganizationSystem.Tests` project (SQLite `:memory:` for the duplicate-key tests), and Development-only seed data (`SeedSampleDataAsync`: 5 members, 3 activities, attendance/participation rows, an `officer` account) verified idempotent on a throw-away database and verified **not** to run under Production. `progress.md` section 20 added. |
| 2026-10-11 | **`boltai.md` review, urgent items C1 + C2 done** (build 0 warnings, 73/73 tests green): **(C1)** the seeded admin's password is no longer `admin123` - it comes from `Seed:AdminPassword`, or the seeder generates one and `Program.cs` prints it once; `User.MustChangePassword` forces a change before any other page opens (`Services/MustChangePasswordPolicy`). **(C2)** `User.SecurityStamp` is minted into the cookie and `Program.cs` `OnValidatePrincipal` re-checks it (via `Services/SecurityStampGuard`, cached ~1 min), so a role change, deactivation or password reset ends the old session at once instead of after 8 h. One migration `AddUserSecurityFields`; new `SecurityHardeningTests`. `planned_features.md` step 1. |
| 2026-10-11 | **Part A UI tidy-up done** (`planned_features.md` step 2): sidebar "Change password" link removed (A1); the Profile page gained an **Account** card linking to `Account/ChangePassword` (A2); a reusable Back button - `Views/Shared/_BackButton.cshtml` + `.btn-back` CSS + a `[data-back]` handler in `site.js` - is used on Profile and the change-password page (A3). Verified with the full test run (73/73) and a runtime walk (sidebar footer has only Logout, `/Profile` = 200 with a Back button and one change-password link, change-password page = 200 with a Back link to `/Profile`). `progress.md` section 22 added. |





