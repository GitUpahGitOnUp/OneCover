// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     TypeScript types for the data the API sends and receives
// Layer:    shared (used by services + components everywhere)
// Concepts: interface = the SHAPE of an object (compile-time only, no code at runtime). type unions like
//           'Member' | 'Admin' = only those exact strings are allowed, so typos are caught by the compiler.
//           these are the front-end twins of the API's DTOs: keep the property names identical (camelCase)
// Related:  api/OneCover.Api/Dtos/ (the C# originals), AuthService, every feature service
// ─────────────────────────────────────────────────────────────────────────────────────────

// these mirror the API's DTOs (camelCase in JSON). enums arrive as text thanks to JsonStringEnumConverter
export type Role = 'Member' | 'Admin';
export type PlanType = 'STD' | 'LTD' | 'Life' | 'Accident' | 'Dental';

export interface AuthResponse {
  token: string;
  role: Role;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegistrationRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  hireDate: string;            // "yyyy-MM-dd", the format DateOnly expects
  annualSalary: number;
  hoursWorkedLast12Months: number;
  ssnLast4: string;
}

export interface Plan {
  id: number;
  name: string;
  type: PlanType;
  description: string;
  monthlyPremium: number;
  benefitPercent: number;      // a fraction: 0.60 = 60%
  weeklyMaximum: number;
  eliminationDays: number;
  isActive: boolean;
}

export type EnrollmentStatus = 'Active' | 'Cancelled';

// mirrors the API's EnrollmentDto: one row of "My Coverage", w/ the plan's key details flattened in
export interface Enrollment {
  id: number;
  planId: number;
  planName: string;
  planType: PlanType;
  monthlyPremium: number;
  enrolledOn: string;          // "yyyy-MM-dd"
  status: EnrollmentStatus;
}

// mirrors the API's ProfileDto (GET /api/me). never includes the password hash
export interface Profile {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: Role;
  hireDate: string;                // "yyyy-MM-dd"
  annualSalary: number;
  hoursWorkedLast12Months: number;
  ssnLast4: string;                // only the last 4 exist anywhere; maskSsn displays them as ***-**-1234
  isFmlaEligible: boolean;
}

export type ClaimStatus = 'Submitted' | 'InReview' | 'InfoRequested' | 'Approved' | 'Denied' | 'Closed';

// one row of a claim's history timeline (mirrors the API's ClaimHistoryDto)
export interface ClaimHistory {
  fromStatus: ClaimStatus | null;   // null on the first row (the submission)
  toStatus: ClaimStatus;
  changedBy: string;                // a display name, e.g. "Barbara Barbarosa"
  changedAt: string;                // ISO date-time (UTC)
  note: string | null;
}

// mirrors the API's ClaimDto
export interface Claim {
  id: number;
  enrollmentId: number;
  memberName: string;
  planName: string;
  planType: PlanType;
  reason: string;
  startDate: string;                // "yyyy-MM-dd"
  endDate: string;
  status: ClaimStatus;
  weeklyBenefit: number;            // calculated by the API, never sent by the client
  totalBenefit: number;
  submittedAt: string;
  nextStatuses: ClaimStatus[];      // moves the workflow allows from here (used by the admin page)
  history: ClaimHistory[];
}

// what the stepper sends: no amounts, no user id (the API works those out)
export interface SubmitClaimRequest {
  enrollmentId: number;
  reason: string;
  startDate: string;
  endDate: string;
}

export type PaymentType = 'Premium' | 'Benefit';
export type PaymentStatus = 'Pending' | 'Paid' | 'Failed';

// mirrors the API's PaymentDto: one line on a statement
export interface Payment {
  id: number;
  memberName: string;
  type: PaymentType;
  description: string;          // ready to show, e.g. "Short-Term Disability premium"
  amount: number;
  dueDate: string;              // "yyyy-MM-dd"
  paidDate: string | null;      // only set once it's Paid
  status: PaymentStatus;
}

// mirrors the API's generic PagedResult<T>: ONE page of a longer list + what a paginator needs.
// GENERICS: the same shape works for any item type, PagedResult<Claim> today, PagedResult<Payment> tomorrow
export interface PagedResult<T> {
  items: T[];
  page: number;          // 1-based (the API's). Material's paginator counts from 0, so the page converts
  pageSize: number;
  totalCount: number;    // across ALL pages, so the paginator can show "21 – 40 of 57"
}

// what the admin plan dialog sends for create AND update (mirrors SavePlanRequest; no isActive, retire is its own call)
export interface SavePlanRequest {
  name: string;
  type: PlanType;
  description: string | null;
  monthlyPremium: number;
  benefitPercent: number;      // a fraction: 0.60 = 60%
  weeklyMaximum: number;
  eliminationDays: number;
}

// body of PUT /api/admin/claims/{id}/status. whether the move is ALLOWED is decided by the API's workflow
export interface UpdateClaimStatusRequest {
  status: ClaimStatus;
  note: string | null;         // shows up in the claim's history timeline
}

// body of PUT /api/admin/payments/{id}/status. paidOn left null = the API uses today
export interface UpdatePaymentStatusRequest {
  status: PaymentStatus;
  paidOn: string | null;       // "yyyy-MM-dd"
}

// mirrors DashboardDto + StatusCountDto: every status is listed, zeros included
export interface StatusCount {
  status: ClaimStatus;
  count: number;
}

export interface Dashboard {
  totalClaims: number;
  claimsByStatus: StatusCount[];
}
