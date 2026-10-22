// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     TEST HELPER: builds token-shaped strings so the app's real role/expiry code runs in tests
// Layer:    testing (only imported by *.spec.ts files, so it never ends up in the app bundle)
// Concepts: a JWT = header.payload.signature, each part base64url-encoded JSON. the signature here is fake,
//           which is fine: the browser never verifies signatures, only the API does.
//           memberToken/adminToken use the LONG role claim, exactly like the real API's tokens
// Related:  shared/jwt.ts (LONG_ROLE_CLAIM, decodeJwtPayload), core/auth.service.spec.ts, app.spec.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { LONG_ROLE_CLAIM } from '../app/shared/jwt';

/** Any payload as a JWT-shaped string. */
export function fakeJwt(payload: Record<string, unknown>): string {
  const encode = (part: unknown) =>
    btoa(JSON.stringify(part)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.fake-signature`;
}

/** Seconds since 1970, the unit JWT `exp` uses (Date.now() is milliseconds). */
export const nowInSeconds = () => Math.floor(Date.now() / 1000);

/** A token shaped like the real API's: role under the long ClaimTypes.Role URI, expiring in `expiresIn` seconds. */
export function apiToken(role: string, expiresIn = 3600): string {
  return fakeJwt({ [LONG_ROLE_CLAIM]: role, exp: nowInSeconds() + expiresIn });
}
