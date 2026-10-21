// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     settings for PRODUCTION builds (ng build). filled in for real at deploy time (e.g. Azure)
// Layer:    environments (build-time configuration)
// Concepts: same shape as environment.development.ts, different values. no secrets ever go here: this
//           file ends up inside the JavaScript sent to every browser
// Related:  environment.development.ts, core/api.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
export const environment = {
  apiUrl: '/api',   // placeholder: replace w/ the deployed API's URL when deploying
};

