// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO (data transfer object) for the auth response: what goes OUT to the client after login/register
// Layer:    Dtos, the API boundary. entities stay inside, DTOs cross it
// Concepts: positional RECORD: one line creates a constructor + 2 read-only properties.
//           returning this instead of the User entity means PasswordHash can never leak
// Related:  AuthService (creates it), AuthController (returns it), Angular AuthService (Day 6, stores token + role)
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Dtos;

/// <summary>What login and register return: a signed JWT and the user's role.</summary>
/// <param name="Token">The JWT the client sends back on every request (expires in 2 hours).</param>
/// <param name="Role">"Member" or "Admin". Angular uses it to show the right area.</param>

public record AuthResponse(string Token, string Role);