# Planned Features — to be done later

Status of this file: **mostly planning.** Part B (B1-B3) and Part C items **C3-C10**
are still only designs. **Part A** (A1-A3) and the two urgent Part C items
(**C1**, **C2**) are **implemented, tested and verified** (2026-10-11). Search this
file for "TODO" to find the items still to do.

Captured: 2026-10-04. Updated: 2026-10-11 (added **B3 - Activity comments**;
added **Part C - security & account hardening** from the `boltai.md` review;
**Messaging removed**; **C1 + C2 done**).
Search this file for "TODO" to find the actionable items.

---

## 0. Context / guardrails

- Stack: **ASP.NET Core 8 MVC + EF Core + MySQL**, Razor views, custom CSS
  (no Bootstrap at runtime), xUnit tests. 52 tests currently green.
- `progress.md` (section 2) lists **"Advanced notifications"** as *out of scope*
  and says *"Do not add major features outside the original proposal unless I
  specifically request them."* These features are a specific request, so they are
  allowed — but keep notifications **simple and in-app** (no email/SMS).
- Every module must follow the existing house style (see `AUDIT.md` section 4):
  `[Authorize]` controller, search/status filter, `TempData["Success"]` message,
  sidebar entry, **no physical deletes** (soft delete via `IsActive`).
- Reuse existing CSS classes (`form-card`, `page-header`, `badge-*`, `table`,
  `_Pager.cshtml`, `.alert`). Avoid inline `style="..."`, inline `on*=` handlers
  and inline `<script>` blocks (Phase 3 rules).
- **Part C** comes from `boltai.md` (an external security/account review). Its
  two "most urgent" items (C1 seed password, C2 cookie invalidation) are placed
  **ahead** of the modules in the order of work.

---

## PART A — Small UI changes (low risk)

### A1. Remove "Change password" from the sidebar  — DONE (2026-10-11) ✅
- **Where now:** `Views/Shared/_Layout.cshtml` lines ~162-165, the
  `<a class="sidebar-signout" asp-controller="Account" asp-action="ChangePassword">`
  block inside `.sidebar-footer`.
- **Change:** delete just that `<a>` block. Keep the **Logout** button below it.
  No CSS change required.

### A2. Move "Change password" into the Profile page  — DONE (2026-10-11) ✅
- **Where now:** `Views/Profile/Index.cshtml` (hint text around lines 91-95 already
  links to `Account/ChangePassword`).
- **Recommended approach:** keep the standalone `Account/ChangePassword` page and
  add an **"Account"** card inside `Profile/Index.cshtml` with a clear
  `Change password` button linking to it. Update the hint text so it no longer
  reads as "go somewhere else".
- **Alternative (more churn):** embed the change-password form directly in the
  Profile page (rename action, rework validation). Only do this if preferred.
- Also add a **Back** link on `Account/ChangePassword.cshtml` pointing to
  `Profile/Index` (uses A3 partial).

### A3. Add a "Back" button to the Profile page  — DONE (2026-10-11) ✅
- New partial `Views/Shared/_BackButton.cshtml`:
  an accessible `<a class="btn-back" data-back href="@fallback">← Back</a>` where
  `fallback` is a safe default (Home for staff, Portal for a Member login).
- `wwwroot/js/site.js`: add a handler for `[data-back]` that calls
  `history.back()` when a same-origin referrer exists, otherwise follows `href`
  (mirrors the existing `data-confirm` pattern — no inline JS).
- `wwwroot/css/components.css`: add `.btn-back`.
- Use the partial on `Profile/Index.cshtml` (and optionally `ChangePassword`).

**Part A - DONE and verified (2026-10-11).** The recommended approach for A2 was taken (the
standalone `Account/ChangePassword` page is kept). What was built:

- **A1:** the `<a class="sidebar-signout" ... ChangePassword>` block was deleted from
  `.sidebar-footer` in `Views/Shared/_Layout.cshtml`; Logout stays.
- **A2:** `Views/Profile/Index.cshtml` gained an **Account** card with a `Change password` primary
  button linking to `Account/ChangePassword`, and the old "use … to change your password" hint text
  was simplified to describe the picture only.
- **A3:** new `Views/Shared/_BackButton.cshtml` partial (`<a class="btn-back" data-back href="...">`).
  The fallback comes from `ViewData["BackFallback"]`, else Home for staff / Portal for a Member.
  `wwwroot/js/site.js` handles `[data-back]` (history.back() on a same-origin referrer, otherwise the
  href), and `wwwroot/css/components.css` adds `.btn-back` + `.page-back`. Used on `Profile/Index`
  and on `Account/ChangePassword` (which sets `BackFallback` to the Profile page).

