// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the app's startup configuration: registers the app-wide services ("providers")
// Layer:    app root. the Angular equivalent of the services half of the API's Program.cs
// Concepts: DEPENDENCY INJECTION registration: each provideX(...) tells Angular's DI container what's available.
//           withInterceptors([...]) plugs authInterceptor into EVERY HttpClient request (like adding middleware)
// Related:  app.routes.ts (the routes), core/auth.interceptor.ts, main.ts (boots the app w/ this config)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),                   // logs uncaught browser errors
    provideRouter(routes),                                  // turns on routing w/ the routes in app.routes.ts
    provideHttpClient(withInterceptors([authInterceptor])), // HttpClient for API calls + the token-adding interceptor
  ]
};
