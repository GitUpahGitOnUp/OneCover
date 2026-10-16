// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the EF Core implementation of IUserRepository. each method = one EF query or one save
// Layer:    Repositories (data access), the only layer that touches AppDbContext
// Concepts: implements an INTERFACE, DEPENDENCY INJECTION (AppDbContext), LINQ -> SQL translation,
//           change tracker (Add marks, SaveChangesAsync sends), scoped lifetime (matches the DbContext's)
// Related:  IUserRepository, AppDbContext.Users, AuthService, Program.cs (AddScoped<IUserRepository, UserRepository>)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Data;
using OneCover.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace OneCover.Api.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository. Turns simple method calls into database queries.
/// </summary>
// ": IUserRepository" = this class promises to provide every method the interface lists.
// AppDbContext gets injected by DI, a fresh one per request, so this class is scoped too
public class UserRepository(AppDbContext db) : IUserRepository
{
    // EF turns this into SELECT TOP(1) ... WHERE Email = @email and builds a User from the row (or null if none).
    // no async/await needed, FirstOrDefaultAsync already returns a Task so we just hand it back
    public Task<User?> GetByEmailAsync(string email) => db.Users.FirstOrDefaultAsync(u => u.Email == email);

    // AnyAsync = "is there at least one row?" stops at the first match, so it's fast
    public Task<bool> EmailExistsAsync(string email) => db.Users.AnyAsync(u => u.Email == email);

    // just tells EF's change tracker "insert this on the next save", nothing goes to SQL Server yet
    public void Add(User user) => db.Users.Add(user);

    // the one actual trip to the DB: sends all pending inserts/updates together in one transaction
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
