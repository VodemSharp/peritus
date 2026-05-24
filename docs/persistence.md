# Peritus — Persistence Guide

[← Back to main guide](../CLAUDE.md)

## EF Core & NoTracking

### Global NoTracking

The `IdentityDbContext` is configured with **global NoTracking**:

```csharp
options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
```

This means every query returns **detached entities** by default.

### Updating Entities with NoTracking

When you query an entity and then modify it, you **must explicitly attach it** before `SaveChangesAsync`:

```csharp
// Query returns detached entity
var session = await db.UserSessions.FirstOrDefaultAsync(...);

session.Status = UserSessionStatus.Terminated;

// ❌ WRONG — SaveChanges does nothing because entity is not tracked
await db.SaveChangesAsync();

// ✅ CORRECT — explicitly tell EF Core to track it as Modified
db.UserSessions.Update(session);
await db.SaveChangesAsync();
```

**All `Update()` calls in the codebase (search for `db.*.Update(`):**

- `RevokeUserSessionFeature`
- `RevokeAllSessionsFeature`
- `SignOutFeature`
- `RefreshTokenFeature`
- `ConfirmEmailFeature`
- `EnableTwoFactorFeature`
- `DisableTwoFactorFeature`
- `VerifyTwoFactorSetupFeature`
- `SignInTwoFactorFeature`
- `ChangePasswordFeature`
- `ResetPasswordFeature`
- `UpdateProfileFeature`
- `UserService.UpdateAsync`

**Rule of thumb:** If you query then modify, call `.Update()`. If you call `.AddAsync()`, no `.Update()` is needed.

### Computed Columns

Some properties are database-computed and `init`-only:

```csharp
// Entity
public Email NormalizedEmail { get; init; }

// Configuration
builder.Property(e => e.NormalizedEmail)
    .HasComputedColumnSql("LOWER(\"email\")", true)  // stored computed column
    .HasMaxLength(256)
    .ValueGeneratedOnAddOrUpdate();
```

EF Core reads these on materialization but will not write them.

### Entity Configurations

`IEntityTypeConfiguration<T>` classes are **co-located with entity classes** in the same `.cs` file, not in a separate
folder. Six entities have configurations: `User`, `UserSession`, `UserRecoveryCode`, `UserExternalLogin`, `Role`,
`UserRole`.

### Interceptors

Two interceptors are registered:

- `PerformanceInterceptor` — logs slow queries (>1s by default)
- `TimestampInterceptor` — auto-sets `CreatedAt` on add and `UpdatedAt` on add/modify for entities implementing
  `ICreatedEntity` / `IUpdatedEntity`

### Snake Case Naming

PostgreSQL columns use snake_case via `EFCore.NamingConventions`:

```csharp
options.UseSnakeCaseNamingConvention();
```

C# `Email` → SQL `email`.

## Database Migrations: DbUp (Not EF Core Migrations)

**Schema is managed by DbUp, not EF Core Migrations.**

- Scripts live in `src/modules/Peritus.Identity/Persistence/Scripts/` as embedded resources
- `Peritus.Migrator` is a background worker that runs DbUp on startup, then seeds admin role/user
- EF Core is used ONLY for querying, change tracking, and `ExecuteDeleteAsync`
- The `Migrations/` folder was deleted — do NOT recreate it

**Adding schema changes:**

1. Write a new `.sql` script with `CREATE TABLE IF NOT EXISTS` / `ALTER TABLE`
2. Add it as an embedded resource in `Peritus.Identity.csproj`
3. Update the corresponding EF entity + `IEntityTypeConfiguration<T>` (co-located in the entity `.cs` file)
4. Run integration tests — they fail immediately if model and schema drift
