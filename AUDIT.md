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

Still NOT verified at runtime (see section 8 before relying on them):

- Whether `attendance[<MemberID>]` binds into `Dictionary<int, string>` with `[FromForm]`
- Whether `<select asp-for="IsActive">` pre-selects the saved value on the Edit pages

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

Still open (moved to Phase 2, item 4): the user/role screen, without which no real Officer account
can be created, plus hiding the buttons a non-admin cannot use.

Evidence: `AccountController` puts `ClaimTypes.Role` into the auth cookie and the top bar
displays it, but `Controllers/` has 0 matches for `[Authorize(Roles = ...)]` and 0 for
`IsInRole`.

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

### P1.1 - Attendance quietly records every member as Present  [ ] open

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

### P1.2 - Attendance status is unvalidated free text  [ ] open

Evidence: `AttendanceController.Record` (POST) stores `entry.Value` exactly as posted; nothing
checks it against "Present"/"Absent". `Attendance.Status` is `varchar(20)` with no check
constraint.

Impact: a typo, a stale form or a hand-crafted POST stores any string, silently breaking every
`status == "Present"` comparison used by badges, counters and future reports. The POST also
never verifies that the posted `MemberID` values are real, active members.

Fix: whitelist the allowed values server-side, reject or skip anything else, validate that each
member id belongs to an active member, and share the allowed values through one constants class
so the controller and the views cannot drift.

### P1.3 - Attendance can be recorded for a cancelled activity  [ ] open

Evidence: neither `Record` (GET) nor `Record` (POST) checks `activity.IsActive`.

Fix: refuse to record when the activity is inactive (show a message) while keeping the saved
history readable.

### P1.4 - Duplicate-key inserts raise an unhandled 500  [ ] open

Evidence: `MemberController.Create` and `ActivityController.Create` check for duplicates with a
SELECT and then INSERT. The unique index is the real guard, so two overlapping submissions throw
`DbUpdateException`, which nothing catches anywhere in the project.

Fix: keep the friendly pre-check, but also wrap `SaveChangesAsync()` in a try/catch for
`DbUpdateException` and convert it into the same field-level error message.

Re-verify: submit the same StudentID from two tabs at once and confirm a field error instead of
an error page.

### P1.5 - Dead concurrency handling in Member Edit  [ ] open

Evidence: `MemberController.Edit` catches `DbUpdateConcurrencyException`, but no model has a
concurrency token (`[Timestamp]`/rowversion), so that catch block can never run.

Fix: either add a real concurrency token (needs a migration, see D4) or delete the block. Dead
safety code is worse than none because it implies a guarantee that does not exist.

### P1.6 - Inputs are never trimmed or format-checked  [ ] open

Evidence: `StudentID`, `FullName`, `Course`, `YearLevel`, `ActivityTitle` and `Location` are
saved exactly as typed; only the `MemberController.Index` search trims its input.

