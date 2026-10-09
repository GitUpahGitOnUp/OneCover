using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

/// <summary>
/// A person who can log in: either an employee member or an admin
/// Holds the employment data used to calculate benefits and FMLA eligibility
/// </summary>
public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary> Hashed pswd from PasswordHasher, plain-text password is never stored. </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly HireDate { get; set; }

/// <summary> Used to calculate weekly short-term disability benefit. </summary>
    public decimal AnnualSalary { get; set; }

/// <summary>Used for FMLA eligibility which needs 1,250+ hours, plus 12+ months employed.</summary>
    public int HoursWorkedLast12Months { get; set; }

/// <summary>Only the last 4 digits are stored, never the full SSN. Displayed masked as ***-**-1234.</summary>
    public string SsnLast4 { get; set; } = string.Empty;

/// <summary>When the account was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}