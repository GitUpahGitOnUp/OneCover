// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     EF Core implementation of IEnrollmentRepository
// Layer:    Repositories (data access)
// Concepts: EAGER LOADING w/ Include (pulls the related Plan in the SAME query, a SQL JOIN),
//           AsNoTracking for read-only lists, AnyAsync for fast existence checks, deferred execution
// Related:  IEnrollmentRepository, AppDbContext.Enrollments, Enrollment, Plan, EnrollmentService
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using OneCover.Api.Data;
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

    // AnyAsync = SELECT CASE WHEN EXISTS(...), stops at the first match. used to block duplicate enrollments
    public Task<bool> HasActiveEnrollmentAsync(int userId, int planId) => db.Enrollments
            .AnyAsync(e => e.UserId == userId && e.PlanId == planId && e.Status == EnrollmentStatus.Active);

    public void Add(Enrollment enrollment) => db.Enrollments.Add(enrollment);

    // saves EVERYTHING this request's DbContext is tracking, not just enrollments (see EnrollmentService)
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