**Verified:** `dotnet build` (0 warnings) and `dotnet test` (73/73). Runtime walk on a throw-away
database: sign-in → sidebar footer has **no** "Change password" (only Logout) → `/Profile` returns
200 with a Back button and exactly one `/Account/ChangePassword` link → that page has a Back link
pointing at `/Profile`.

---

## PART B — Three new modules

> Each module needs: model + EF migration + controller + views + dev seed +
> tests + sidebar entry + (optional) dashboard/portal surface. Build in the
> sequence below.

### B1. Announcements  (build first — natural quick win)  — TODO
- **Model `Announcement`:** `AnnouncementID` PK, `Title` (150, Required),
  `Body` (Required), `Audience` (`All` / `Members` / `Staff`), `IsPinned`,
  `IsActive` (soft delete), `CreatedByUserID` FK→User (nullable),
  `CreatedAt`, `UpdatedAt`.
- **Controller `AnnouncementController`:**
  - `[Authorize]` read: `Index` (search + paging, pinned-first then newest),
    `Details`.
  - `[Authorize(Roles = AdminOrOfficer)]` write: `Create`, `Edit`,
    `Deactivate` / `Activate`.
- **Views:** `Index`, `Create`, `Edit`, `Details` (reuse card/table/badge CSS).
- **Surfaces:** new sidebar **"Communication"** section (staff **and** Member);
  latest 3 on the Home dashboard and on `Portal/Index`.

### B2. Notifications  (in-app only)  — DONE (2026-10-11) ✅
- **Model `Notification`:** `NotificationID` PK, `UserID` FK (recipient),
  `Title`, `Message`, `Link` (nullable, local path only), `Category`,
  `IsRead`, `CreatedAt`, `ReadAt`.
- **Service `Services/NotificationService.cs`** (follow existing `Services/`
  style), called on key events: new announcement, activity created/cancelled,
  account approved, message received.
- **Controller `NotificationController`** `[Authorize]`: `Index` (my
  notifications, unread first), POST `MarkRead`, `MarkAllRead`.
- **Topbar bell** with unread badge. Because `_Layout` renders on every page,
  supply the count via a **`ViewComponent`** (`NotificationBell`) instead of a DB
  call in the layout.
- **View:** `Index` + a dropdown/panel.

### B3. Activity comments — everyone can discuss an activity  — DONE (2026-10-11) ✅
- **Why:** *everyone* — **Admin, Officer and Member** — should be able to leave a
  comment under an activity, so an event has one shared discussion thread. This
  is the one module whose read **and** write access is deliberately **not**
  staff-only.
- **Model `ActivityComment`:** `CommentID` PK, `ActivityID` FK→Activity
  (Required), `UserID` FK→User (the author; Required — a comment always has a
  signed-in author), `Body` (Required, `StringLength(1000)`), `IsActive`
  (soft delete), `CreatedAt`, `UpdatedAt`. Add an
  `ICollection<ActivityComment> Comments` navigation on `Activity`.
- **Controller `ActivityCommentController`** — `[Authorize]` with **no role
  list**, so every signed-in user (a Member login included) can reach it:
  - GET `Index(int activityId)` — comments for one activity, oldest first, with
    inactive (soft-deleted) rows filtered out.
  - POST `Create(int activityId, string body)` — trim and length-check the body,
    then save; `TempData["Success"] = "Comment added."`.
  - GET/POST `Edit(int id)` and POST `Delete(int id)` (soft delete): **author
    only, plus Admin moderation** — an Officer cannot touch another user's
    comment.
  - Resolve the current user from `ClaimTypes.NameIdentifier`, exactly like
    `ProfileController.GetCurrentUserAsync` — never from the URL — so nobody can
    post as, edit or delete another user's comment.
- **Views:** a reusable `Views/Shared/_CommentList.cshtml` partial (thread + "Add
  comment" form, anti-forgery token, no inline JS/CSS) dropped into:
  - `Activity/Details.cshtml` (staff side), and
  - a new **`Portal/Activities`** action + `Views/Portal/Activities.cshtml` on
    the Member side. A Member cannot open `ActivityController` (it is
    `AdminOrOfficer`), so the Portal needs its own entry point; scope the list to
    the signed-in member's own activities (their attendance/participation
    activities, plus active upcoming ones) — never to a request-supplied id.
