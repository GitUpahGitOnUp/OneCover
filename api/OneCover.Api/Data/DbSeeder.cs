// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     fills an empty dev database w/ demo data: 5 plans + 3 users (admin, FMLA-eligible member, new hire)
// Layer:    Data. runs once at startup, not part of handling requests
// Concepts: static class (no state, never instantiated), async/await (AnyAsync, SaveChangesAsync = DB I/O),
//           DI scopes (CreateScope + GetRequiredService = asking the container manually at startup),
//           object initializers (new Plan { ... }), password hashing (salted, so seeded here, not w/ HasData)
// Related:  Program.cs (calls SeedAsync in Development, or when Seed:DemoData is true, e.g. the Azure demo),
//           AppDbContext, Plan, User, Role/PlanType enums
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using OneCover.Api.Models;
using OneCover.Api.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace OneCover.Api.Data;

// static class; a class you never create objs from, i.e. never write ' new DbSeeder() ' in code.
public static class DbSeeder
{
    // a static class can only have static members, which is why SeedAsync is also static

    private const string DemoPassword = "Demo123!";

    /// <summary>
    /// Adds demo plans and users in Development. Skips any table that already has data,
    /// so it's safe to run on every startup.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    // ^ async means this method contains 'await's, so it can pause while waiting on the db w/o blocking a thread ^
    // ^ Task is what an *async* method returns when it has not result. Task = a promise that this work will finish. ^
    // The caller can *await* it.
    // ^ IServiceProvider is the DI container's "ask for something" interface; "give me an obj of type X" ^
    // IServiceProvider is the *manual* to get things; hold the container and ask it directly. The container otherwise gives
    //  *automatically* via constructors (i.e. AppDbContext gets its options)
    {
        // CreateScope(): AppDbContext is registered as scoped (one per unit of work, usually one per HTTP req.)
        // since there's no request at startup, a scope is manually created, a temp. 'unit of work' bubble

        // using var: when the method ends, auto-dispose of the scope and everything created inside it, including the
        // AppDbContext and its DB connection so that cleanup is not accidentally forgotten.

        //using(top of file): a directive that imports a namespace
        // using(inside a method): a declaration that signifies 'dispose of this when done'
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // asks: does the Plans table have *any* rows? like SQL SELECT CASE WHEN EXISTS (SELECT ! FROM PLANS...)
        // it's fast as it stops at the first row it encounters
        // await: waits for the DB's true/false answer w/o blocking
        // whole conditional makes it safe to run at every startup. It only seeds the DB the first time and is skipped thereafter
        if (!await db.Plans.AnyAsync())
        {
            var std = new Plan
            {
                Name = "Short-Term Disability",
                Type = PlanType.STD,
                Description = "Replaces part of your income while you recover from a short-term illness or injury.",
                MonthlyPremium = 35.00m,
                BenefitPercent = 0.60m,
                WeeklyMaximum = 1500m,
                EliminationDays = 7
            };

            var ltd = new Plan
            {
                Name = "Long-Term Disability",
                Type = PlanType.LTD,
                Description = "Replaces part of your income while you recover from a long-term illness or injury.",
                MonthlyPremium = 25.00m,
                BenefitPercent = 0.60m,
                WeeklyMaximum = 2300m,
                EliminationDays = 90
            };

            // Non-disability plans: BenefitPercent, WeeklyMaximum and EliminationDays stay at their default of 0.
            var life = new Plan
            {
                Name = "Basic Life",
                Type = PlanType.Life,
                Description = "Pays a lump sum to your beneficiaries if you pass away while covered.",
                MonthlyPremium = 10.00m
            };

            var accident = new Plan
            {
                Name = "Accident",
                Type = PlanType.Accident,
                Description = "Pays a set amount toward costs from covered accidental injuries, like fractures or ER visits.",
                MonthlyPremium = 15.00m
            };

            var dental = new Plan
            {
                Name = "Dental",
                Type = PlanType.Dental,
                Description = "Covers preventive cleanings and exams, plus part of the cost of fillings and other dental work.",
                MonthlyPremium = 30.00m
            };

            db.Plans.AddRange(std, ltd, life, accident, dental);

            await db.SaveChangesAsync();
        }

        // safety check, only seed if no users exist yet
        if (!await db.Users.AnyAsync())
        {
            // this one hasher gets reused for all three users
            var hasher = new PasswordHasher<User>();

            // uses today's date, no time, with all hire dates relative to it
            var today = DateOnly.FromDateTime(DateTime.Today);

            var admin = new User
            {
                Email = "admin@onecover.dev",
                Role = Role.Admin,
                FirstName = "Jordan",
                LastName = "Lee",
                HireDate = today.AddYears(-5),
                AnnualSalary = 95000m,
                HoursWorkedLast12Months = 2000,
                SsnLast4 = "1111",
                CreatedAt = DateTime.UtcNow
            };
            // comes *after* the initializer as HashPassword needs the user obj. to already exist.
            admin.PasswordHash = hasher.HashPassword(admin, DemoPassword);

            var member = new User
            {
                Email = "member@onecover.dev",
                Role = Role.Member,
                FirstName = "Barbara",
                LastName = "Barbarosa",
                HireDate = today.AddYears(-3),
                AnnualSalary = 78000m,
                HoursWorkedLast12Months = 1800,
                SsnLast4 = "2222",
                CreatedAt = DateTime.UtcNow
            };
            member.PasswordHash = hasher.HashPassword(member, DemoPassword);

            var newHire = new User
            {
                Email = "newhire@onecover.dev",
                Role = Role.Member,
                FirstName = "Michael",
                LastName = "Corleone",
                HireDate = today.AddMonths(-6),
                AnnualSalary = 62000m,
                HoursWorkedLast12Months = 900,
                SsnLast4 = "3333",
                CreatedAt = DateTime.UtcNow
            };
            newHire.PasswordHash = hasher.HashPassword(newHire, DemoPassword);

            // marks the users for insert
            db.Users.AddRange(admin, member, newHire);
            // sends the users to the DB
            await db.SaveChangesAsync();
        }
    }
}