Impact: `"2024-001 "` and `"2024-001"` can both exist if the server uses a NO PAD collation
(MySQL 8's default `utf8mb4_0900_ai_ci` is one), so the unique index does not catch lookalike
duplicates. Confirm with `SHOW VARIABLES LIKE 'collation_server'`.

Fix: `Trim()` before validating and saving, and add light format validation to `StudentID`
(for example `[RegularExpression(@"^\d{4}-\d{5}$")]`). Annotations do not change the schema.

### P1.7 - Attendance POST runs one query per member (N+1)  [ ] open

Evidence: the save loop calls `FirstOrDefaultAsync` inside `foreach (var entry in attendance)`.

Fix: load all existing rows for that activity in a single query into a
`Dictionary<int, Attendance>`, then update or add from it. Attendance for 50 members should be
one read plus one write, not 51 round trips.

### P1.8 - Updating attendance destroys the original timestamp  [ ] open

Evidence: `existing.RecordedAt = DateTime.Now;` overwrites the first recording time, and the
`Attendances` table has no `UpdatedAt` column.

Fix: stop overwriting `RecordedAt` (keep the first record time), or add `UpdatedAt` through a
migration (decision D4). Either way, the current behaviour permanently loses history.

### P1.9 - Login is minimal and has no recovery path  [ ] open

Evidence / gaps in `AccountController`:

- `SuccessRehashNeeded` from `VerifyHashedPassword` is treated as success but never re-hashed,
  so old password hashes never upgrade to the current algorithm.
- No `returnUrl` handling: a user bounced off a protected page always lands on the Dashboard.
- No failed-attempt counter, delay, lockout or rate limiting, so the form is brute-forceable.
- No change-password and no user management screen, so a lost password cannot be recovered from
  the app at all. The only working credentials are the seeded `admin` / `admin123`.

Fix: handle `SuccessRehashNeeded`, pass and honour `returnUrl`, add a simple attempt limiter,
and provide change-password plus minimal user management (shared with P0.6).

### P1.10 - Query tightening  [ ] open

Evidence: `HomeController.Index` issues 6 separate `COUNT` queries for one screen;
`ActivityController.Index` and most `Details`/`Edit` reads do not use `AsNoTracking()`.

Fix: collapse the dashboard into one projection (or 2-3 grouped queries), and add
`AsNoTracking()` to read-only queries.

### P1.11 - Two sources of truth for membership status  [ ] open

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
| 1 | Login / User Accounts | Partial - login works; no user CRUD, roles or password change |
| 6 | Membership Management | Not started - model and table exist, no controller or views |
| 7 | Participation Monitoring | Not started - model and table exist, no controller or views |
| 8 | Reports | Not started - no per-activity summary, member summary or print view |
| 9 | Search / Filtering | Partial - Members (ID/name) and Activities (title/date/status) only |
| - | Member history (attendance + participation per member) | Not started - Member Details shows profile data only |
| - | Navigation from a member to their records | Not started |

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

- [ ] `wwwroot/css/site.css` is dead code now: it is a comment stub, the layout links 11 real
      stylesheets and no longer includes it - delete it or make it the single documented index.
- [ ] `login.css` is linked by `_Layout.cshtml`, so it loads on every page - move it into the
      Login page only.
- [ ] Inline `style="..."` attributes remain in `Member/Create`, `Member/Details`,
      `Activity/Create`, `Activity/Edit`, `Activity/Details`, `Attendance/Details` and
      `Attendance/Record` - replace them with classes.
- [ ] `Views/Attendance/Record.cshtml` contains an inline `<script>` block while other pages use
      `js/site.js` plus `@section Scripts` - move that behaviour into `site.js`.
- [ ] No pagination on the Members, Activities or Attendance lists. Fine at demo scale, but it
      should be documented as a known limit.
- [ ] There is no test project of any kind. The highest-value first tests (xUnit) would cover:
      duplicate StudentID, duplicate attendance, the attendance status whitelist, and the Index
      filters. These are exactly the areas where the P1 bugs live.
- [ ] Accessibility: no skip link; inactive nav items emit `aria-current="false"` (better to omit
      the attribute); verify focus visibility on the sidebar links and the radio toggles.
- [ ] `notes.txt` only contains a `cd` command and is tracked in git - harmless noise.

---

## 6. Self-critique of the sidebar UI redesign (this session's work)

Being honest about my own changes, not just the inherited code:

- [ ] I added a CDN dependency (jsDelivr) for jQuery and jQuery Validation because `wwwroot/lib`
      was missing. If the demo laptop has no internet, client-side validation silently stops
      working (server-side validation still runs). Vendoring the files into `wwwroot/lib` is the
      safer long-term option.
- [ ] I left `wwwroot/css/site.css` in the project even though nothing links it any more.
- [ ] `_Layout.cshtml` links `login.css` on every page (see section 5).
- [ ] The sidebar "Soon" items (Membership, Participation, Reports) are visible but inert. That
      communicates the roadmap honestly, but they must become real links as modules land, or they
      become permanent decoration.
- [ ] I emit `aria-current="false"` on inactive nav items instead of omitting the attribute.
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

### Phase 2 - finish the system

1. 3G Membership Management (+ write `Membership` rows on registration) - after D2
2. 3H Participation Monitoring (upsert per the unique index)
3. 3I Reports (per-activity summary, per-member summary, print stylesheet)
4. Users and roles (user CRUD, role assignment, change password) + enforced roles
5. 3J unified search
6. Member history on Member Details

Acceptance: each new screen has search/filter, respects the unique indexes, follows the shared
module pattern, and moves the dashboard counters.

### Phase 3 - polish and testing

CSS cleanup (dead file, `login.css`, inline styles, inline script), pagination, an xUnit test
project for the high-risk logic, an accessibility pass, and richer dev-only seed data.

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
| D1 | Attendance: allow a "not recorded" state (tri-state), or force a choice with bulk "all present / all absent" buttons? | Tri-state plus bulk buttons: it keeps data honest and the form fast |
| D2 | Membership status: `Member.IsActive` as the single source of truth (mirrored into `Membership.Status`), or two independent values? | Keep `Member.IsActive` authoritative; let `Membership.Status` express standing (Active / Inactive / Suspended) and never contradict `IsActive` |
| D3 | Roles: is everyone effectively an Admin, or are Admin vs Officer enforced? | Enforce at least on destructive actions (Deactivate, status changes) - the role claim and the UI already exist |
| D4 | `Attendance.RecordedAt` on update: accept the overwrite, or add an `UpdatedAt` column? | Add `UpdatedAt` (and optionally a real concurrency token) through a migration; document the schema change in progress.md, since the tables were declared final |

Decisions already taken:

- **D3 - decided (Phase 0).** Admin vs Officer *is* enforced, on status changes only: `Edit`
  (GET and POST), `Activate` and `Deactivate` in `MemberController` and `ActivityController` carry
  `[Authorize(Roles = AppRoles.Admin)]`. Reading, registering members/activities and recording
  attendance stay open to any signed-in user. Reversing this decision is a one-line change per
  action. The user/role screen that makes the policy usable stays in Phase 2 (item 4).

D1, D2 and D4 are still open and still block the work listed next to them.

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
- [ ] Sign in with the seeded account (`admin` / `admin123`) and change the password immediately
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