- **Surfaces:** optional "latest comments" strip on the Home dashboard and on
  `Portal/Index`; a **Discussions** entry on the Member nav beside the two
  existing Portal links. Staff reach the thread through `Activity/Details`, so
  no new staff sidebar section is needed.
- **Cross-cutting:** new `DbSet<ActivityComment>` and two
  `DeleteBehavior.Restrict` relationships; migration `AddActivityComments`;
  guarded dev seed in `DbInitializer`; tests for the body rules, the
  author-or-admin delete rule, and the ordering/inactive filter.

### Cross-cutting (Part B)
- **`Data/ApplicationDbContext.cs`:** add one `DbSet` per new model (3 total:
  announcements, notifications, activity comments) + relationships
  using `DeleteBehavior.Restrict` (match existing style).
- **Migrations:** e.g. `AddAnnouncements`, `AddNotifications`,
  `AddActivityComments`.
- **`Data/DbInitializer.cs`:** extend `SeedSampleDataAsync` (guarded, idempotent,
  Development-only, as today).
- **Tests** in `StudentOrganizationSystem.Tests`: pure rules — audience
  filtering, notification creation, and comment rules
  (body trim/length, author-or-admin delete, inactive rows excluded) — in the
  existing style.
- **Docs:** update `todo.md`, `progress.md`, `AUDIT.md` to keep the repo honest.

---

## PART C — Security & account hardening (from the `boltai.md` review)

> Source of truth: `boltai.md`. Every item below was checked against the code and
> is real. Items **C1** and **C2** are the review's "most urgent" and are done
> **first** (see the order of work below).

### C1. Remove the hardcoded admin password  — DONE (2026-10-11) ✅
- **Where now:** `Data/DbInitializer.cs:50-64` (`"admin123"`) and the dev-only
  sample account `"officer123"` at `:168-177`.
- **Risk:** anyone who has read the repo can sign in as a fresh deployment's
  admin.
- **Change:**
  - The first admin's password comes from `Seed:AdminPassword`
    (config / env / user-secrets). If that is not set, generate a random
    password and print it **once** to the console on first run.
  - Add `MustChangePassword` (bool, default `true` for the seeded admin) to
    `User` (migration `AddUserSecurityFields`), and force a change on first
    sign-in (redirect to `Account/ChangePassword`, block other pages until done).
  - Keep `officer123` as a Development-only sample (it already only exists when
    `seedSampleData` is true — `Program.cs:76`), or move it behind the same
    config switch.
- **DONE (2026-10-11):** the first admin's password now comes from `Seed:AdminPassword`
  (config / env / user-secrets); when it is not set the seeder generates a random 16-character
  password and `Program.cs` prints it **once** on first run (never written to a file or a log).
  `User.MustChangePassword` (migration `AddUserSecurityFields`) is carried in the cookie, and
  `Services/MustChangePasswordPolicy` (used by a `Program.cs` middleware) keeps every other page
  closed until the password is replaced. `officer123` stays a Development-only sample.
  Tests: `SecurityHardeningTests`.

### C2. Invalidate the auth cookie on role change / deactivation  — DONE (2026-10-11) ✅
- **Where now:** `Program.cs:47` sets `ExpireTimeSpan = 8h` with sliding
  expiration and there is **no** `OnValidatePrincipal`, so a demoted or
  deactivated user keeps their old role for up to 8 h. Claims are minted in
  `Services/UserClaims.cs`.
- **Change (security-stamp pattern):**
  - Add `SecurityStamp` (string) to `User`; mint it into the cookie as a claim in
    `UserClaims.Build`.
  - `Program.cs`: add `options.Events.OnValidatePrincipal` that loads the user's
    current stamp + `IsActive` / `RoleID` and calls `RejectPrincipal()` +
    `SignOutAsync()` on a mismatch. Cache the lookup in `IMemoryCache` (~1–2 min)
    so it is not a DB round trip on every request.
  - Bump the stamp (invalidate) in `UserController.Edit`, `Deactivate`,
    `ResetPassword`, and `AccountController.ChangePassword`; also on `Reject`.
