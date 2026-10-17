// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     entity = a C# class EF Core maps to a table. one User object = one row in Users
// Layer:    Models (domain), the bottom layer. never sent to the client directly (it has PasswordHash), DTOs are
// Concepts: EF Core conventions (property named Id = primary key, auto-increment), nullable reference types
//           (= string.Empty so a non-nullable string is never null), no encapsulation needed, no rules between fields
// Related:  AppDbContext.Users (the table), UserRepository (data access), AuthService (creates + checks users),
//           DbSeeder (demo users), Role enum
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

/// <summary>
/// A person who can log in: either an employee member or an admin
/// Holds the employment data used to calculate benefits and FMLA eligibility
/// </summary>
public class User
{
    // primary key by convention. SQL Server assigns it (identity column), we never set it ourselves
    public int Id { get; set; }

    // unique index + max length 256 set in AppDbContext.OnModelCreating
    public string Email { get; set; } = string.Empty;

    /// <summary> Hashed pswd from PasswordHasher, plain-text password is never stored. </summary>
    public string PasswordHash { get; set; } = string.Empty;

    // property named Role, of type Role (the enum). stored as text, e.g. "Admin"
    public Role Role { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    // DateOnly = calendar date, no time of day (maps to SQL "date")
    public DateOnly HireDate { get; set; }

/// <summary> Used to calculate weekly short-term disability benefit. </summary>
    // decimal for money, never double (no rounding errors)
    public decimal AnnualSalary { get; set; }

/// <summary>Used for FMLA eligibility which needs 1,250+ hours, plus 12+ months employed.</summary>
    public int HoursWorkedLast12Months { get; set; }

/// <summary>Only the last 4 digits are stored, never the full SSN. Displayed masked as ***-**-1234.</summary>
    // string, not int, so leading zeros survive ("0123" would become 123 as a number)
    public string SsnLast4 { get; set; } = string.Empty;

/// <summary>When the account was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
