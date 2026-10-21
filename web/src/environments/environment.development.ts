// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     settings used by `ng serve` (local development)
// Layer:    environments (build-time configuration)
// Concepts: Angular swaps this file in for environment.ts during development builds (fileReplacements in
//           angular.json), so code always imports "environment" and gets the right values for where it runs
// Related:  environment.ts (production values), core/api.ts (reads apiUrl), the API's launchSettings.json
// ─────────────────────────────────────────────────────────────────────────────────────────
export const environment = {
  apiUrl: 'http://localhost:5173/api',   // the API's "http" launch profile
};