- **DONE (2026-10-11):** `User.SecurityStamp` (same migration) is minted into the cookie
  (`AppClaims.SecurityStamp`); `Program.cs` adds `OnValidatePrincipal`, which re-reads the account
  through `Services/SecurityStampGuard` (cached ~1 min) and rejects the cookie on a stamp / role /
  active mismatch. The stamp is rotated in `UserController.Edit`, `Deactivate`, `Activate`,
  `ResetPassword` and `AccountController.ChangePassword`; `DbInitializer` backfills existing rows.
  Tests: `SecurityHardeningTests`.

### C3. Audit logging for administrative actions  — TODO
- **Model `AuditLog`:** `AuditLogID` PK, `ActorUserID` FK→User (nullable),
  `Action` (e.g. `User.Create`), `EntityName`, `EntityID` (nullable),
  `Details` (nullable), `IpAddress` (nullable), `CreatedAt`.
- **Service `Services/AuditService.cs`** (follow the existing `Services/` style:
  `LoginAttemptTracker`, `UserClaims`, `AvatarUpload`): one
  `RecordAsync(actorId, action, entity, entityId, details)` an action calls after
  a successful save.
- **Wire into** every state-changing action in `UserController` (Create, Edit,
  Approve, Reject, Deactivate, Activate, ResetPassword); optionally Member and
  Activity changes later.
- **Views:** `AuditLogController` `[Authorize(Roles = AppRoles.Admin)]` `Index`
  with search + filter + `_Pager.cshtml`, read-only (append-only).
- **Cross-cutting:** `DbSet<AuditLog>` with `DeleteBehavior.Restrict`; migration
  `AddAuditLogs`; sidebar entry under **Administration**.

### C4. Validate the submitted Role ID before saving  — TODO
- **Where now:** `UserController.Create` `catch (DbUpdateException)` `:103-117`
  and `Edit` `:217-228` map **any** `DbUpdateException` to "That username is
  already taken…". An invalid `RoleID` (an FK violation) therefore shows that
  misleading message.
- **Change:** before saving, check the role exists
  (`_context.Roles.AnyAsync(r => r.RoleID == model.RoleID)`); add a `ModelState`
  error on `RoleID` ("Choose a valid role."). Narrow the catch so the duplicate
  message is only shown for a genuine unique-index violation (inspect
  `DbUpdateException.InnerException` / `MySqlException.Number`).
- **Tests:** an unknown role id produces a field error, not a 500 and not the
  duplicate-username text.

### C5. Self-deactivation guard on the Edit action  — TODO
- **Where now:** `Deactivate` already refuses to deactivate the signed-in
  account (`:347-353`), but **Edit** POST (`:155-235`) has no such guard, so a
  user can deactivate or demote themselves by editing their own row.
- **Change:** in `Edit` POST, if `id == GetCurrentUserId()` and the posted
  `IsActive` is false (or the posted role removes admin), add a `ModelState`
  error and refuse — mirroring the `Deactivate` message.

### C6. Super-admin tier so one admin cannot demote a peer  — TODO
- **Where now:** the only protection is `IsRemovingLastAdmin` (`:483-518`),
  which stops the *last* admin being removed but lets any admin demote another.
- **Change (recommended: a flag, not a new role):** add `IsSuperAdmin` to `User`;
  the seeded first admin is the super-admin; a normal Admin cannot edit, demote,
  deactivate or reset the password of another Admin, and the role dropdown in
  `User/Create` / `User/Edit` hides Admin for non-super-admins.
- **See Open decision 7** for the alternative (a fourth `SuperAdmin` role).

### C7. Auto-deactivate long-dormant accounts  — TODO
- **Where now:** `User` has no `LastLoginAt`; there is no way to spot graduated
  students who never sign in again.
- **Change:** add `LastLoginAt` (nullable) set in `AccountController.Login` on a
  successful sign-in; add `Services/DormantAccountPolicy.cs` with a pure
  `IsDormant(lastLoginAt, thresholdDays, now)` rule, and an explicit
  Admin-only action (button on `User/Index`) that deactivates every account past
  the threshold — showing a count first. **No background scheduler exists in
  this app**, so keep it a manual action rather than a hosted service.
- **Tests:** the pure dormancy rule (edge days, null `LastLoginAt`).

### C8. Bug — synchronous DB calls inside an async method  — TODO
- **Where now:** `UserController.IsRemovingLastAdmin` (`:488-518`) uses the
  synchronous `FirstOrDefault` / `Count` even though both callers are `async`.
- **Change:** switch to `FirstOrDefaultAsync` / `CountAsync`, make the method
  `async Task<bool>`, and `await` it in `Edit` POST and `Deactivate`.

