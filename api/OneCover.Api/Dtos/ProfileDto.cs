// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for the logged-in user's profile (GET /api/me), including their FMLA eligibility
// Layer:    Dtos, the API boundary
// Concepts: a DTO is a deliberate SUBSET of the entity: no PasswordHash, no internal fields.
//           SsnLast4 goes out as the 4 digits; Angular's maskSsn pipe displays it as ***-**-1234 (Day 7)
// Related:  User + IEligibilityService (built from), ProfileService, MeController, Angular profile area (Day 7)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Dtos;

/// <summary>The logged-in user's profile. Never includes the password hash.</summary>
public record ProfileDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    Role Role,
    DateOnly HireDate,
    decimal AnnualSalary,
    int HoursWorkedLast12Months,
    string SsnLast4,          // only the last 4 are stored at all. the UI masks them for display
    bool IsFmlaEligible);
