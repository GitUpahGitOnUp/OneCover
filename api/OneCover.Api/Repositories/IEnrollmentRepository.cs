// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     REPOSITORY INTERFACE for enrollments
// Layer:    Repositories (data access)
// Concepts: ABSTRACTION, single responsibility, YAGNI (only the queries enrolling, "my coverage" and
//           filing a claim actually need)
// Related:  EnrollmentRepository (implementation), EnrollmentService (caller), Program.cs (AddScoped)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models;

namespace OneCover.Api.Repositories;

/// <summary>Data access for enrollments. Services use this instead of AppDbContext directly.</summary>
public interface IEnrollmentRepository
{
    /// <summary>A member's enrollments with their Plan loaded, newest first.</summary>
    Task<IReadOnlyList<Enrollment>> GetByUserAsync(int userId);

    /// <summary>One enrollment, or null. Used by ClaimService to check the member owns it before filing a claim.</summary>
    Task<Enrollment?> GetByIdAsync(int id);

    /// <summary>True if the member already has an ACTIVE enrollment in this plan.</summary>
    Task<bool> HasActiveEnrollmentAsync(int userId, int planId);

    void Add(Enrollment enrollment);

    /// <summary>Saves everything pending in this request's DbContext, including payments added through IPaymentRepository.</summary>
    Task SaveChangesAsync();
}
