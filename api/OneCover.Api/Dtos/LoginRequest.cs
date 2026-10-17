// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     DTO for the login request: what comes IN from the client (the JSON body of POST /api/auth/login)
// Layer:    Dtos, the API boundary
// Concepts: RECORD w/ init properties (can't change after arrival), validation attributes
//           ([Required], [EmailAddress]) = "validate at the edges", checked automatically by [ApiController]
// Related:  AuthController.Login (receives it), AuthService.LoginAsync (uses it)
// ─────────────────────────────────────────────────────────────────────────────────────────
using System.ComponentModel.DataAnnotations;

namespace OneCover.Api.Dtos;
public record LoginRequest
// record == a class designed for data that has value-based equality, immutable friendly, and works w/ 'init'. 
// selected here for DTOs as the request shouldn't change after arrival
{
    [Required, EmailAddress] // validation attributes. [ApiController] will check these automatically b4 the code runs
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}