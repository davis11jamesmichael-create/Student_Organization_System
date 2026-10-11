# Development Guidelines — Student Organization System

## Code Quality Standards

### Naming Conventions
- **Classes**: PascalCase — `UserController`, `ApplicationDbContext`, `LoginAttemptTracker`
- **Methods**: PascalCase — `InitializeAsync`, `LoadLookupsAsync`, `ValidateMemberLinkAsync`
- **Private fields**: `_camelCase` prefix — `_context`, `_passwordHasher`, `_stampValidator`
- **Local variables**: camelCase — `adminRole`, `sessionAffected`, `linkedIds`
- **Constants**: PascalCase — `PasswordAlphabet`, `MOBILE_BREAKPOINT` (JS uses SCREAMING_SNAKE)
- **Test methods**: descriptive sentence style — `AttendanceStatus_whitelists_only_known_values`
- **Route comments**: `// GET: /Controller/Action` and `// POST: /Controller/Action` above every action

### File & Namespace Structure
- One class per file, filename matches class name
- Namespace: `StudentOrganizationSystem.<Layer>` (e.g., `.Controllers`, `.Models`, `.Data`, `.Services`)
- Test namespace: `StudentOrganizationSystem.Tests` (file-scoped namespace declaration)
- All model files use `System.ComponentModel.DataAnnotations` for validation attributes

### String Initialization
- String properties default to `string.Empty`, never `null` unless explicitly nullable:
  ```csharp
  public string StudentID { get; set; } = string.Empty;
  public string? AvatarPath { get; set; }  // nullable when truly optional
  ```

---

## Controller Patterns

### Class-Level Authorization
- Admin-only controllers get `[Authorize(Roles = AppRoles.Admin)]` on the class
- Mixed-access controllers use `[Authorize]` on the class and `[Authorize(Roles = AppRoles.Admin)]` on specific actions
- Use `AppRoles` string constants, never hardcode role names:
  ```csharp
  [Authorize(Roles = AppRoles.Admin)]
  public class UserController : Controller { ... }
  ```

### Constructor Injection
- Inject `ApplicationDbContext`, services, and `IPasswordHasher<User>` via constructor
- Field names follow `_camelCase` convention:
  ```csharp
  private readonly ApplicationDbContext _context;
  private readonly IPasswordHasher<User> _passwordHasher;
  private readonly SecurityStampGuard _stampValidator;
  ```

### Action Method Patterns
- GET actions: `[HttpGet]` attribute, return `View(model)` or `View(viewModel)`
- POST actions: `[HttpPost]` + `[ValidateAntiForgeryToken]` always together
- Null-check `id` parameter first, return `NotFound()` immediately:
  ```csharp
  if (id == null) return NotFound();
  var entity = await _context.Entity.FindAsync(id);
  if (entity == null) return NotFound();
  ```
- Trim string inputs at the start of POST actions: `model.Username = model.Username.Trim();`
- Re-populate ViewBag lookups before returning the view on validation failure

### Mass Assignment Prevention
- All POST action parameters use `[Bind("field1,field2,...")]` to whitelist allowed fields
- Never bind `CreatedAt`, `IsActive`, `SecurityStamp`, or other sensitive fields from POST

### DbUpdateException Handling
- Wrap `SaveChangesAsync()` in try/catch for `DbUpdateException` on create/edit operations
- Add a model-level error (`""` key) with a user-friendly message:
  ```csharp
  catch (DbUpdateException)
  {
      ModelState.AddModelError("", "That username is already taken, or the member already has an account.");
      return View(model);
  }
  ```

### Success/Error Feedback
- Use `TempData["Success"]` for success messages, `TempData["Error"]` for error messages
- Always redirect after a successful POST (`RedirectToAction(nameof(Index))`)
- Include the entity name in the message: `$"Account \"{user.Username}\" created."`

### Read-Only Queries
- Always use `.AsNoTracking()` on queries that don't modify data:
  ```csharp
  var users = await _context.Users
      .AsNoTracking()
      .Include(u => u.Role)
      .OrderBy(u => u.Username)
      .ToListAsync();
  ```

### Lookup Population
- Extract ViewBag population into a private `LoadLookupsAsync()` method
- Call it at the start of GET actions and before returning the view in POST actions

---

## Data Layer Patterns

### EF Core Query Style
- Chain `.AsNoTracking()` → `.Include()` → `.Where()` / `.OrderBy()` → `.ToListAsync()`
- Use `.AnyAsync()` for existence checks instead of `.CountAsync() > 0`
- Use `.FindAsync(id)` for primary key lookups when tracking is needed (edit/update)
- Use `.FirstOrDefaultAsync(predicate)` for single-record lookups with a filter

### Unique Index Enforcement
- All uniqueness is enforced at the database level via `OnModelCreating` unique indexes
- Controllers catch `DbUpdateException` as the safety net — never rely on application-level checks alone
- Application-level pre-checks (`.AnyAsync()`) are added for better UX error messages

### Relationship Configuration
- All FK relationships use `DeleteBehavior.Restrict` — no cascading deletes
- Navigation properties are configured in `OnModelCreating`, not via data annotations on FKs
- Optional relationships use nullable FK properties (`int? MemberID`)

### Seeding Pattern
- All seed methods are idempotent: check existence before inserting
- `EnsureRoleAsync` pattern: check → create if missing → return entity
- Dev-only data is guarded by `if (await context.Members.AnyAsync()) return;`
- Never seed in Production: `seedSampleData: app.Environment.IsDevelopment()`

