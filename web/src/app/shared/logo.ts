// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the OneCover mark: a teal shield (coverage = protection) w/ a yellow check (a claim handled)
// Layer:    shared (a presentational component: used in the toolbar and on the login/register pages)
// Concepts: an INLINE SVG: no image file to load, crisp at any size, colors from the brand CSS variables.
//           input() w/ a default (size) so each place can scale it. aria-hidden: the "OneCover" text next to it
//           already names the brand, so screen readers shouldn't announce a meaningless picture too
// Related:  app.html (toolbar brand), features/auth/login.ts + register.ts, public/favicon.svg (same shape),
//           styles.scss (--oc-teal, --oc-yellow)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, input } from '@angular/core';

@Component({
  selector: 'app-logo',
  template: `
    <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <path d="M16 2 L28 6.5 V15 C28 22.6 22.9 28 16 30.5 C9.1 28 4 22.6 4 15 V6.5 Z" fill="var(--oc-teal)" />
      <polyline points="10.5,16 14.5,20 22,12" fill="none" stroke="var(--oc-yellow)" stroke-width="3.2"
                stroke-linecap="round" stroke-linejoin="round" />
    </svg>
  `,
  styles: `:host { display: inline-flex; line-height: 0; }`,
})
export class Logo {
  readonly size = input(28);   // pixels, square
}
