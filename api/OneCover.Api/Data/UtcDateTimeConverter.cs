// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     tells EF Core "every DateTime in this database is UTC" when it reads values back
// Layer:    Data (an EF Core value converter, applied to all DateTime properties in AppDbContext.ConfigureConventions)
// Concepts: SQL Server's datetime2 has no time zone, so EF reads values back as DateTimeKind.Unspecified.
//           System.Text.Json then writes them WITHOUT a "Z", and browsers treat "2026-10-21T14:00:00" as LOCAL time,
//           shifting every timestamp. we always SAVE UTC (TimeProvider.GetUtcNow), so on READ we just label it UTC.
//           schema-neutral: the column type doesn't change, so no migration is needed
//           INHERITANCE: : ValueConverter<TModel, TProvider> gets EF's conversion plumbing; we supply the 2 lambdas
// Related:  AppDbContext.ConfigureConventions, User.CreatedAt, BenefitClaim.SubmittedAt, ClaimStatusHistory.ChangedAt
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OneCover.Api.Data;

/// <summary>
/// Marks DateTimes read from the database as UTC, so the API serializes them with a "Z" and clients
/// don't mistake them for local time. Values are stored unchanged.
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toDb => toDb,                                              // saving: we already store UTC, leave it as is
    fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc))  // reading: same value, now labeled UTC
{
}
