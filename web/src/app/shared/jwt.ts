// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     small helpers that READ a JWT on the client: when it expires, and which role it carries
// Layer:    shared (pure functions, no Angular, no HTTP)
// Concepts: a JWT is header.payload.signature, each part base64url-encoded. the payload is just JSON, so the
//           browser can read it (exp, role) w/o the secret key. this does NOT verify the signature: only the API
//           can do that. reading it here is purely for UX (don't show a logged-in screen for an expired token).
//           the TOKEN is the source of truth for the role, so localStorage can't be edited to fake a role
// Related:  core/auth.service.ts (uses all three), the API's AuthService.CreateToken (puts exp + role in the token)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Role } from './models';

// our API writes the role under the long ClaimTypes.Role URI (verified by decoding a real token). some .NET setups
// shorten it to "role", so both are accepted, in case the API's token settings ever change
// exported so tests build tokens w/ the exact claim name the real API uses
export const LONG_ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

/** Decodes a JWT's payload (the middle part). Returns null if the token is malformed. */
export function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const payload = token.split('.')[1];
    if (!payload) {
      return null;
    }
    // base64url -> base64: swap the URL-safe characters back, then pad to a multiple of 4
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(payload.length / 4) * 4, '=');
    return JSON.parse(atob(base64)) as Record<string, unknown>;
  } catch {
    return null;   // not a real JWT: treat it as unusable
  }
}

/** True if the token has no expiry or its expiry (exp, in SECONDS since 1970) has passed. */
export function isTokenExpired(token: string, nowMs: number = Date.now()): boolean {
  const exp = decodeJwtPayload(token)?.['exp'];
  return typeof exp !== 'number' || exp * 1000 <= nowMs;   // * 1000: seconds -> milliseconds
}

/** The role claim inside the token, or null if it's missing or not one of our roles. */
export function roleFromToken(token: string): Role | null {
  const payload = decodeJwtPayload(token);
  const raw = payload?.['role'] ?? payload?.[LONG_ROLE_CLAIM];
  const value = Array.isArray(raw) ? raw[0] : raw;
  return value === 'Member' || value === 'Admin' ? value : null;   // anything else (e.g. 'member') is rejected
}
