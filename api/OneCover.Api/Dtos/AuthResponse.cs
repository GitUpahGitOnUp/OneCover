namespace OneCover.Api.Dtos;

/// <summary>What login and register return: a signed JWT and the user's role.</summary>
/// <param name="Token">The JWT the client sends back on every request (expires in 2 hours).</param>
/// <param name="Role">"Member" or "Admin". Angular uses it to show the right area.</param>

public record AuthResponse(string Token, string Role);