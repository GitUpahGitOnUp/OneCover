// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     a small colored label for a claim or payment status (Submitted, In Review, Approved, Paid…)
// Layer:    shared (a reusable presentational component: used on My Claims, claim detail, statements, admin pages)
// Concepts: input.required<string>() = a value the PARENT passes in: <app-status-badge [status]="claim.status" />.
//           computed() derives the label + CSS class from it. [class] binding picks the color.
//           dark text on a light background keeps WCAG AA contrast; the text label (not just color) carries the meaning
// Related:  shared/models.ts (ClaimStatus, PaymentStatus), every page that shows a status
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  template: `<span [class]="'badge ' + cssClass()">{{ label() }}</span>`,
  styles: `
    .badge { display: inline-block; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; font-weight: 500; }
    .submitted     { background: #e3f2fd; color: #0d47a1; }
    .inreview      { background: #fff3e0; color: #8a3a00; }
    .inforequested { background: #fce4ec; color: #880e4f; }
    .approved, .paid    { background: #e8f5e9; color: #1b5e20; }
    .denied, .failed    { background: #ffebee; color: #b71c1c; }
    .closed        { background: #eeeeee; color: #424242; }
    .pending       { background: #fff8e1; color: #6d4c00; }
  `,
})
/**
 * A presentational ("dumb") component: it holds no state of its own and calls no services.
 * The parent hands it a status string, and the badge only decides how to DISPLAY it.
 * That single job is why it can be reused on every page that shows a status.
 */
export class StatusBadge {
  // INPUT SIGNAL: the one value the parent passes in, e.g. <app-status-badge [status]="claim.status" />.
  // Reading it means calling it like a function: this.status(). Calling it inside a computed()
  // also tells Angular "recalculate me whenever this input changes".
  // input.required means the compiler errors if a parent forgets it (a badge with no status is meaningless).
  readonly status = input.required<string>();

  // COMPUTED SIGNAL: a read-only value DERIVED from other signals. Nobody sets it; Angular works it out.
  //  - It re-runs only when a signal it read inside (here, status) changes, and the result is cached
  //    between changes. So the regex below doesn't run on every change-detection pass.
  //  - The template reads it with parentheses, {{ label() }}, because a signal is a function.
  //  - `protected` lets the template use it while keeping it off the component's public API.
  //
  // label: the human-friendly text shown to the user.
  //   "InReview" -> "In Review", "InfoRequested" -> "Info Requested", "Approved" -> "Approved".
  //   The regex finds a lowercase letter directly followed by a capital ([a-z])([A-Z]) and rewrites it
  //   as "$1 $2": first letter, a space, second letter. The g flag makes it apply to every match.
  protected readonly label = computed(() => this.status().replace(/([a-z])([A-Z])/g, '$1 $2'));

  // cssClass: the name of the color style to apply. "InReview" -> "inreview", which matches the
  // `.inreview` rule in this component's styles above. The template adds it to the badge via a [class] binding.
  // Keeping label and cssClass as two separate computeds gives each one a single job.
  protected readonly cssClass = computed(() => this.status().toLowerCase());
}