---

## Model Patterns

### Data Annotations
- `[Key]` on primary key properties
- `[Required]` + `[StringLength(n)]` on all required string fields
- `[RegularExpression]` with `ErrorMessage` for format-validated fields
- `[Display(Name = "...")]` for human-readable labels in views
- `[ForeignKey("PropertyName")]` on navigation properties

### Enum-like Status Classes
- Status values are string constants in a static class, not C# enums:
  ```csharp
  public static class AttendanceStatus
  {
      public const string Present = "Present";
      public const string Absent = "Absent";
      public static bool IsValid(string? value) => ...
  }
  ```
- Always include an `IsValid(string? value)` method for server-side whitelisting
- Status classes include helper methods: `BadgeClass()`, `IsFilterable()`, `Selectable`

### Navigation Properties
- Collections initialized to `new List<T>()` in the property declaration
- Optional navigation properties are nullable: `public Role? Role { get; set; }`
- Required navigation properties are non-nullable with `!` suppression where needed

### View Models
- Separate view model classes for forms (never bind domain entities directly to POST)
- View models live in the `Models/` folder alongside domain entities
- Suffix: `ViewModel` for display models, no suffix for form models (e.g., `UserFormViewModel`)

---

## Security Patterns

### Security Stamp Invalidation
- Call `SecurityStamps.New()` and `_stampValidator.Evict(userId)` whenever role, active status, or password changes:
  ```csharp
  private void InvalidateSessions(User user, bool changed)
  {
      if (!changed) return;
      user.SecurityStamp = SecurityStamps.New();
      _stampValidator.Evict(user.UserID);
  }
  ```
- Only invalidate when the change actually affects session privileges (not a rename)

### Password Handling
- Always use `IPasswordHasher<User>` — never store plain text
- Blank password field in edit forms means "keep current password"
- Minimum password length: 6 characters (enforced in controllers and view model annotations)
- `MustChangePassword = true` on any admin-created or seeded account

### Last Admin Guard
- Always check `IsRemovingLastAdmin()` before deactivating or demoting an admin account
- Prevents the system from being locked out of administration

---

## JavaScript Patterns (site.js)

### Module Pattern
- All JS wrapped in an IIFE with `"use strict"`:
  ```js
  (function () {
      "use strict";
      document.addEventListener("DOMContentLoaded", function () { ... });
  })();
  ```

### DOM Interaction
- Use `document.getElementById()` and `querySelector()` — no jQuery in site.js
- Guard every element reference: `if (toggle) { ... }`
- Use `for` loops with index variables (ES5 style for broad compatibility)
- Use `Array.prototype.slice.call()` to convert NodeLists

### Data Attributes for Behavior
- `data-confirm="message"` on forms triggers a confirmation dialog before submit
- `data-back` on links prefers `history.back()` over the href
- `data-mark-all="value"` on buttons marks all attendance radios to a value
- `data-toggle-password` on buttons toggles password field visibility
- `data-member-row` on attendance rows for JS targeting

### Accessibility in JS
- Update `aria-expanded` on toggle buttons
- Update `aria-pressed` and `aria-label` on toggle buttons when state changes
- Close sidebar on Escape key

---

## Testing Patterns

### Test Organization
- One test class per concern: `ModelRulesTests`, `DatabaseConstraintTests`, `SecurityHardeningTests`
- File-scoped namespace declarations: `namespace StudentOrganizationSystem.Tests;`
- XML doc comment on each test class explaining what it covers

### xUnit Conventions
- `[Fact]` for single-case tests
- `[Theory]` + `[InlineData(...)]` for parameterized tests
- Test method names: `Subject_condition_expectedOutcome` (snake_case with underscores)
- Assert one logical thing per test; use `Assert.Single()`, `Assert.Equal()`, `Assert.Contains()`

### SQLite In-Memory for DB Tests
- Use `TestDb` helper for SQLite `:memory:` database in constraint tests
- Ensures unique indexes are enforced (unlike EF Core in-memory provider)

### Pure Logic Tests
- Test policy/status classes directly without a database (no mocking needed)
- `LoginAttemptTracker` tested with a real `MemoryCache` instance

---

## CSS Patterns

### CSS Custom Properties (Tokens)
- All colors, spacing, and typography defined as CSS variables in `base.css`
- Components reference tokens, never hardcode values

### Modular File Split
- One CSS file per concern; never mix sidebar rules into forms.css
- Load order matters: `base.css` → `sidebar.css` → `navbar.css` → feature CSS

### Class Naming
- BEM-inspired: `.app-shell`, `.app-main`, `.app-content`, `.nav-item`, `.radio-group`, `.radio-option`
- State modifiers: `.is-soon`, `.is-clear`, `.sidebar-open`, `.selected`
- Badge variants: `.badge-upcoming`, `.badge-ongoing`, `.badge-cancelled`, `.badge-finished`

---

## Audit Trail Conventions

### Code Comments
- Reference audit decisions inline: `// AUDIT.md P0.4`, `// decision D2`, `// C1`, `// C2`
- Explain *why* a guard exists, not just what it does
- Route comments above every controller action: `// GET: /User/Edit/5`

### Configuration Safety
- `appsettings.json` committed with placeholder values only (`YOUR_PASSWORD`, `""`)
- Real secrets go in `appsettings.Development.json` (git-ignored)
- Never put real credentials in any tracked file
