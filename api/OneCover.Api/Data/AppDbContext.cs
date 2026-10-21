// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the DbContext = EF Core's working session w/ the database. describes the tables + rules, runs
//           queries, tracks changes (change tracker) and saves them. NOT the database itself
// Layer:    Data. only repositories talk to it, services never do
// Concepts: INHERITANCE (: DbContext gets all of EF's querying/tracking/saving for free),
//           POLYMORPHISM (overrides DbContext's virtual methods, EF calls them, our versions run),
//           DEPENDENCY INJECTION (options come in through the constructor), CODE FIRST (migrations read this
//           file to generate the SQL that creates the tables), Fluent API + builder pattern
// Related:  Program.cs (AddDbContext registers it, scoped = one per request), Models/ (the entities),
//           Data/Migrations (generated from this), UserRepository (uses it), DbSeeder (fills it)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using OneCover.Api.Models;

namespace OneCover.Api.Data;

/// <summary>
/// EF Core's Connection to the OneCover DB. One DbSet per table, plus the rules conventions can't infer
/// </summary>
/// <param name="options">Which database and how to connect. Provided by DI from Program.cs.</param>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // one DbSet per table. the property name becomes the table name (Users, Plans...).
    // DbSet<T> = generic type, "a set of T entities". at runtime it's your handle for querying + adding rows
    public DbSet<User> Users => Set<User>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<BenefitClaim> Claims => Set<BenefitClaim>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<ClaimStatusHistory> ClaimStatusHistories => Set<ClaimStatusHistory>();

    public DbSet<Payment> Payments => Set<Payment>();

    // rules by type are written once and applied to every matching property in every entity
    // protected override = replacing DbContext's version of this method w/ ours (polymorphism). DRY: can't forget one
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<decimal>().HavePrecision(18, 2);
        builder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(50);
        // every DateTime comes back labeled UTC -> JSON gets a "Z" -> browsers convert to local time correctly
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    // rules for specific properties and relationships that conventions of EF Core can't infer
    // b = ModelBuilder (builder pattern), each call describes one rule. runs once per app run, then the model is cached
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Plan>().Property(p => p.Name).HasMaxLength(100);
        b.Entity<Plan>().Property(p => p.Description).HasMaxLength(1000);
        b.Entity<ClaimStatusHistory>().Property(csh => csh.Note).HasMaxLength(1000);
        b.Entity<BenefitClaim>().Property(bc => bc.Reason).HasMaxLength(1000);

        // grouped config: user = the builder for User, u = a User object pointing at which property
        b.Entity<User>(user =>
        {
            // unique index = fast lookups by email AND a DB-level rule: no duplicate accounts
            user.HasIndex(u => u.Email).IsUnique();
            user.Property(u => u.Email).HasMaxLength(256);
            user.Property(u => u.SsnLast4).HasMaxLength(4).IsFixedLength();
            user.Property(u => u.FirstName).HasMaxLength(100);
            user.Property(u => u.LastName).HasMaxLength(100);
        });

        // FILTERED UNIQUE INDEX: only ONE *Active* enrollment per member per plan. the service checks this first
        // for a friendly message, but two requests at the same moment (a double-click) could both pass that check.
        // the database guarantees it no matter what. HasFilter = the rule only applies to Active rows, so a member
        // could cancel and re-enroll later. this is "defense in depth", same idea as the unique Email index
        b.Entity<Enrollment>()
            .HasIndex(e => new { e.UserId, e.PlanId })
            .IsUnique()
            .HasFilter("[Status] = 'Active'");

        // CHECK CONSTRAINT: every payment links to EXACTLY ONE of an enrollment (premium) or a claim (benefit).
        // the factories already guarantee it in C#; this makes SQL Server refuse a bad row from anywhere else too.
        // defined here (not just in a .sql file) so the migration carries it and a rebuilt DB always has it
        b.Entity<Payment>().ToTable(t => t.HasCheckConstraint(
            "CK_Payments_ExactlyOneLink",
            "([EnrollmentId] IS NOT NULL AND [ClaimId] IS NULL) OR ([EnrollmentId] IS NULL AND [ClaimId] IS NOT NULL)"));

        // ClaimStatusHistories has a database TRIGGER that blocks UPDATE/DELETE (see sql/ClaimStatusHistory_AppendOnly.sql).
        // telling EF about it makes EF save rows to that table in a trigger-safe way (SQL Server rejects the
        // faster "OUTPUT" style EF normally uses when a table has triggers)
        b.Entity<ClaimStatusHistory>().ToTable(t => t.HasTrigger("TR_ClaimStatusHistories_AppendOnly"));

        // nothing in OneCover is hard-deleted. Plans are retired and claims, history, and payments
        // are immutable records to keep. So refuse deletes instead of cascading. This avoids
        // SQL Server's multiple cascade paths error. 
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}