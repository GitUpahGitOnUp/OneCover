// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     entity, and a JOIN ENTITY. users <-> plans is many-to-many, so Enrollment sits in the middle.
//           each row = one specific user enrolled in one specific plan
// Layer:    Models (domain)
// Concepts: foreign key (UserId, PlanId = the columns) + navigation property (User, Plan = the objects in C#).
//           EF pairs them by name: User + Id = UserId. it's a full entity (not just a link table) b/c it has
//           its own data: EnrolledOn, Status
// Related:  User, Plan, BenefitClaim (claims are filed against an enrollment),
//           Payment.EnrollmentId (premiums are billed for one), IEnrollmentRepository / EnrollmentService (Day 4)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Models;

/// <summary>
/// A member's sign-up for a coverage plan. Connects a User to a Plan.
/// Claims are filed against an enrollment, and premium payments are billed for one.
/// </summary>

public class Enrollment
{
    public int Id  { get; set; }

    // foreign key (column) + navigation (object). required relationship: = null! means
    // "EF fills this in when it loads from the DB, trust me compiler"
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int PlanId { get; set; }

    public Plan Plan { get; set; } = null!;

    /// <summary>The date coverage started.</summary>
    public DateOnly EnrolledOn { get; set; }

    // explicit default, doesn't rely on Active happening to be the enum's first value
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}
