// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     turns any HTTP error into ONE user-friendly sentence, the same way on every page
// Layer:    shared (a pure function)
// Concepts: the API always answers errors w/ ProblemDetails JSON, so the UI can rely on its shape:
//             detail -> our own message ("Invalid email or password.")
//             errors -> field validation messages (400 from [ApiController]); show the first one
//             title  -> the generic status name ("Forbidden")
//           status 0 = the request never reached a server at all (API down, CORS, network)
//           DRY: every page calls this instead of digging through err.error itself
// Related:  the API's GlobalExceptionHandler + UseStatusCodePages (produce ProblemDetails), every page's error handler
// ─────────────────────────────────────────────────────────────────────────────────────────
import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;   // field name -> its messages
}

/** The best message to show for an error, or the fallback if there's nothing useful in it. */
export function errorMessage(err: unknown, fallback: string): string {
  if (!(err instanceof HttpErrorResponse)) {
    return fallback;
  }
  if (err.status === 0) {
    return "Can't reach the server. Is the API running?";
  }

  const problem = err.error as ProblemDetails | null;
  if (problem?.detail) {
    return problem.detail;
  }
  const firstFieldError = problem?.errors ? Object.values(problem.errors).flat()[0] : undefined;
  return firstFieldError ?? problem?.title ?? fallback;
}
