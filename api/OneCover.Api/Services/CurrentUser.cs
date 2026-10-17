// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the implementation of ICurrentUser. reads the id + role claims out of the JWT on this request
// Layer:    Services
// Concepts: implements an INTERFACE (: ICurrentUser), DEPENDENCY INJECTION (IHttpContextAccessor),
//           ENCAPSULATION (claim names + string->int parsing hidden in here), expression-bodied properties (=>),
//           fail fast (clear error if used w/o [Authorize]), scoped lifetime (one per request = one user per request)
// Related:  ICurrentUser, AuthService.CreateToken (puts these claims IN the token), Program.cs
//           (AddHttpContextAccessor + AddScoped), JWT middleware (builds the ClaimsPrincipal this reads)
// ─────────────────────────────────────────────────────────────────────────────────────────
using System.Security.Claims;

namespace OneCover.Api.Services;

/// <summary>
/// Reads the logged-in user's id and role from the JWT on the current request.
/// Registered as the ICurrentUser implementation in Program.cs.
/// </summary>
// ": ICurrentUser" = this class promises to provide everything the interface lists.
// accessor gets injected by DI and is how we reach the current HTTP request
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    // the logged-in user, built by the authentication middleware from the JWT.
    // it holds "claims" = facts about the user (id, email, role) that AuthService puts in the token
    private ClaimsPrincipal User => accessor.HttpContext!.User;

    // finds the claim holding the user's id. claims are stored as text, so int.Parse turns it into a number
    public int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) 
    ?? throw new InvalidOperationException("No authenticated user. Is the endpoint missing [Authorize]?"));

    // checks the role claim, same check [Authorize(Roles = "Admin")] uses
    public bool IsAdmin => User.IsInRole("Admin");
}