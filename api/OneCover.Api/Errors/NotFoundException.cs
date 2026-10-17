// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     custom exception type for "that record doesn't exist (or isn't yours)" -> 404
// Layer:    Errors. thrown by services, caught by GlobalExceptionHandler
// Concepts: INHERITANCE (: Exception), primary constructor, class w/ no body (;).
//           404 not 403 for someone else's record: 403 would confirm it exists, 404 reveals nothing
// Related:  GlobalExceptionHandler (maps it to 404), BusinessRuleException, UnauthorizedException,
//           services on Day 4 (e.g. a member asking for a claim that isn't theirs)
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Errors;

/// <summary>Thrown when a requested record doesn't exist or doesn't belong to the current user. Returned as 404.</summary>
public class NotFoundException(string message) : Exception(message); // no class body means class dec. ends in ';' vs '{}'. Acceptable since C# v.12

// : Exception(message) inherits from .NET's built-in Exception and passes a message up to it. 

// why make the class type at all? The type now carries implicit meaning, the handler can tell "not found" from
// "a rule was broken" w/o reading the message text.