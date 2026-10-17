// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     INTERFACE for authentication: register + login. both return an AuthResponse (token + role)
// Layer:    Services (business logic), called by AuthController
// Concepts: ABSTRACTION (the controller only knows this contract), async (Task<T> = result arrives later),
//           DTOs in and out (never entities)
// Related:  AuthService (implementation), AuthController (caller), Dtos/ (RegistrationRequest, LoginRequest,
//           AuthResponse), Program.cs (AddScoped<IAuthService, AuthService>)
// ─────────────────────────────────────────────────────────────────────────────────────────
using OneCover.Api.Dtos;

namespace OneCover.Api.Services;

public interface IAuthService
{
    // throws BusinessRuleException if the email is taken
    Task<AuthResponse> RegisterAsync(RegistrationRequest request);

    // throws UnauthorizedException if the email or password is wrong 
    Task<AuthResponse> LoginAsync(LoginRequest request);
}