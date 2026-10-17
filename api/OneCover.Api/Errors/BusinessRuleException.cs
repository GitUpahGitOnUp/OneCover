// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     custom exception type for "that breaks a rule" -> 400, w/ a message safe to show the user
// Layer:    Errors. thrown by services + entities, caught by GlobalExceptionHandler
// Concepts: INHERITANCE (: Exception), the TYPE carries the meaning so the handler never parses message text
// Related:  GlobalExceptionHandler (maps it to 400), AuthService (duplicate email),
//           BenefitClaim.MoveTo + Payment.MarkPaid (Day 5, invalid moves)
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Errors;

/// <summary>
/// Exception is thrown when a request breaks a business rule like an invalid claim status move. 
/// </summary>
public class BusinessRuleException(string message) : Exception(message);