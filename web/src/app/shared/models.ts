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
