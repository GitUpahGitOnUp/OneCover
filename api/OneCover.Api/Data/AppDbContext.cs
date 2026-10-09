using Microsoft.EntityFrameworkCore;
using OneCover.Api.Models;

namespace OneCover.Api.Data;

/// <summary>
/// EF Core's Connection to the OneCover DB. One DbSet per table, plus the rules conventions can't infer
/// </summary>
/// <param name="options">Which database and how to connect. Provided by DI from Program.cs.</param>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<BenefitClaim> Claims => Set<BenefitClaim>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<ClaimStatusHistory> ClaimStatusHistories => Set<ClaimStatusHistory>();

    public DbSet<Payment> Payments => Set<Payment>();

    // rules by type are written once and applied to every matching property in every entity
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<decimal>().HavePrecision(18, 2);
        builder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(50);
    }

    // rules for specific properties and relationships that conventions of EF Core can't infer
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Plan>().Property(p => p.Name).HasMaxLength(100);
        b.Entity<Plan>().Property(p => p.Description).HasMaxLength(1000);
        b.Entity<ClaimStatusHistory>().Property(csh => csh.Note).HasMaxLength(1000);
        b.Entity<BenefitClaim>().Property(bc => bc.Reason).HasMaxLength(1000);

        b.Entity<User>(user =>
        {
            user.HasIndex(u => u.Email).IsUnique();
            user.Property(u => u.Email).HasMaxLength(256);
            user.Property(u => u.SsnLast4).HasMaxLength(4).IsFixedLength();
            user.Property(u => u.FirstName).HasMaxLength(100);
            user.Property(u => u.LastName).HasMaxLength(100);
        });

        // nothing in OneCover is hard-deleted. Plans are retired and claims, history, and payments
        // are immutable records to keep. So refuse deletes instead of cascading. This avoids
        // SQL Server's multiple cascade paths error. 
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}