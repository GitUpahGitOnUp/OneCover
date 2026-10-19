// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     EF Core implementation of IEnrollmentRepository
// Layer:    Repositories (data access)
// Concepts: EAGER LOADING w/ Include (pulls the related Plan in the SAME query, a SQL JOIN),
//           AsNoTracking for read-only lists, AnyAsync for fast existence checks, deferred execution,
//           exception filters (catch ... when) to translate a DB duplicate-key error into a business error
// Related:  IEnrollmentRepository, AppDbContext.Enrollments, Enrollment, Plan, EnrollmentService
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OneCover.Api.Data;
using OneCover.Api.Errors;
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Repositories;

public class EnrollmentRepository(AppDbContext db) : IEnrollmentRepository
{
    // .Include(e => e.Plan) = EAGER LOADING: "also load each enrollment's Plan". EF turns it into a JOIN, so it's
    // one query instead of one per enrollment. without it, e.Plan would be null and ToDto() would crash
    public async Task<IReadOnlyList<Enrollment>> GetByUserAsync(int userId) => await db.Enrollments
            .AsNoTracking()
            .Include(e => e.Plan)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledOn)
            .ToListAsync();

    // read-only lookup (AsNoTracking): ClaimService only reads it to check ownership + status, then links the
    // new claim by EnrollmentId, never by the navigation, so EF never mistakes it for a new enrollment.
    // Include(Plan): the benefit math needs the plan's percent, weekly max and elimination days
    public Task<Enrollment?> GetByIdAsync(int id) => db.Enrollments
            .AsNoTracking()
            .Include(e => e.Plan)
            .FirstOrDefaultAsync(e => e.Id == id);

    // AnyAsync = SELECT CASE WHEN EXISTS(...), stops at the first match. used to block duplicate enrollments
    public Task<bool> HasActiveEnrollmentAsync(int userId, int planId) => db.Enrollments
            .AnyAsync(e => e.UserId == userId && e.PlanId == planId && e.Status == EnrollmentStatus.Active);

    public void Add(Enrollment enrollment) => db.Enrollments.Add(enrollment);

    // saves EVERYTHING this request's DbContext is tracking, not just enrollments (see EnrollmentService).
    // if the filtered unique index rejects a duplicate (two enrolls racing), translate SQL Server's error into
    // the same friendly 400 the service's own check gives, instead of an unexpected 500.
    // the SQL-specific detail stays HERE in the data layer, so the service never needs to know about SQL error codes
    public async Task SaveChangesAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        // 2601 / 2627 = SQL Server's "duplicate key" error numbers (unique index / unique constraint).
        // 'when' = only catch THIS kind of DbUpdateException, anything else still bubbles up as a 500
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessRuleException("You're already enrolled in this plan.");
        }
    }
}
