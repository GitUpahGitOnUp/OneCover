// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     custom exception type for "we couldn't verify who you are" -> 401
// Layer:    Errors. thrown by AuthService, caught by GlobalExceptionHandler
// Concepts: INHERITANCE (: Exception). OPEN/CLOSED PRINCIPLE: adding this new error type took one class +
//           two lines in the handler, nothing else had to change
// Related:  AuthService.LoginAsync (wrong email or password), GlobalExceptionHandler (maps it to 401),
//           Angular interceptor (Day 6, redirects to login on 401)
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Errors;

/// <summary>
/// Exception thrown when the user cannot be verified e.g. wrong username and / or password, returned as 401
/// </summary>
public class UnauthorizedException(string message) : Exception(message);