### C9. Bug — two-step delete in `Reject` can orphan a Member row  — TODO
- **Where now:** `UserController.Reject` (`:313-326`) saves the user removal,
  then in a **second** `SaveChangesAsync` deletes the `Members` row. If that
  second save fails, a Member row is left with no login and no owner.
- **Change:** make it atomic — detach `user.MemberID`, remove the `User` **and**
  the linked `Member` in one change set and call `SaveChangesAsync()` once (or
  wrap both in an explicit `_context.Database.BeginTransactionAsync()`), so the
  pair can never be half-removed.

### C10. Bug — `DateTime.Now` used everywhere instead of `DateTime.UtcNow`  — TODO
- **Where now:** model defaults (`Activity`, `Member`, `User.CreatedAt`…),
  `ActivityController`, `UserController.Approve` (`:259`), and the `DbInitializer`
  seeds all use local `DateTime.Now`, so stored timestamps shift with the
  server's time zone / DST.
- **Change:** standardize on `DateTime.UtcNow` for all stored timestamps and
  render local time in the views (a small `ToLocalTime()` display helper keeps
  the conversion in one place). See Open decision 9 for the `TimeProvider`
  alternative.

---

## Open decisions (need a call before building)

1. **Change-password placement:** standalone page linked from Profile
   *(recommended)* vs. embedded form inside Profile.
2. **Notifications:** in-app only *(recommended; matches your scope note)*
   vs. also email.
3. **Comment moderation:** who may hide/delete a comment — author + Admin
   *(recommended)* vs. author only vs. any staff (Admin **and** Officer).
4. **Member comment surface:** a new `Portal/Activities` page *(recommended —
   members cannot open `ActivityController`)* vs. squeezing the thread into the
   existing `Portal/History` list.
5. **Seed admin password (C1):** generate a random one and print it once, with
   `MustChangePassword` *(recommended)* vs. read a `Seed:AdminPassword`
   setting/env var vs. require the operator to set it via user-secrets.
6. **Session revocation (C2):** security-stamp claim + `OnValidatePrincipal`
   re-check *(recommended, standard ASP.NET Core pattern)* vs. shortening
   `ExpireTimeSpan` only (weaker — does not revoke immediately).
7. **Super-admin tier (C6):** an `IsSuperAdmin` flag on `User` *(recommended —
   no new role, keeps the three-role model)* vs. a fourth `SuperAdmin` role.
8. **Dormant accounts (C7):** an explicit Admin "deactivate dormant accounts"
   action with a configurable threshold days *(recommended — there is no
   background scheduler in this app)* vs. add a hosted background service.
9. **UTC vs. local time (C10):** switch storage to `DateTime.UtcNow` everywhere
   and render local in views *(recommended)* vs. introduce .NET 8 `TimeProvider`
   for testability (more churn).
10. **Phasing:** the recommended sequence below vs. all at once.

---

## Recommended order of work

> Priority comes from `boltai.md`: the two **urgent** security items are done
> first, ahead of the cosmetic tidy-up and the new modules.

1. **[DONE 2026-10-11] C1 + C2 (Part C — urgent)** — the hardcoded seed password is
   gone (config value, else generated + printed once, with a forced change) and the
   auth cookie now revalidates on role change / deactivation. Migration
   `AddUserSecurityFields`; `SecurityHardeningTests` added; build 0 warnings, 73 tests green.
2. **[DONE 2026-10-11] Part A** — the sidebar "Change password" link was removed, the Profile page
   gained an Account card linking to it, and a reusable Back button (partial + `.btn-back` +
   `data-back` handler) was added.
3. **[DONE 2026-10-11] B1 Announcements** — self-contained; gives Notifications something to fire on.
4. **[DONE 2026-10-11] B2 Notifications** — depends conceptually on B1 events.
5. **[DONE 2026-10-11] B3 Activity comments** — small, but touches both the staff
   `Activity/Details` page and a new Member `Portal/Activities` page; do it last
   of the modules so the Portal surface already exists.
6. **C3–C10 (Part C — recommendations + bug fixes)** — audit logging, role
   validation, the self-deactivation guard on `Edit`, super-admin tier, dormant
   accounts, and the async / atomic-`Reject` / UTC clean-ups. These are lower
   risk than C1/C2 and can land after the modules.

Each step ends with: `dotnet build` (0 warnings), `dotnet test` (all green),
and a short manual page walk.