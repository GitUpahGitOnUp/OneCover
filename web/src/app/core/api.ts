// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the single place the API's address lives
// Layer:    core (app-wide)
// Concepts: one constant instead of the URL typed into every service (DRY). the value comes from the ENVIRONMENT
//           file: `ng serve` swaps in environment.development.ts (localhost), a production build uses environment.ts,
//           so the same code runs locally and when deployed. the API's CORS policy must allow this app's origin
// Related:  src/environments/*.ts, every service that calls the API (AuthService, PlanService...),
//           api/.../Program.cs (CORS "web"), angular.json (fileReplacements)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { environment } from '../../environments/environment';   // always import the plain file; Angular swaps it

// the API's base URL comes from the environment file, so the same code runs locally and when deployed
export const API_URL = environment.apiUrl;
