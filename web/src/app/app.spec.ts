// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     tests for the root component's shell: logged out (brand, no nav), and the role-based nav links
// Layer:    Tests (Vitest + Angular TestBed)
// Concepts: App injects AuthService, which needs HttpClient + the Router, so the test PROVIDES them (DI: the test
//           decides what's available). provideHttpClientTesting = no real network: a startup request would be
//           caught by http.verify() instead of hitting localhost. the logged-in state comes from a token saved
//           BEFORE the component is created (AuthService restores it on startup, like a page refresh)
// Related:  app.ts, app.html, core/auth.service.ts, testing/fake-jwt.ts
// ─────────────────────────────────────────────────────────────────────────────────────────
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { apiToken } from '../testing/fake-jwt';

describe('App', () => {
  beforeEach(async () => {
    localStorage.clear();   // logged out unless a test saves a token
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());   // the shell must not call the API on its own

  // renders the shell and returns the page + the text of every nav link
  async function render(): Promise<{ page: HTMLElement; links: string[] }> {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;
    const links = Array.from(page.querySelectorAll('nav a'), a => a.textContent?.trim() ?? '');
    return { page, links };
  }

  it('creates the app', () => {
    expect(TestBed.createComponent(App).componentInstance).toBeTruthy();
  });

  it('shows the brand and no navigation when logged out', async () => {
    const { page } = await render();

    expect(page.querySelector('.brand')?.textContent).toContain('OneCover');
    expect(page.querySelector('nav')).toBeNull();   // the nav only renders for a logged-in user
  });

  it('shows member links (and no admin links) to a member', async () => {
    localStorage.setItem('token', apiToken('Member'));

    const { links } = await render();

    expect(links).toEqual(['Plans', 'My Coverage', 'My Claims', 'Statements', 'Profile']);
  });

  it('shows admin links (and no member links) to an admin', async () => {
    localStorage.setItem('token', apiToken('Admin'));

    const { links } = await render();

    expect(links).toEqual(['Dashboard', 'Claims', 'Payments', 'Plans']);
  });
});
