// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     smoke test for the root component: it builds, and shows the brand + no member/admin nav when logged out
// Layer:    Tests (Vitest + Angular TestBed)
// Concepts: replaces the CLI's generated test (which looked for "Hello, web"). App injects AuthService, which needs
//           HttpClient + the Router, so the test PROVIDES them (DI: the test decides what's available)
// Related:  app.ts, app.html, core/auth.service.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    localStorage.clear();   // logged out
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();
  });

  it('creates the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows the brand and no navigation when logged out', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('.brand')?.textContent).toContain('OneCover');
    expect(page.querySelector('nav')).toBeNull();   // the nav only renders for a logged-in user
  });
});
