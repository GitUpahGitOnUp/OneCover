// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the admin claims queue: every member's claims, newest first, w/ a status filter + server-side paging
// Layer:    features/admin (a page component)
// Concepts: SERVER-SIDE PAGING: only one page of rows ever leaves the database. mat-paginator just reports which
//           page the admin wants; we fetch it. Material's pageIndex is 0-based, the API's page is 1-based (+1).
//           SIGNALS -> one computed "query" -> toObservable -> switchMap: any change (page, size, filter) sends
//           ONE new request and CANCELS the previous one, so a slow old page can't overwrite the new one.
//           the status filter lives in the URL (?status=Submitted): the dropdown writes the URL, the page reads it
//           (queryParamMap), so dashboard links, refresh and Back always agree w/ what's shown.
//           no column sorting on purpose: sorting one page client-side would look right but be wrong
// Related:  core/admin-claim.service.ts (getPage), shared/models.ts (PagedResult<T>), claim-review.ts (step 3),
//           the API's GET /api/admin/claims + ClaimRepository.GetPageAsync (Skip/Take)
// ─────────────────────────────────────────────────────────────────────────────────────────
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { AdminClaimService } from '../../core/admin-claim.service';
import { Claim, ClaimStatus } from '../../shared/models';
import { StatusBadge } from '../../shared/status-badge';
import { StatusLabelPipe } from '../../shared/status-label';
import { errorMessage } from '../../shared/problem-details';

@Component({
  selector: 'app-claims-queue',
  imports: [
    CurrencyPipe, DatePipe, RouterLink, MatTableModule, MatPaginatorModule, MatFormFieldModule, MatSelectModule,
    MatProgressBarModule, StatusBadge, StatusLabelPipe,
  ],
  template: `
    <h1>Claims queue</h1>
    <p class="hint">Newest first. Pick a claim to review it.</p>

    <mat-form-field>
      <mat-label>Status</mat-label>
      <mat-select [value]="status()" (selectionChange)="onStatusChange($event.value)">
        <mat-option [value]="null">All statuses</mat-option>
        @for (s of statuses; track s) {
          <mat-option [value]="s">{{ s | statusLabel }}</mat-option>
        }
      </mat-select>
    </mat-form-field>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (loading() && claims().length === 0) {
      <p>Loading claims…</p>
    } @else if (claims().length === 0) {
      <p>No claims match this filter.</p>
    } @else {
      <!-- while a new page/filter loads, the OLD rows are dimmed + unclickable under a progress bar,
           so nobody clicks a row that doesn't match the filter they just picked -->
      @if (loading()) {
        <mat-progress-bar mode="indeterminate" aria-label="Loading claims" />
      }
      <table mat-table [dataSource]="claims()" [class.stale]="loading()" [attr.aria-busy]="loading()">
        <ng-container matColumnDef="id">
          <th mat-header-cell *matHeaderCellDef>Claim</th>
          <td mat-cell *matCellDef="let c"><a [routerLink]="['/admin/claims', c.id]">#{{ c.id }}</a></td>
        </ng-container>
        <ng-container matColumnDef="member">
          <th mat-header-cell *matHeaderCellDef>Member</th>
          <td mat-cell *matCellDef="let c">{{ c.memberName }}</td>
        </ng-container>
        <ng-container matColumnDef="plan">
          <th mat-header-cell *matHeaderCellDef>Coverage</th>
          <td mat-cell *matCellDef="let c">{{ c.planName }}</td>
        </ng-container>
        <ng-container matColumnDef="submitted">
          <th mat-header-cell *matHeaderCellDef>Submitted</th>
          <!-- a real timestamp (UTC w/ "Z"), so no 'UTC' here: shown in the admin's local time -->
          <td mat-cell *matCellDef="let c">{{ c.submittedAt | date: 'mediumDate' }}</td>
        </ng-container>
        <ng-container matColumnDef="total">
          <th mat-header-cell *matHeaderCellDef>Total benefit</th>
          <td mat-cell *matCellDef="let c">{{ c.totalBenefit | currency }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let c"><app-status-badge [status]="c.status" /></td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns"></tr>
      </table>

      <!-- the paginator holds no data: it shows "21 – 40 of 57" from totalCount and emits (page) on clicks -->
      <mat-paginator
        [length]="totalCount()"
        [pageIndex]="pageIndex()"
        [pageSize]="pageSize()"
        [pageSizeOptions]="[10, 20, 50]"
        (page)="onPage($event)"
        aria-label="Claims queue pages"
      />
    }
  `,
  // .hint is global (styles.scss)
  styles: `.stale { opacity: 0.5; pointer-events: none; }`,
})
export class ClaimsQueuePage {
  private adminClaims = inject(AdminClaimService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  protected readonly columns = ['id', 'member', 'plan', 'submitted', 'total', 'status'];
  protected readonly statuses: ClaimStatus[] = ['Submitted', 'InReview', 'InfoRequested', 'Approved', 'Denied', 'Closed'];

  // what the admin asked for (the INPUTS)
  protected readonly pageIndex = signal(0);                       // 0-based, like mat-paginator
  protected readonly pageSize = signal(20);
  // the filter MIRRORS the URL's ?status= (set in the constructor below), so the URL is the single source of truth
  protected readonly status = signal<ClaimStatus | null>(null);

  // what came back (the RESULTS)
  protected readonly claims = signal<Claim[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  // the 3 inputs as ONE value. when any of them changes, this recomputes -> one new request
  private readonly query = computed(() => ({
    page: this.pageIndex() + 1,   // 0-based (Material) -> 1-based (API)
    pageSize: this.pageSize(),
    status: this.status(),
  }));

  constructor() {
    // URL -> filter. queryParamMap is an OBSERVABLE: it emits now AND whenever ?status= changes while this page is
    // reused (e.g. the "Claims" nav link from /admin/claims?status=Submitted). only a REAL status is accepted:
    // ?status=junk falls back to "all" instead of sending junk to the API (validate input at the edge)
    this.route.queryParamMap
      .pipe(
        map(params => this.statuses.find(s => s === params.get('status')) ?? null),
        takeUntilDestroyed(),
      )
      .subscribe(status => {
        this.status.set(status);
        this.pageIndex.set(0);   // a new filter starts at page 1 (page 3 of "Denied" may not exist)
      });

    // toObservable: signal -> observable, so we can use RxJS's switchMap on it
    toObservable(this.query)
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        // a new query cancels the request still in flight for the old one
        switchMap(q =>
          this.adminClaims.getPage(q.page, q.pageSize, q.status).pipe(
            catchError((err: unknown) => {
              this.error.set(errorMessage(err, 'Could not load the claims queue.'));
              return of(null);   // keep the stream alive so the next filter/page change still works
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(result => {
        if (result) {
          this.claims.set(result.items);
          this.totalCount.set(result.totalCount);
        }
        this.loading.set(false);
      });
  }

  // dropdown -> URL (not straight to the signal): the queryParamMap subscription above then updates the filter,
  // so refresh, back/forward and shared links always show what the URL says. null removes ?status= entirely.
  // replaceUrl: changing a filter doesn't pile up entries in the browser's Back history
  protected onStatusChange(status: ClaimStatus | null) {
    this.router.navigate([], { relativeTo: this.route, queryParams: { status }, replaceUrl: true });
  }

  protected onPage(event: PageEvent) {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }
}
