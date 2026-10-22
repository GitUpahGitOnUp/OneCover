// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     settings for PRODUCTION builds (ng build): points the deployed site at the Azure-hosted API
// Layer:    environments (build-time configuration)
// Concepts: same shape as environment.development.ts, different values. no secrets ever go here: this
//           file ends up inside the JavaScript sent to every browser
// Related:  environment.development.ts, core/api.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
export const environment = {
  // the API on Azure App Service (a different origin than the site, which is why the API's CORS allows the site's URL)
  apiUrl: 'https://onecover-api-ah3645.azurewebsites.net/api',
};

