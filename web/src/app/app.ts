// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the ROOT component: the app's shell (toolbar on top, the current page underneath)
// Layer:    app root. every page renders inside its <router-outlet />
// Concepts: standalone component (imports exactly what its template uses), DEPENDENCY INJECTION (AuthService),
//           SIGNALS in the template (auth.isLoggedIn() / auth.role() re-render the nav when you log in or out)
// Related:  app.html (the toolbar markup), app.scss, core/auth.service.ts, app.routes.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  // protected = the template can use it, but other classes can't
  protected readonly auth = inject(AuthService);
}
