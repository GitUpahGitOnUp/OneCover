// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     builds the logged-in user's profile: their User row + the FMLA eligibility rule
// Layer:    Services. MeController above, IUserRepository + IEligibilityService below
// Concepts: DEPENDENCY INJECTION + COMPOSITION: this service combines two others (data + a business rule)
//           instead of duplicating either. named arguments when building the DTO
// Related:  IProfileService, IUserRepository, IEligibilityService, ICurrentUser, ProfileDto
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;
using OneCover.Api.Errors;
using OneCover.Api.Repositories;

namespace OneCover.Api.Services;

public class ProfileService(
    IUserRepository users,
    IEligibilityService eligibility,
    ICurrentUser currentUser) : IProfileService
{
    public async Task<ProfileDto> GetMineAsync()
    {
        // a valid token for an account that no longer exists (e.g. after a DB reset) -> 401, so the app logs out
        var user = await users.GetByIdAsync(currentUser.UserId)
            ?? throw new UnauthorizedException("Your account could not be found.");

        return new ProfileDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role,
            HireDate: user.HireDate,
            AnnualSalary: user.AnnualSalary,
            HoursWorkedLast12Months: user.HoursWorkedLast12Months,
            SsnLast4: user.SsnLast4,
            IsFmlaEligible: eligibility.IsFmlaEligible(user.HireDate, user.HoursWorkedLast12Months));
    }
}
