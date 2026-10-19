// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     SERVICE INTERFACE for the logged-in user's own profile
// Layer:    Services (business logic)
// Concepts: ABSTRACTION. no id parameter: "me" always comes from the JWT
// Related:  ProfileService, MeController, ProfileDto
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;

namespace OneCover.Api.Services;

/// <summary>The logged-in user's profile.</summary>
public interface IProfileService
{
    /// <summary>The logged-in user's profile with FMLA eligibility. Throws UnauthorizedException if the account no longer exists.</summary>
    Task<ProfileDto> GetMineAsync();
}
