using OneCover.Api.Dtos;

namespace OneCover.Api.Services;

public interface IAuthService
{
    // throws BusinessRuleException if the email is taken
    Task<AuthResponse> RegisterAsync(RegistrationRequest request);

    // throws UnauthorizedException if the email or password is wrong 
    Task<AuthResponse> LoginAsync(LoginRequest request);
}