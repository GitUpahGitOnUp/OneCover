// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     REPOSITORY INTERFACE = the contract for getting users in + out of the database
// Layer:    Repositories (data access). services call this, it's the only path to user data
// Concepts: ABSTRACTION (services see "get user by email", not SQL or EF), single responsibility (data access only),
//           testability (a FakeUserRepository w/ a List<User> could stand in, polymorphism)
// Related:  UserRepository (EF Core implementation), AuthService (the caller), Program.cs (AddScoped)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;

namespace OneCover.Api.Repositories;

/// <summary>
/// Data access for users. Services use this instead of AppDbContext directly.
/// </summary>
// interface = the contract. just method signatures, no code and no data. UserRepository fills in the "how".
// services (like AuthService) only ever see this, so tests can swap in a fake one
//
// about Task: Task is a class that stands for work that's still in progress, like a receipt for an order
// that isn't ready yet. the caller 'await's it to wait for the result w/o blocking a thread.
//   Task<User?> = "I'll hand you a User (or null) when the DB answers"
//   Task<bool>  = "I'll hand you true/false when the DB answers"
//   Task        = "I'll let you know when I'm done", no result, just finished (like void, but async)
public interface IUserRepository
{
    /// <summary>Finds a user by email. Returns null if no user has that email.</summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>True if an account already uses this email.</summary>
    Task<bool> EmailExistsAsync(string email);

    // not a Task b/c it doesn't hit the DB, it just marks the user to be inserted on the next save
    void Add(User user);

    /// <summary>Sends everything added or changed to the database, in one transaction.</summary>
    Task SaveChangesAsync();
}
