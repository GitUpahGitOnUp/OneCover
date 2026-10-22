# OneCover Code Map

A lookup table for "where is X, and what is it?" Every row says which **side** it's on (API or Web), which **layer**, the **file and line**, and what it connects to. Links are clickable in VS Code (Ctrl+Click).

> **Line numbers are a snapshot (Oct 22, 2026).** If a link lands a few lines off, search the *symbol name* in the "Piece" column (Ctrl+Shift+F, or Ctrl+T for symbols). Every piece is named so it's searchable even when the numbers drift.

**Contents**
1. [Legend and how the layers connect](#1-legend-and-how-the-layers-connect)
2. [One request end to end: filing a claim](#2-one-request-end-to-end-filing-a-claim)
3. [Endpoint table (URL → every layer)](#3-endpoint-table-url--every-layer)
4. [API by feature](#4-api-by-feature)
5. [Web by feature](#5-web-by-feature)
6. ["Where is the...?" concept index](#6-where-is-the-concept-index)
7. [Tests, config, SQL, tooling](#7-tests-config-sql-tooling)
8. [Ideas to make this map better](#8-ideas-to-make-this-map-better)

---

## 1. Legend and how the layers connect

| Tag | Meaning |
|---|---|
| **API** | `api/OneCover.Api` (ASP.NET Core, .NET 10) |
| **WEB** | `web/src/app` (Angular + Material) |
| **TEST** | `api/OneCover.Tests` (xUnit) or `*.spec.ts` (Vitest) |

**API layers (top to bottom):**

```
HTTP request
  → Middleware pipeline        Program.cs (auth, CORS, exception handler)
  → Controller                 thin: HTTP only, no logic
  → Service                    business logic + ownership checks, uses ICurrentUser
  → Repository                 EF Core queries only (the only code that touches AppDbContext)
  → Entity (Models/)           holds its own rules (BenefitClaim.Submit / MoveTo, Payment.MarkPaid)
  → AppDbContext → SQL Server
  ← Mapping (Dtos/*Mappings)   entity → DTO on the way out. Entities never leave the API
```

**WEB layers:**

```
URL → app.routes.ts (guard) → Page component (features/…)
        → core/*.service.ts (HttpClient, returns Observables)
        → auth.interceptor.ts adds the Bearer token
        → API
Shared helpers: shared/ (models, validators, pipes, badges)
```

**Mirror image:** `Program.cs` ⇄ `app.config.ts` (startup/DI registration) · middleware ⇄ `auth.interceptor.ts` · `[Authorize(Roles=…)]` ⇄ `roleGuard` · DTOs ⇄ `shared/models.ts` · controller routes ⇄ `core/*.service.ts` URLs.

---

## 2. One request end to end: filing a claim

`POST /api/claims` as a Member. Follow the numbers.

| # | Side | Step | Where |
|---|---|---|---|
| 1 | WEB | Page builds two reactive forms; `submit()` calls the service | [file-claim.ts:173](../web/src/app/features/member/file-claim.ts#L173) (forms), [:222](../web/src/app/features/member/file-claim.ts#L222) (`submit`) |
| 2 | WEB | Client-side date rules (UX only; the API re-checks) | [validators.ts:15](../web/src/app/shared/validators.ts#L15) `endAfterStart`, [:22](../web/src/app/shared/validators.ts#L22) `withinOneYear`, [:38](../web/src/app/shared/validators.ts#L38) `startOnOrAfter` |
| 3 | WEB | `ClaimService.submit()` → `http.post` | [claim.service.ts:29](../web/src/app/core/claim.service.ts#L29) |
| 4 | WEB | Interceptor clones the request and adds `Authorization: Bearer <jwt>` | [auth.interceptor.ts:18](../web/src/app/core/auth.interceptor.ts#L18) |
| 5 | API | Middleware: CORS → Authentication (validates JWT) → Authorization | [Program.cs:129-132](../api/OneCover.Api/Program.cs#L129) |
| 6 | API | Route matched; `[Authorize(Roles="Member")]` checked; `[ApiController]` validates the DTO (400 if bad) | [ClaimsController.cs:32-34](../api/OneCover.Api/Controllers/ClaimsController.cs#L32), DTO: [SubmitClaimRequest.cs:16](../api/OneCover.Api/Dtos/SubmitClaimRequest.cs#L16) |
| 7 | API | Controller calls the service through the interface (DI) | [ClaimsController.cs:18](../api/OneCover.Api/Controllers/ClaimsController.cs#L18) (constructor), registered at [Program.cs:67](../api/OneCover.Api/Program.cs#L67) |
| 8 | API | Service: ownership check (`currentUser.UserId` from the JWT), active check, disability-plan check | [ClaimService.cs:33-55](../api/OneCover.Api/Services/ClaimService.cs#L33) |
| 9 | API | `ICurrentUser` reads the user id out of the JWT claims | [CurrentUser.cs:27](../api/OneCover.Api/Services/CurrentUser.cs#L27) |
| 10 | API | Benefit math (weekly, then total after elimination period) | [ClaimService.cs:67-68](../api/OneCover.Api/Services/ClaimService.cs#L67) → [BenefitCalculator.cs:16](../api/OneCover.Api/Services/BenefitCalculator.cs#L16), [:25](../api/OneCover.Api/Services/BenefitCalculator.cs#L25) |
| 11 | API | `BenefitClaim.Submit` (static factory): date rules + first history row | [BenefitClaim.cs:65](../api/OneCover.Api/Models/BenefitClaim.cs#L65) |
| 12 | API | Repo `Add` + `SaveChangesAsync` (INSERT claim, then history, one transaction) | [ClaimService.cs:80-81](../api/OneCover.Api/Services/ClaimService.cs#L80), [ClaimRepository.cs:72-74](../api/OneCover.Api/Repositories/ClaimRepository.cs#L72) |
| 13 | API | Reload with Includes so the DTO has names | [ClaimService.cs:84](../api/OneCover.Api/Services/ClaimService.cs#L84), [ClaimRepository.cs:23](../api/OneCover.Api/Repositories/ClaimRepository.cs#L23) `ClaimsWithDetails` |
| 14 | API | Entity → DTO | [ClaimMappings.cs:16](../api/OneCover.Api/Dtos/ClaimMappings.cs#L16) |
| 15 | API | `201 Created` + `Location` header | [ClaimsController.cs:34-38](../api/OneCover.Api/Controllers/ClaimsController.cs#L34) |
| 16 | API (error path) | A thrown `BusinessRuleException` becomes a 400 ProblemDetails | [GlobalExceptionHandler.cs:36](../api/OneCover.Api/Errors/GlobalExceptionHandler.cs#L36) |
| 17 | WEB | Success panel, or snackbar with the API's message | [file-claim.ts:231-240](../web/src/app/features/member/file-claim.ts#L231), message extractor [problem-details.ts:21](../web/src/app/shared/problem-details.ts#L21) |

---

## 3. Endpoint table (URL → every layer)

Auth column: **Public** = no login · **Any** = any logged-in user · **Member** / **Admin** = role required.

| Verb + URL | Auth | Controller action | Service method | Repository method(s) | Request → Response DTO | Angular caller | Page that uses it |
|---|---|---|---|---|---|---|---|
| `POST /api/auth/register` | Public | [AuthController.cs:34](../api/OneCover.Api/Controllers/AuthController.cs#L34) | [AuthService.cs:47](../api/OneCover.Api/Services/AuthService.cs#L47) | [UserRepository.cs:26](../api/OneCover.Api/Repositories/UserRepository.cs#L26) `EmailExistsAsync`, `:32` `Add`, `:35` Save | [RegistrationRequest](../api/OneCover.Api/Dtos/RegistrationRequest.cs#L16) → [AuthResponse](../api/OneCover.Api/Dtos/AuthResponse.cs#L14) | [auth.service.ts:49](../web/src/app/core/auth.service.ts#L49) | [register.ts](../web/src/app/features/auth/register.ts#L91) |
| `POST /api/auth/login` | Public | [AuthController.cs:29](../api/OneCover.Api/Controllers/AuthController.cs#L29) | [AuthService.cs:84](../api/OneCover.Api/Services/AuthService.cs#L84) | [UserRepository.cs:23](../api/OneCover.Api/Repositories/UserRepository.cs#L23) `GetByEmailAsync` | [LoginRequest](../api/OneCover.Api/Dtos/LoginRequest.cs#L11) → AuthResponse | [auth.service.ts:43](../web/src/app/core/auth.service.ts#L43) | [login.ts](../web/src/app/features/auth/login.ts#L53) |
| `GET /api/plans` | Public | [PlansController.cs:22](../api/OneCover.Api/Controllers/PlansController.cs#L22) | [PlanService.cs:23](../api/OneCover.Api/Services/PlanService.cs#L23) | [PlanRepository.cs:28](../api/OneCover.Api/Repositories/PlanRepository.cs#L28) `GetActiveAsync` | → [PlanDto](../api/OneCover.Api/Dtos/PlanDto.cs#L19) | [plan.service.ts:19](../web/src/app/core/plan.service.ts#L19) | [plans.ts](../web/src/app/features/member/plans.ts#L75), [plans-admin.ts](../web/src/app/features/admin/plans-admin.ts#L78) |
| `GET /api/plans/{id}` | Public | [PlansController.cs:29](../api/OneCover.Api/Controllers/PlansController.cs#L29) | [PlanService.cs:30](../api/OneCover.Api/Services/PlanService.cs#L30) | [PlanRepository.cs:38](../api/OneCover.Api/Repositories/PlanRepository.cs#L38) `GetByIdAsync` | → PlanDto | *(no Angular caller)* | Postman only |
| `POST /api/plans` | Admin | [PlansController.cs:36](../api/OneCover.Api/Controllers/PlansController.cs#L36) | [PlanService.cs:33](../api/OneCover.Api/Services/PlanService.cs#L33) | `Add` [:41](../api/OneCover.Api/Repositories/PlanRepository.cs#L41), Save `:44` | [SavePlanRequest](../api/OneCover.Api/Dtos/SavePlanRequest.cs#L21) → PlanDto | [plan.service.ts:24](../web/src/app/core/plan.service.ts#L24) | [plan-dialog.ts](../web/src/app/features/admin/plan-dialog.ts#L100) |
| `PUT /api/plans/{id}` | Admin | [PlansController.cs:45](../api/OneCover.Api/Controllers/PlansController.cs#L45) | [PlanService.cs:46](../api/OneCover.Api/Services/PlanService.cs#L46) | `GetByIdAsync` (tracked) + Save | SavePlanRequest → PlanDto | [plan.service.ts:29](../web/src/app/core/plan.service.ts#L29) | plan-dialog.ts |
| `DELETE /api/plans/{id}` | Admin | [PlansController.cs:52](../api/OneCover.Api/Controllers/PlansController.cs#L52) | [PlanService.cs:65](../api/OneCover.Api/Services/PlanService.cs#L65) `RetireAsync` (soft delete) | GetById + Save | → 204 | [plan.service.ts:34](../web/src/app/core/plan.service.ts#L34) | [plans-admin.ts:126](../web/src/app/features/admin/plans-admin.ts#L126) |
| `GET /api/enrollments` | Member | [EnrollmentsController.cs:21](../api/OneCover.Api/Controllers/EnrollmentsController.cs#L21) | [EnrollmentService.cs:68](../api/OneCover.Api/Services/EnrollmentService.cs#L68) | [EnrollmentRepository.cs:22](../api/OneCover.Api/Repositories/EnrollmentRepository.cs#L22) | → [EnrollmentDto](../api/OneCover.Api/Dtos/EnrollmentDto.cs#L14) | [enrollment.service.ts:18](../web/src/app/core/enrollment.service.ts#L18) | [coverage.ts](../web/src/app/features/member/coverage.ts#L62), [plans.ts](../web/src/app/features/member/plans.ts#L75), [file-claim.ts](../web/src/app/features/member/file-claim.ts#L160) |
| `POST /api/enrollments` | Member | [EnrollmentsController.cs:27](../api/OneCover.Api/Controllers/EnrollmentsController.cs#L27) | [EnrollmentService.cs:25](../api/OneCover.Api/Services/EnrollmentService.cs#L25) (also creates the first premium Payment) | [EnrollmentRepository.cs:38](../api/OneCover.Api/Repositories/EnrollmentRepository.cs#L38) `HasActiveEnrollmentAsync`, `:41` Add, `:47` Save (catches duplicate-key) | [EnrollRequest](../api/OneCover.Api/Dtos/EnrollRequest.cs#L13) → EnrollmentDto | [enrollment.service.ts:23](../web/src/app/core/enrollment.service.ts#L23) | [plans.ts:109](../web/src/app/features/member/plans.ts#L109) |
| `GET /api/claims` | Member | [ClaimsController.cs:21](../api/OneCover.Api/Controllers/ClaimsController.cs#L21) | [ClaimService.cs:89](../api/OneCover.Api/Services/ClaimService.cs#L89) | [ClaimRepository.cs:28](../api/OneCover.Api/Repositories/ClaimRepository.cs#L28) | → [ClaimDto](../api/OneCover.Api/Dtos/ClaimDto.cs#L14) | [claim.service.ts:19](../web/src/app/core/claim.service.ts#L19) | [claims.ts](../web/src/app/features/member/claims.ts#L65) |
| `GET /api/claims/{id}` | Any (member = own only) | [ClaimsController.cs:27](../api/OneCover.Api/Controllers/ClaimsController.cs#L27) | [ClaimService.cs:95](../api/OneCover.Api/Services/ClaimService.cs#L95) | [ClaimRepository.cs:36](../api/OneCover.Api/Repositories/ClaimRepository.cs#L36) | → ClaimDto | [claim.service.ts:24](../web/src/app/core/claim.service.ts#L24) | [claim-detail.ts](../web/src/app/features/member/claim-detail.ts#L38), [claim-review.ts](../web/src/app/features/admin/claim-review.ts#L80) |
| `POST /api/claims` | Member | [ClaimsController.cs:32](../api/OneCover.Api/Controllers/ClaimsController.cs#L32) | [ClaimService.cs:33](../api/OneCover.Api/Services/ClaimService.cs#L33) | `Add` `:72`, Save `:74` | [SubmitClaimRequest](../api/OneCover.Api/Dtos/SubmitClaimRequest.cs#L16) → ClaimDto (201) | [claim.service.ts:29](../web/src/app/core/claim.service.ts#L29) | [file-claim.ts:222](../web/src/app/features/member/file-claim.ts#L222) |
| `GET /api/payments` | Member | [PaymentsController.cs:20](../api/OneCover.Api/Controllers/PaymentsController.cs#L20) | [PaymentService.cs:21](../api/OneCover.Api/Services/PaymentService.cs#L21) | [PaymentRepository.cs:27](../api/OneCover.Api/Repositories/PaymentRepository.cs#L27) | → [PaymentDto](../api/OneCover.Api/Dtos/PaymentDto.cs#L14) | [payment.service.ts:18](../web/src/app/core/payment.service.ts#L18) | [statements.ts](../web/src/app/features/member/statements.ts#L61) |
| `GET /api/me` | Any | [MeController.cs:21](../api/OneCover.Api/Controllers/MeController.cs#L21) | [ProfileService.cs:19](../api/OneCover.Api/Services/ProfileService.cs#L19) (+ FMLA check at `:35`) | [UserRepository.cs:29](../api/OneCover.Api/Repositories/UserRepository.cs#L29) | → [ProfileDto](../api/OneCover.Api/Dtos/ProfileDto.cs#L13) | [profile.service.ts:17](../web/src/app/core/profile.service.ts#L17) | [profile.ts](../web/src/app/features/member/profile.ts#L64) |
| `GET /api/admin/claims?status&page&pageSize` | Admin | [AdminClaimsController.cs:24](../api/OneCover.Api/Controllers/AdminClaimsController.cs#L24) | [ClaimService.cs:111](../api/OneCover.Api/Services/ClaimService.cs#L111) `GetPageAsync` | [ClaimRepository.cs:44](../api/OneCover.Api/Repositories/ClaimRepository.cs#L44) (Skip/Take, returns a tuple) | → [PagedResult\<ClaimDto\>](../api/OneCover.Api/Dtos/PagedResult.cs#L14) | [admin-claim.service.ts:21](../web/src/app/core/admin-claim.service.ts#L21) | [claims-queue.ts](../web/src/app/features/admin/claims-queue.ts#L107) |
| `PUT /api/admin/claims/{id}/status` | Admin | [AdminClaimsController.cs:33](../api/OneCover.Api/Controllers/AdminClaimsController.cs#L33) | [ClaimService.cs:127](../api/OneCover.Api/Services/ClaimService.cs#L127) → `claim.MoveTo`, maybe `Payment.PendingBenefit` | ClaimRepo GetById + `payments.Add` + Save | [UpdateClaimStatusRequest](../api/OneCover.Api/Dtos/UpdateClaimStatusRequest.cs#L14) → ClaimDto | [admin-claim.service.ts:32](../web/src/app/core/admin-claim.service.ts#L32) | [claim-review.ts:132](../web/src/app/features/admin/claim-review.ts#L132) |
| `GET /api/admin/dashboard` | Admin | [AdminDashboardController.cs:20](../api/OneCover.Api/Controllers/AdminDashboardController.cs#L20) | [ClaimService.cs:152](../api/OneCover.Api/Services/ClaimService.cs#L152) | [ClaimRepository.cs:67](../api/OneCover.Api/Repositories/ClaimRepository.cs#L67) `CountByStatusAsync` (GroupBy) | → [DashboardDto](../api/OneCover.Api/Dtos/DashboardDto.cs#L14) | [admin-claim.service.ts:37](../web/src/app/core/admin-claim.service.ts#L37) | [dashboard.ts](../web/src/app/features/admin/dashboard.ts#L74) |
| `GET /api/admin/payments?status` | Admin | [AdminPaymentsController.cs:21](../api/OneCover.Api/Controllers/AdminPaymentsController.cs#L21) | [PaymentService.cs:27](../api/OneCover.Api/Services/PaymentService.cs#L27) | [PaymentRepository.cs:33](../api/OneCover.Api/Repositories/PaymentRepository.cs#L33) | → PaymentDto list | [admin-payment.service.ts:20](../web/src/app/core/admin-payment.service.ts#L20) | [payments.ts](../web/src/app/features/admin/payments.ts#L106) |
| `PUT /api/admin/payments/{id}/status` | Admin | [AdminPaymentsController.cs:27](../api/OneCover.Api/Controllers/AdminPaymentsController.cs#L27) | [PaymentService.cs:33](../api/OneCover.Api/Services/PaymentService.cs#L33) → `payment.MarkPaid` / `MarkFailed` | `GetByIdAsync` `:45`, Save `:47` | [UpdatePaymentStatusRequest](../api/OneCover.Api/Dtos/UpdatePaymentStatusRequest.cs#L14) → PaymentDto | [admin-payment.service.ts:26](../web/src/app/core/admin-payment.service.ts#L26) | [payments.ts:148](../web/src/app/features/admin/payments.ts#L148) |

---

## 4. API by feature

Base path for all links: `api/OneCover.Api/`.

### 4.1 Startup, DI, middleware (the composition root)

| Piece | File : line | What it does / related |
|---|---|---|
| Builder + controllers + enums-as-text JSON | [Program.cs:23-29](../api/OneCover.Api/Program.cs#L23) | `JsonStringEnumConverter` so Angular receives `"InReview"`, not `1` |
| `AddDbContext<AppDbContext>` (Scoped) | [Program.cs:34](../api/OneCover.Api/Program.cs#L34) | Connection string `ConnectionStrings:Default` → SQL Server |
| `TimeProvider` (Singleton) | [Program.cs:38](../api/OneCover.Api/Program.cs#L38) | Injected clock; tests swap in a fake. Used by Auth/Claim/Payment/Enrollment/Eligibility services |
| ProblemDetails + `GlobalExceptionHandler` | [Program.cs:41-43](../api/OneCover.Api/Program.cs#L41) | Pair with `app.UseExceptionHandler()` at [:108](../api/OneCover.Api/Program.cs#L108) |
| `IHttpContextAccessor` | [Program.cs:46](../api/OneCover.Api/Program.cs#L46) | Lets `CurrentUser` see the request |
| `ICurrentUser → CurrentUser` (Scoped) | [Program.cs:50](../api/OneCover.Api/Program.cs#L50) | |
| Repositories (Scoped): User `:54`, Plan `:57`, Enrollment `:58`, Payment `:59`, Claim `:60` | [Program.cs:54-60](../api/OneCover.Api/Program.cs#L54) | Scoped because they use the scoped DbContext |
| Services (Scoped): Auth `:63`, Plan `:65`, Enrollment `:66`, Claim `:67`, Payment `:68`, Profile `:69` | [Program.cs:63-69](../api/OneCover.Api/Program.cs#L63) | |
| Singletons: `IBenefitCalculator`, `IEligibilityService` | [Program.cs:73-74](../api/OneCover.Api/Program.cs#L73) | Pure rules, no per-request state |
| JWT key fail-fast | [Program.cs:77](../api/OneCover.Api/Program.cs#L77) | `Jwt:Key` from user-secrets / Azure setting, never in source |
| JWT validation parameters | [Program.cs:81-91](../api/OneCover.Api/Program.cs#L81) | Issuer, audience, lifetime, signing key |
| `AddAuthorization` | [Program.cs:94](../api/OneCover.Api/Program.cs#L94) | |
| CORS policy `"web"` | [Program.cs:99-101](../api/OneCover.Api/Program.cs#L99) | Origins from config `Cors:Origins` ([appsettings.json:13](../api/OneCover.Api/appsettings.json#L13)) |
| `builder.Build()` | [Program.cs:104](../api/OneCover.Api/Program.cs#L104) | Everything above = services, everything below = pipeline |
| Pipeline: ExceptionHandler `:108` → StatusCodePages `:111` → HttpsRedirection `:128` → CORS `:129` → Authentication `:131` → Authorization `:132` → MapControllers `:134` | [Program.cs:108-134](../api/OneCover.Api/Program.cs#L108) | Order matters |
| Seed demo data | [Program.cs:115-118](../api/OneCover.Api/Program.cs#L115) → [DbSeeder.cs:28](../api/OneCover.Api/Data/DbSeeder.cs#L28) | Dev always; prod only if `Seed:DemoData` |
| OpenAPI + Swagger UI (dev only) | [Program.cs:121-125](../api/OneCover.Api/Program.cs#L121) | |

### 4.2 Auth and identity (JWT)

| Piece | Layer | File : line | Related |
|---|---|---|---|
| `AuthController` (`api/auth`) | Controller | [AuthController.cs:22](../api/OneCover.Api/Controllers/AuthController.cs#L22) | login `:29`, register `:34`. No `[Authorize]` (public) |
| `IAuthService` | Interface | [IAuthService.cs:13](../api/OneCover.Api/Services/IAuthService.cs#L13) | |
| `AuthService` | Service | [AuthService.cs:34](../api/OneCover.Api/Services/AuthService.cs#L34) | `RegisterAsync` `:47`, `LoginAsync` `:84`, `CreateToken` `:107` |
| Password hashing (`PasswordHasher<User>`) | Service | [AuthService.cs:38](../api/OneCover.Api/Services/AuthService.cs#L38) hash `:73`, verify `:93` | Salted, one-way |
| Role forced to Member on register | Service | [AuthService.cs:62](../api/OneCover.Api/Services/AuthService.cs#L62) | Role never comes from the request body |
| Same error for bad email / bad password | Service | [AuthService.cs:93-96](../api/OneCover.Api/Services/AuthService.cs#L93) | Doesn't leak which emails exist |
| JWT claims (id, email, role) | Service | [AuthService.cs:111-116](../api/OneCover.Api/Services/AuthService.cs#L111) | 2-hour expiry at `:133` |
| `ICurrentUser` / `CurrentUser` | Service | [ICurrentUser.cs:17](../api/OneCover.Api/Services/ICurrentUser.cs#L17) / [CurrentUser.cs:20](../api/OneCover.Api/Services/CurrentUser.cs#L20) | `UserId` `:27`, `IsAdmin` `:31`. **This is how "members only get their own data" works** |
| `IUserRepository` / `UserRepository` | Repo | [IUserRepository.cs:23](../api/OneCover.Api/Repositories/IUserRepository.cs#L23) / [UserRepository.cs:19](../api/OneCover.Api/Repositories/UserRepository.cs#L19) | |
| `User` entity (`SsnLast4`, `PasswordHash`, `HireDate`, salary) | Entity | [User.cs:17](../api/OneCover.Api/Models/User.cs#L17) | Only last 4 of SSN stored |
| `Role` enum | Enum | [Role.cs](../api/OneCover.Api/Models/Enums/Role.cs) | `Member`, `Admin` |
| Request/response DTOs | DTO | [LoginRequest.cs:11](../api/OneCover.Api/Dtos/LoginRequest.cs#L11), [RegistrationRequest.cs:16](../api/OneCover.Api/Dtos/RegistrationRequest.cs#L16), [AuthResponse.cs:14](../api/OneCover.Api/Dtos/AuthResponse.cs#L14) | Validation attributes live here |
| **Web side of auth** | WEB | see [5.1](#51-core-shell-auth-routing) | `auth.service.ts`, `auth.interceptor.ts`, `role.guard.ts`, `jwt.ts` |

### 4.3 Plans (admin CRUD, member browse)

| Piece | Layer | File : line | Related |
|---|---|---|---|
| `PlansController` (`api/plans`) | Controller | [PlansController.cs:19](../api/OneCover.Api/Controllers/PlansController.cs#L19) | GET list/id public; POST/PUT/DELETE `[Authorize(Roles="Admin")]` |
| `IPlanService` / `PlanService` | Interface / Service | [IPlanService.cs:15](../api/OneCover.Api/Services/IPlanService.cs#L15) / [PlanService.cs:19](../api/OneCover.Api/Services/PlanService.cs#L19) | `FindOrThrowAsync` helper `:75` (404) |
| Soft delete = `IsActive = false` | Service | [PlanService.cs:65-71](../api/OneCover.Api/Services/PlanService.cs#L65) | Row stays because enrollments point to it |
| `IPlanRepository` / `PlanRepository` | Repo | [IPlanRepository.cs:18](../api/OneCover.Api/Repositories/IPlanRepository.cs#L18) / [PlanRepository.cs:18](../api/OneCover.Api/Repositories/PlanRepository.cs#L18) | `GetActiveAsync` is `AsNoTracking`; `GetByIdAsync` stays *tracked* for update/retire |
| `Plan` entity, `IsActive` `:48`, `IsDisability` `:56` | Entity | [Plan.cs:18](../api/OneCover.Api/Models/Plan.cs#L18) | `IsDisability` (STD/LTD) is used by `ClaimService` rule 3 |
| `PlanType` enum | Enum | [PlanType.cs](../api/OneCover.Api/Models/Enums/PlanType.cs) | STD, LTD, Life, Accident, Dental |
| DTOs + mapping | DTO | [PlanDto.cs:19](../api/OneCover.Api/Dtos/PlanDto.cs#L19), [SavePlanRequest.cs:21](../api/OneCover.Api/Dtos/SavePlanRequest.cs#L21), [PlanMappings.cs:29](../api/OneCover.Api/Dtos/PlanMappings.cs#L29) `ToDto`, [:43](../api/OneCover.Api/Dtos/PlanMappings.cs#L43) `ApplyTo` | `ApplyTo` copies request → entity |

### 4.4 Enrollment

| Piece | Layer | File : line | Related |
|---|---|---|---|
| `EnrollmentsController` (`api/enrollments`, class-level Member) | Controller | [EnrollmentsController.cs:18](../api/OneCover.Api/Controllers/EnrollmentsController.cs#L18) | |
| `IEnrollmentService` / `EnrollmentService` | Interface / Service | [IEnrollmentService.cs:12](../api/OneCover.Api/Services/IEnrollmentService.cs#L12) / [EnrollmentService.cs:18](../api/OneCover.Api/Services/EnrollmentService.cs#L18) | `EnrollAsync` `:25`: user id from JWT `:27`, plan active `:33`, no duplicate `:39` |
| First premium created with the enrollment (one save, two inserts) | Service | [EnrollmentService.cs:55-63](../api/OneCover.Api/Services/EnrollmentService.cs#L55) | `Payment.PendingPremium` |
| `IEnrollmentRepository` / `EnrollmentRepository` | Repo | [IEnrollmentRepository.cs:13](../api/OneCover.Api/Repositories/IEnrollmentRepository.cs#L13) / [EnrollmentRepository.cs:18](../api/OneCover.Api/Repositories/EnrollmentRepository.cs#L18) | `HasActiveEnrollmentAsync` `:38` (AnyAsync) |
| Duplicate-key catch (SQL 2601/2627 → friendly 400) | Repo | [EnrollmentRepository.cs:47-60](../api/OneCover.Api/Repositories/EnrollmentRepository.cs#L47) | Backed by the filtered unique index in [AppDbContext.cs:71-74](../api/OneCover.Api/Data/AppDbContext.cs#L71) |
| `Enrollment` entity (join entity user ↔ plan) | Entity | [Enrollment.cs:20](../api/OneCover.Api/Models/Enrollment.cs#L20) | `EnrolledOn` `:35`, `Status` `:38` |
| DTOs + mapping | DTO | [EnrollRequest.cs:13](../api/OneCover.Api/Dtos/EnrollRequest.cs#L13), [EnrollmentDto.cs:14](../api/OneCover.Api/Dtos/EnrollmentDto.cs#L14), [EnrollmentMappings.cs:15](../api/OneCover.Api/Dtos/EnrollmentMappings.cs#L15) | |

### 4.5 Claims (the biggest cluster)

| Piece | Layer | File : line | Related |
|---|---|---|---|
| `ClaimsController` (`api/claims`) | Controller (member) | [ClaimsController.cs:18](../api/OneCover.Api/Controllers/ClaimsController.cs#L18) | Class `[Authorize]` `:17`; GET list/POST add `Roles="Member"` |
| `AdminClaimsController` (`api/admin/claims`) | Controller (admin) | [AdminClaimsController.cs:20](../api/OneCover.Api/Controllers/AdminClaimsController.cs#L20) | `[FromQuery]` paging `:24-28` |
| `AdminDashboardController` | Controller (admin) | [AdminDashboardController.cs:17](../api/OneCover.Api/Controllers/AdminDashboardController.cs#L17) | Uses `IClaimService` (not a separate dashboard service) |
| `IClaimService` / `ClaimService` (6 deps) | Interface / Service | [IClaimService.cs:14](../api/OneCover.Api/Services/IClaimService.cs#L14) / [ClaimService.cs:20](../api/OneCover.Api/Services/ClaimService.cs#L20) | |
| Ownership: not yours = 404, not 403 | Service | [ClaimService.cs:38](../api/OneCover.Api/Services/ClaimService.cs#L38) (enrollment), [:101](../api/OneCover.Api/Services/ClaimService.cs#L101) (claim) | Doesn't reveal other people's ids exist |
| Business rules in the service (enrollment active, disability only) | Service | [ClaimService.cs:44-55](../api/OneCover.Api/Services/ClaimService.cs#L44) | |
| Approve → creates benefit Payment in the **same** save | Service | [ClaimService.cs:138-144](../api/OneCover.Api/Services/ClaimService.cs#L138) | Unit of work |
| Page size cap (`MaxPageSize = 100`) | Service | [ClaimService.cs:29](../api/OneCover.Api/Services/ClaimService.cs#L29), clamp at `:115` | |
| `BenefitClaim` entity | Entity | [BenefitClaim.cs:18](../api/OneCover.Api/Models/BenefitClaim.cs#L18) | private ctor `:56`, `Submit` `:65`, `MoveTo` `:121` |
| Private setters (`Status`, benefits) | Entity | [BenefitClaim.cs:38-43](../api/OneCover.Api/Models/BenefitClaim.cs#L38) | **Encapsulation** |
| Private `_history` list, exposed read-only | Entity | [BenefitClaim.cs:49-52](../api/OneCover.Api/Models/BenefitClaim.cs#L49) | EF finds it by convention |
| `ClaimWorkflow` (allowed-move table) | Entity helper | [ClaimWorkflow.cs:18](../api/OneCover.Api/Models/ClaimWorkflow.cs#L18) | `Allowed` dictionary `:21-29`, `CanMove` `:34`, `NextStatuses` `:39` |
| `ClaimStatusHistory` (audit row) | Entity | [ClaimStatusHistory.cs:18](../api/OneCover.Api/Models/ClaimStatusHistory.cs#L18) | `init` properties = can't be edited |
| `ClaimStatus` enum | Enum | [ClaimStatus.cs](../api/OneCover.Api/Models/Enums/ClaimStatus.cs) | Submitted, InReview, InfoRequested, Approved, Denied, Closed |
| `IClaimRepository` / `ClaimRepository` | Repo | [IClaimRepository.cs:14](../api/OneCover.Api/Repositories/IClaimRepository.cs#L14) / [ClaimRepository.cs:16](../api/OneCover.Api/Repositories/ClaimRepository.cs#L16) | `ClaimsWithDetails` `:23` (Include/ThenInclude), `GetPageAsync` `:44`, `CountByStatusAsync` `:67` |
| `BenefitCalculator` | Service (singleton) | [IBenefitCalculator.cs:12](../api/OneCover.Api/Services/IBenefitCalculator.cs#L12) / [BenefitCalculator.cs:11](../api/OneCover.Api/Services/BenefitCalculator.cs#L11) | `WeeklyBenefit` `:16`, `TotalBenefit` `:25` |
| DTOs + mapping | DTO | [ClaimDto.cs:14](../api/OneCover.Api/Dtos/ClaimDto.cs#L14), [ClaimHistoryDto `:31`](../api/OneCover.Api/Dtos/ClaimDto.cs#L31), [SubmitClaimRequest.cs:16](../api/OneCover.Api/Dtos/SubmitClaimRequest.cs#L16), [UpdateClaimStatusRequest.cs:14](../api/OneCover.Api/Dtos/UpdateClaimStatusRequest.cs#L14), [PagedResult.cs:14](../api/OneCover.Api/Dtos/PagedResult.cs#L14), [DashboardDto.cs:14](../api/OneCover.Api/Dtos/DashboardDto.cs#L14), [ClaimMappings.cs:16](../api/OneCover.Api/Dtos/ClaimMappings.cs#L16) | |

### 4.6 Payments

| Piece | Layer | File : line | Related |
|---|---|---|---|
| `PaymentsController` (member) | Controller | [PaymentsController.cs:17](../api/OneCover.Api/Controllers/PaymentsController.cs#L17) | "Whose statement" comes from the JWT |
| `AdminPaymentsController` | Controller | [AdminPaymentsController.cs:18](../api/OneCover.Api/Controllers/AdminPaymentsController.cs#L18) | |
| `IPaymentService` / `PaymentService` | Interface / Service | [IPaymentService.cs:13](../api/OneCover.Api/Services/IPaymentService.cs#L13) / [PaymentService.cs:16](../api/OneCover.Api/Services/PaymentService.cs#L16) | `UpdateStatusAsync` `:33`, future-date check `:47` |
| `Payment` entity | Entity | [Payment.cs:22](../api/OneCover.Api/Models/Payment.cs#L22) | factories `PendingPremium` `:54`, `PendingBenefit` `:66`; `MarkPaid` `:78`, `MarkFailed` `:90` |
| Exactly-one-link check (enrollment XOR claim) | Data | [AppDbContext.cs:76-81](../api/OneCover.Api/Data/AppDbContext.cs#L76) | Migration: [PaymentLinkCheckAndHistoryTrigger](../api/OneCover.Api/Data/Migrations/20261019211433_PaymentLinkCheckAndHistoryTrigger.cs) |
| `IPaymentRepository` / `PaymentRepository` | Repo | [IPaymentRepository.cs:14](../api/OneCover.Api/Repositories/IPaymentRepository.cs#L14) / [PaymentRepository.cs:15](../api/OneCover.Api/Repositories/PaymentRepository.cs#L15) | `PaymentsWithDetails` `:22` (optional-navigation Includes) |
| Enums | Enum | [PaymentType.cs](../api/OneCover.Api/Models/Enums/PaymentType.cs), [PaymentStatus.cs](../api/OneCover.Api/Models/Enums/PaymentStatus.cs) | Premium/Benefit; Pending/Paid/Failed |
| DTOs + mapping | DTO | [PaymentDto.cs:14](../api/OneCover.Api/Dtos/PaymentDto.cs#L14), [UpdatePaymentStatusRequest.cs:14](../api/OneCover.Api/Dtos/UpdatePaymentStatusRequest.cs#L14), [PaymentMappings.cs:16](../api/OneCover.Api/Dtos/PaymentMappings.cs#L16), `Describe` [:28](../api/OneCover.Api/Dtos/PaymentMappings.cs#L28) | |

### 4.7 Profile and FMLA eligibility

| Piece | Layer | File : line | Related |
|---|---|---|---|
| `MeController` (`api/me`, no id in URL on purpose) | Controller | [MeController.cs:18](../api/OneCover.Api/Controllers/MeController.cs#L18) | |
| `IProfileService` / `ProfileService` | Interface / Service | [IProfileService.cs:12](../api/OneCover.Api/Services/IProfileService.cs#L12) / [ProfileService.cs:14](../api/OneCover.Api/Services/ProfileService.cs#L14) | Calls eligibility at `:35` |
| `IEligibilityService` / `EligibilityService` | Service (singleton) | [IEligibilityService.cs:10](../api/OneCover.Api/Services/IEligibilityService.cs#L10) / [EligibilityService.cs:11](../api/OneCover.Api/Services/EligibilityService.cs#L11) | `IsFmlaEligible` `:15` (12 months + 1,250 hours) |
| `ProfileDto` | DTO | [ProfileDto.cs:13](../api/OneCover.Api/Dtos/ProfileDto.cs#L13) | |

### 4.8 Errors (one approach for the whole API)

| Piece | File : line | What it does |
|---|---|---|
| `NotFoundException` → 404 | [NotFoundException.cs:12](../api/OneCover.Api/Errors/NotFoundException.cs#L12) | Thrown by services |
| `BusinessRuleException` → 400 | [BusinessRuleException.cs:13](../api/OneCover.Api/Errors/BusinessRuleException.cs#L13) | Thrown by services **and entities** (`BenefitClaim`, `Payment`) |
| `UnauthorizedException` → 401 | [UnauthorizedException.cs:14](../api/OneCover.Api/Errors/UnauthorizedException.cs#L14) | |
| `GlobalExceptionHandler` | [GlobalExceptionHandler.cs:21](../api/OneCover.Api/Errors/GlobalExceptionHandler.cs#L21) | `TryHandleAsync` `:26`, status switch `:36`; unexpected errors are logged and return a generic 500 message |
| Registered in | [Program.cs:41-43](../api/OneCover.Api/Program.cs#L41), used at [:108](../api/OneCover.Api/Program.cs#L108) | |
| Angular reads the message | [problem-details.ts:21](../web/src/app/shared/problem-details.ts#L21) `errorMessage()` | Used by every page's `error:` handler |

### 4.9 Data (EF Core, SQL Server)

| Piece | File : line | What it does |
|---|---|---|
| `AppDbContext` + the 6 `DbSet`s | [AppDbContext.cs:21](../api/OneCover.Api/Data/AppDbContext.cs#L21) (sets `:25-35`) | Users, Plans, Claims, Enrollments, ClaimStatusHistories, Payments |
| Conventions: decimal(18,2), enums as strings, UTC dates | [AppDbContext.cs:39-45](../api/OneCover.Api/Data/AppDbContext.cs#L39) | |
| `UtcDateTimeConverter` | [UtcDateTimeConverter.cs:19](../api/OneCover.Api/Data/UtcDateTimeConverter.cs#L19) | Dates come back as UTC so JSON gets a `Z` |
| Unique email index | [AppDbContext.cs:60](../api/OneCover.Api/Data/AppDbContext.cs#L60) | |
| Filtered unique index (one Active enrollment per user per plan) | [AppDbContext.cs:71-74](../api/OneCover.Api/Data/AppDbContext.cs#L71) | Migration: [UniqueActiveEnrollment](../api/OneCover.Api/Data/Migrations/20261018191150_UniqueActiveEnrollment.cs) |
| Check constraint on Payments | [AppDbContext.cs:79-81](../api/OneCover.Api/Data/AppDbContext.cs#L79) | |
| Append-only trigger declared for EF | [AppDbContext.cs:86](../api/OneCover.Api/Data/AppDbContext.cs#L86) | Trigger itself: [ClaimStatusHistory_AppendOnly.sql](../sql/ClaimStatusHistory_AppendOnly.sql) (run once in SSMS) |
| No cascade deletes (`Restrict`) | [AppDbContext.cs:91-92](../api/OneCover.Api/Data/AppDbContext.cs#L91) | Nothing is hard-deleted |
| Migrations | [Data/Migrations/](../api/OneCover.Api/Data/Migrations/) | `InitialCreate`, `UniqueActiveEnrollment`, `PaymentLinkCheckAndHistoryTrigger`, plus the model snapshot |
| `DbSeeder` | [DbSeeder.cs:28](../api/OneCover.Api/Data/DbSeeder.cs#L28) | 5 plans + 3 users; demo password at `:22`; skips tables that have data |

---

## 5. Web by feature

Base path for all links: `web/src/app/`.

### 5.1 Core, shell, auth, routing

| Piece | Layer | File : line | Related |
|---|---|---|---|
| Boot | Entry | [main.ts](../web/src/main.ts) | `bootstrapApplication(App, appConfig)` |
| App providers (router, `HttpClient`, interceptor) | Config | [app.config.ts:45-50](../web/src/app/app.config.ts#L45) | Angular's `Program.cs` services half |
| Routes + lazy loading + role guards | Routing | [app.routes.ts:11-30](../web/src/app/app.routes.ts#L11) | member `:17`, admin `:22`, redirects `:28-29` |
| Member child routes | Routing | [member.routes.ts:9](../web/src/app/features/member/member.routes.ts#L9) | `claims/new` (`:13`) is listed **before** `claims/:id` on purpose |
| Admin child routes | Routing | [admin.routes.ts](../web/src/app/features/admin/admin.routes.ts) | |
| Root component + nav | Shell | [app.ts:20](../web/src/app/app.ts#L20), [app.html](../web/src/app/app.html) | Nav switches on `auth.role()` |
| `AuthService` (signals: `token` `:33`, `role` `:36`, `isLoggedIn` `:40`) | Core | [auth.service.ts:28](../web/src/app/core/auth.service.ts#L28) | `login` `:43`, `register` `:49`, `hasValidSession` `:59`, `logout` `:69`, `homeRoute` `:75`, `restoreToken` `:97` |
| Token in `localStorage` | Core | [auth.service.ts:23](../web/src/app/core/auth.service.ts#L23) (`TOKEN_KEY`) | Fine for training; httpOnly cookies are the production-leaning alternative |
| `authInterceptor` (adds Bearer, logs out on 401) | Core | [auth.interceptor.ts:18](../web/src/app/core/auth.interceptor.ts#L18) | Only sends the token to `API_URL`, never on `/auth/` calls (`:120-122`) |
| `roleGuard('Admin' \| 'Member')` | Core | [role.guard.ts:17](../web/src/app/core/role.guard.ts#L17) | UX only; the API's `[Authorize]` is the real gate |
| JWT decode helpers (`decodeJwtPayload` `:18`, `isTokenExpired` `:33`, `roleFromToken` `:39`) | Shared | [jwt.ts](../web/src/app/shared/jwt.ts) | Role comes **from the token**, not from localStorage; long .NET role claim name at `:15` |
| `API_URL` | Core | [api.ts:13](../web/src/app/core/api.ts#L13) | From [environment.ts](../web/src/environments/environment.ts) (prod/Azure) and [environment.development.ts](../web/src/environments/environment.development.ts) (localhost:5173); swapped by `fileReplacements` in [angular.json:54-57](../web/angular.json#L54) |
| Azure SPA fallback | Config | [staticwebapp.config.json](../web/public/staticwebapp.config.json) | Deep links rewrite to `index.html` |

### 5.2 HTTP services (one per API area; each returns Observables)

| Service | File | Methods : line | Calls |
|---|---|---|---|
| `AuthService` | [auth.service.ts](../web/src/app/core/auth.service.ts) | see above | `/auth/*` |
| `PlanService` | [plan.service.ts:15](../web/src/app/core/plan.service.ts#L15) | `getActive` `:19`, `create` `:24`, `update` `:29`, `retire` `:34` | `/plans` |
| `EnrollmentService` | [enrollment.service.ts:14](../web/src/app/core/enrollment.service.ts#L14) | `getMine` `:18`, `enroll` `:23` | `/enrollments` |
| `ClaimService` | [claim.service.ts:15](../web/src/app/core/claim.service.ts#L15) | `getMine` `:19`, `getById` `:24`, `submit` `:29` | `/claims` |
| `AdminClaimService` | [admin-claim.service.ts:16](../web/src/app/core/admin-claim.service.ts#L16) | `getPage` `:21`, `updateStatus` `:32`, `getDashboard` `:37` | `/admin/claims`, `/admin/dashboard` |
| `PaymentService` | [payment.service.ts:14](../web/src/app/core/payment.service.ts#L14) | `getMine` `:18` | `/payments` |
| `AdminPaymentService` | [admin-payment.service.ts:15](../web/src/app/core/admin-payment.service.ts#L15) | `getAll` `:20`, `updateStatus` `:26` | `/admin/payments` |
| `ProfileService` | [profile.service.ts:13](../web/src/app/core/profile.service.ts#L13) | `getMine` `:17` | `/me` |

### 5.3 Member pages (`/member/...`)

| URL | Component | File : line | Services used | Notable concept |
|---|---|---|---|---|
| `/member/plans` | `PlansPage` | [plans.ts:75](../web/src/app/features/member/plans.ts#L75) | Plan, Enrollment | `forkJoin` `:88` loads plans + enrollments together; enroll `:109` |
| `/member/coverage` | `CoveragePage` | [coverage.ts:62](../web/src/app/features/member/coverage.ts#L62) | Enrollment | |
| `/member/claims` | `ClaimsPage` | [claims.ts:65](../web/src/app/features/member/claims.ts#L65) | Claim | Material table |
| `/member/claims/new` | `FileClaimPage` | [file-claim.ts:160](../web/src/app/features/member/file-claim.ts#L160) | Claim, Enrollment | Two-step reactive forms `:173`, `:190`; group validators `:196`; `toSignal` `:179`; `computed` `:185`; `effect` `:203` |
| `/member/claims/:id` | `ClaimDetailPage` | [claim-detail.ts:38](../web/src/app/features/member/claim-detail.ts#L38) | Claim | `route.paramMap` + `switchMap` + `takeUntilDestroyed` (`:65`) |
| `/member/statements` | `StatementsPage` | [statements.ts:61](../web/src/app/features/member/statements.ts#L61) | Payment | |
| `/member/profile` | `ProfilePage` | [profile.ts:64](../web/src/app/features/member/profile.ts#L64) | Profile | `maskSsn` pipe in template `:37` |

### 5.4 Admin pages (`/admin/...`)

| URL | Component | File : line | Services used | Notable concept |
|---|---|---|---|---|
| `/admin` | `DashboardPage` | [dashboard.ts:74](../web/src/app/features/admin/dashboard.ts#L74) | AdminClaim | Cards link to `/admin/claims?status=…` via `[queryParams]` `:41` |
| `/admin/claims` | `ClaimsQueuePage` | [claims-queue.ts:107](../web/src/app/features/admin/claims-queue.ts#L107) | AdminClaim | URL is the source of truth for the filter (`queryParamMap` `:138`); `computed` query `:128`; `toObservable` + `switchMap` `:149-170`; paginator `onPage` `:182` |
| `/admin/claims/:id` | `ClaimReviewPage` | [claim-review.ts:80](../web/src/app/features/admin/claim-review.ts#L80) | Claim, AdminClaim | `moveForm` `:91`; `move()` `:123` → `updateStatus` `:132` |
| `/admin/payments` | `AdminPaymentsPage` | [payments.ts:106](../web/src/app/features/admin/payments.ts#L106) | AdminPayment | Mark paid/failed `:148` |
| `/admin/plans` | `PlansAdminPage` | [plans-admin.ts:78](../web/src/app/features/admin/plans-admin.ts#L78) | Plan | Opens `MatDialog` with `PlanDialog`; retire `:126` |
| *(dialog)* | `PlanDialog` | [plan-dialog.ts:100](../web/src/app/features/admin/plan-dialog.ts#L100) | Plan | Form `:113`, disability-only fields toggled by `valueChanges` `:133`; one `save$` for create or update `:175` |

### 5.5 Auth pages

| URL | Component | File : line | Notable concept |
|---|---|---|---|
| `/login` | `LoginPage` | [login.ts:53](../web/src/app/features/auth/login.ts#L53) | Form `:62`; `auth.login(...).subscribe` `:76` |
| `/register` | `RegisterPage` | [register.ts:91](../web/src/app/features/auth/register.ts#L91) | Form `:100-108` mirrors the API's `RegistrationRequest` attributes |

### 5.6 Shared (reused building blocks)

| Piece | File : line | Used by |
|---|---|---|
| Models (TypeScript mirrors of the API DTOs): `Plan` `:35`, `Enrollment` `:50`, `Profile` `:61`, `Claim` `:86`, `Payment` `:115`, `PagedResult<T>` `:128`, `Dashboard` `:164` | [models.ts](../web/src/app/shared/models.ts) | Every service and page |
| `ClaimSummary` component (`<app-claim-summary>`) | [claim-summary.ts:63](../web/src/app/shared/claim-summary.ts#L63) | claim-detail and claim-review |
| `StatusBadge` (`<app-status-badge>`, `computed` label/class) | [status-badge.ts:31](../web/src/app/shared/status-badge.ts#L31) | Claims and payments tables |
| `statusLabel` / `StatusLabelPipe` | [status-label.ts:12](../web/src/app/shared/status-label.ts#L12) / [:18](../web/src/app/shared/status-label.ts#L18) | "InReview" → "In review" |
| `MaskSsnPipe` (`maskSsn`) | [mask-ssn.pipe.ts:12](../web/src/app/shared/mask-ssn.pipe.ts#L12) | profile.ts |
| Date validators | [validators.ts](../web/src/app/shared/validators.ts) | file-claim.ts |
| `errorMessage()` | [problem-details.ts:21](../web/src/app/shared/problem-details.ts#L21) | Every page's error handler |
| Global styles | [styles.scss](../web/src/styles.scss) | |

---

## 6. "Where is the...?" concept index

The questions an interviewer (or you, at 11pm) will ask. Each answer is where to look.

| Question | API | WEB |
|---|---|---|
| **Dependency injection** (registration) | [Program.cs:50-74](../api/OneCover.Api/Program.cs#L50) | [app.config.ts:45-50](../web/src/app/app.config.ts#L45) |
| DI (usage / constructor injection) | Primary constructors, e.g. [ClaimService.cs:20-27](../api/OneCover.Api/Services/ClaimService.cs#L20) | `inject(...)`, e.g. [file-claim.ts:161-164](../web/src/app/features/member/file-claim.ts#L161) |
| DI lifetimes (Singleton vs Scoped) | Singleton [Program.cs:38, 73-74](../api/OneCover.Api/Program.cs#L73) vs Scoped `:34, 50-69` | `@Service()` = app-wide singleton, [auth.service.ts:27](../web/src/app/core/auth.service.ts#L27) |
| **Abstraction / interfaces** | Every `I*.cs` in `Services/` and `Repositories/` | |
| **Encapsulation** | [BenefitClaim.cs:38-56](../api/OneCover.Api/Models/BenefitClaim.cs#L38) (private setters/ctor), [Payment.cs:44-51](../api/OneCover.Api/Models/Payment.cs#L44) | `protected readonly` signals in components |
| **Inheritance** | `AppDbContext : DbContext` [AppDbContext.cs:21](../api/OneCover.Api/Data/AppDbContext.cs#L21); controllers `: ControllerBase`; exceptions `: Exception` | |
| **Polymorphism** | `override ConfigureConventions` / `OnModelCreating` [AppDbContext.cs:39,49](../api/OneCover.Api/Data/AppDbContext.cs#L39); one interface, real class in prod and fake in tests | `PipeTransform`s ([mask-ssn.pipe.ts:12](../web/src/app/shared/mask-ssn.pipe.ts#L12)) |
| **Authentication (who are you)** | JWT issue [AuthService.cs:107](../api/OneCover.Api/Services/AuthService.cs#L107); validate [Program.cs:81-91](../api/OneCover.Api/Program.cs#L81), `UseAuthentication` `:131` | Login → store token [auth.service.ts:43](../web/src/app/core/auth.service.ts#L43); attach [auth.interceptor.ts:18](../web/src/app/core/auth.interceptor.ts#L18) |
| **Authorization (what may you do)** | `[Authorize(Roles=…)]` on controllers; `UseAuthorization` [Program.cs:132](../api/OneCover.Api/Program.cs#L132) | [role.guard.ts:17](../web/src/app/core/role.guard.ts#L17) on [app.routes.ts:19,24](../web/src/app/app.routes.ts#L19) |
| **"Members only see their own data"** | `ICurrentUser.UserId` from the JWT: [CurrentUser.cs:27](../api/OneCover.Api/Services/CurrentUser.cs#L27); used at [ClaimService.cs:38,91,101](../api/OneCover.Api/Services/ClaimService.cs#L38), [EnrollmentService.cs:27](../api/OneCover.Api/Services/EnrollmentService.cs#L27), [PaymentService.cs:23](../api/OneCover.Api/Services/PaymentService.cs#L23) | |
| **Password hashing** | [AuthService.cs:38,73,93](../api/OneCover.Api/Services/AuthService.cs#L38) | |
| **SSN: only last 4** | [User.cs](../api/OneCover.Api/Models/User.cs#L17) `SsnLast4`; [RegistrationRequest.cs:40](../api/OneCover.Api/Dtos/RegistrationRequest.cs#L40) regex; column [AppDbContext.cs:62](../api/OneCover.Api/Data/AppDbContext.cs#L62) | Masked: [mask-ssn.pipe.ts](../web/src/app/shared/mask-ssn.pipe.ts), [profile.ts:37](../web/src/app/features/member/profile.ts#L37) |
| **Input validation** | Attributes on request DTOs (`[Required]`, `Range`…) auto-checked by `[ApiController]`; domain rules in entities ([BenefitClaim.cs:70-87](../api/OneCover.Api/Models/BenefitClaim.cs#L70)) | Reactive form validators; [validators.ts](../web/src/app/shared/validators.ts) |
| **Error handling** | [GlobalExceptionHandler.cs:26-56](../api/OneCover.Api/Errors/GlobalExceptionHandler.cs#L26) | [problem-details.ts:21](../web/src/app/shared/problem-details.ts#L21); interceptor 401 [auth.interceptor.ts:133](../web/src/app/core/auth.interceptor.ts#L133) |
| **Claim workflow / state machine** | [ClaimWorkflow.cs:21-29](../api/OneCover.Api/Models/ClaimWorkflow.cs#L21) + [BenefitClaim.MoveTo :121](../api/OneCover.Api/Models/BenefitClaim.cs#L121) | [claim-review.ts:123-132](../web/src/app/features/admin/claim-review.ts#L123) |
| **Audit trail** | `ClaimStatusHistory` rows written in `Submit` `:105` and `MoveTo` `:130`; DB trigger [ClaimStatusHistory_AppendOnly.sql](../sql/ClaimStatusHistory_AppendOnly.sql) | History list in [claim-summary.ts:44](../web/src/app/shared/claim-summary.ts#L44) |
| **Benefit calculation** | [BenefitCalculator.cs:16,25](../api/OneCover.Api/Services/BenefitCalculator.cs#L16) | |
| **FMLA eligibility** | [EligibilityService.cs:15](../api/OneCover.Api/Services/EligibilityService.cs#L15) | Shown from `Profile.isFmlaEligible` ([models.ts:61](../web/src/app/shared/models.ts#L61)) |
| **Unit of work / one transaction** | [ClaimService.cs:143-144](../api/OneCover.Api/Services/ClaimService.cs#L143) (status + history + payment), [EnrollmentService.cs:58-63](../api/OneCover.Api/Services/EnrollmentService.cs#L58) | |
| **Change tracking** | Update with no `Update()` call: [PlanService.cs:46-60](../api/OneCover.Api/Services/PlanService.cs#L46); tracked vs `AsNoTracking` split in repos | |
| **Eager loading (Include)** | [ClaimRepository.cs:23-26](../api/OneCover.Api/Repositories/ClaimRepository.cs#L23), [PaymentRepository.cs:22-25](../api/OneCover.Api/Repositories/PaymentRepository.cs#L22), [EnrollmentRepository.cs:24](../api/OneCover.Api/Repositories/EnrollmentRepository.cs#L24) | |
| **Paging** | [ClaimRepository.cs:44-65](../api/OneCover.Api/Repositories/ClaimRepository.cs#L44), cap [ClaimService.cs:29,115](../api/OneCover.Api/Services/ClaimService.cs#L29) | [claims-queue.ts:116-132,182](../web/src/app/features/admin/claims-queue.ts#L116) |
| **Soft delete** | [PlanService.cs:65](../api/OneCover.Api/Services/PlanService.cs#L65); `Restrict` deletes [AppDbContext.cs:91](../api/OneCover.Api/Data/AppDbContext.cs#L91) | |
| **Defense in depth (service check + DB rule)** | Duplicate enrollment: service [EnrollmentService.cs:39](../api/OneCover.Api/Services/EnrollmentService.cs#L39) + index [AppDbContext.cs:71](../api/OneCover.Api/Data/AppDbContext.cs#L71) + catch [EnrollmentRepository.cs:55](../api/OneCover.Api/Repositories/EnrollmentRepository.cs#L55) | Date rules in [validators.ts](../web/src/app/shared/validators.ts) + `BenefitClaim.Submit` |
| **DTO mapping (named arguments)** | [ClaimMappings.cs:16](../api/OneCover.Api/Dtos/ClaimMappings.cs#L16), [PlanMappings.cs:29](../api/OneCover.Api/Dtos/PlanMappings.cs#L29) | |
| **Enums as text in JSON** | [Program.cs:29](../api/OneCover.Api/Program.cs#L29) (API), [AppDbContext.cs:42](../api/OneCover.Api/Data/AppDbContext.cs#L42) (DB) | String-literal unions in [models.ts:11-12, 74](../web/src/app/shared/models.ts#L11) |
| **async/await** | Every `*Async` method; startup seeding [Program.cs:117](../api/OneCover.Api/Program.cs#L117) | |
| **Observables / RxJS** | | Services return them; `switchMap` [claims-queue.ts:149](../web/src/app/features/admin/claims-queue.ts#L149), [claim-review.ts:100-120](../web/src/app/features/admin/claim-review.ts#L100); `forkJoin` [plans.ts:88](../web/src/app/features/member/plans.ts#L88); interceptor `catchError` [auth.interceptor.ts:129](../web/src/app/core/auth.interceptor.ts#L129) |
| **Signals / computed / effect** | | `signal` + `computed` [auth.service.ts:33-40](../web/src/app/core/auth.service.ts#L33); `effect` [file-claim.ts:203](../web/src/app/features/member/file-claim.ts#L203); `computed` query [claims-queue.ts:128](../web/src/app/features/admin/claims-queue.ts#L128) |
| **Reactive forms** | | [login.ts:62](../web/src/app/features/auth/login.ts#L62), [register.ts:100](../web/src/app/features/auth/register.ts#L100), [file-claim.ts:173,190](../web/src/app/features/member/file-claim.ts#L173), [plan-dialog.ts:113](../web/src/app/features/admin/plan-dialog.ts#L113), [claim-review.ts:91](../web/src/app/features/admin/claim-review.ts#L91) |
| **Lazy loading** | | [app.routes.ts:13-25](../web/src/app/app.routes.ts#L13) |
| **Unsubscribe / memory leaks** | | `takeUntilDestroyed()` e.g. [claim-detail.ts:65](../web/src/app/features/member/claim-detail.ts#L65) |
| **Environments (dev vs prod URL)** | `appsettings*.json`, user-secrets (`Jwt:Key`) | [environment.ts](../web/src/environments/environment.ts), [environment.development.ts](../web/src/environments/environment.development.ts), [angular.json:54-57](../web/angular.json#L54) |
| **CORS** | [Program.cs:99-101,129](../api/OneCover.Api/Program.cs#L99) | Why the dev API URL must be plain `http` (comment in [environment.development.ts](../web/src/environments/environment.development.ts)) |
| **Deployment (Azure)** | `Seed:DemoData`, `Cors__Origins__0` app settings | [staticwebapp.config.json](../web/public/staticwebapp.config.json), prod [environment.ts](../web/src/environments/environment.ts) |

---

## 7. Tests, config, SQL, tooling

### 7.1 API tests (xUnit) — `api/OneCover.Tests/`

| File | Covers | Test names (line) |
|---|---|---|
| [BenefitCalculatorTests.cs](../api/OneCover.Tests/BenefitCalculatorTests.cs) | `BenefitCalculator` | cap `:26`, non-disability zero `:35`, elimination period `:44`, `:55`, partial week `:66`, both end days counted `:77` |
| [BenefitClaimTests.cs](../api/OneCover.Tests/BenefitClaimTests.cs) | `BenefitClaim.Submit` / `MoveTo` | submit `:32`, bad dates `:44`, over a year `:53`, before coverage `:61`, valid move `:69`, invalid move `:85`, blank note `:98` |
| [ClaimWorkflowTests.cs](../api/OneCover.Tests/ClaimWorkflowTests.cs) | `ClaimWorkflow` | `[Theory]` allowed `:24`, blocked `:36`, `NextStatuses` `:42`, `:48` |
| [EligibilityServiceTests.cs](../api/OneCover.Tests/EligibilityServiceTests.cs) | FMLA rule (fake clock) | boundary cases `:27-45` |
| [PaymentTests.cs](../api/OneCover.Tests/PaymentTests.cs) | `Payment` factories + `MarkPaid`/`MarkFailed` | `:20-52` |
| [PlanTests.cs](../api/OneCover.Tests/PlanTests.cs) | `Plan.IsDisability` | `[Theory]` `:21` |

Run: `dotnet test` from `api/`.

### 7.2 Web tests (Vitest) — `web/src/app/`

| File | Covers |
|---|---|
| [app.spec.ts](../web/src/app/app.spec.ts) | Nav shows the right links per role (`:41`, `:48`, `:56`) |
| [auth.service.spec.ts](../web/src/app/core/auth.service.spec.ts) | login `:35`, restore token `:71`, expiry `:95` |
| [mask-ssn.pipe.spec.ts](../web/src/app/shared/mask-ssn.pipe.spec.ts) | SSN masking |
| [validators.spec.ts](../web/src/app/shared/validators.spec.ts) | the 3 date validators |
| [testing/fake-jwt.ts](../web/src/testing/fake-jwt.ts) | helper that builds fake tokens for tests |

Run: `ng test` from `web/`.

### 7.3 Config and tooling

| File | What it is |
|---|---|
| [OneCover.Api.csproj](../api/OneCover.Api/OneCover.Api.csproj) / [OneCover.Tests.csproj](../api/OneCover.Tests/OneCover.Tests.csproj) | NuGet packages and target framework |
| [appsettings.json](../api/OneCover.Api/appsettings.json) / [appsettings.Development.json](../api/OneCover.Api/appsettings.Development.json) | Issuer, audience, CORS origins, connection string; **`Jwt:Key` is NOT here** (user-secrets / Azure) |
| [launchSettings.json](../api/OneCover.Api/Properties/launchSettings.json) | Dev ports (the API runs on 5173 over http) |
| [OneCover.slnx](../api/OneCover.slnx) | Solution file |
| [angular.json](../web/angular.json) / [package.json](../web/package.json) | Angular build + scripts |
| [OneCover.postman_collection.json](../postman/OneCover.postman_collection.json) | Postman folders: Plans, Enrollments, Claims, Admin, Payments, Profile, plus Login/Who Am I/Admin Check |
| [PROJECT_PLAN.md](../PROJECT_PLAN.md), [DEV_JOURNAL.md](../DEV_JOURNAL.md), [PROJECT_TALKING_POINTS.md](../PROJECT_TALKING_POINTS.md), [BRD.md](BRD.md), [AI_USAGE.md](AI_USAGE.md) | Planning and interview material |

---

## 8. Ideas to make this map better

1. **Use the symbol, not just the line.** When a link is off by a few lines (it will be, after edits), `Ctrl+T` and type the symbol (`MoveTo`, `ClaimsWithDetails`) to jump straight there.
2. **Pin the sections you're weakest on.** Print or keep section 2 (end-to-end claim) and section 6 (concept index) open while you practice the quiz in [PROJECT_QUIZ.md](PROJECT_QUIZ.md).
3. **Add a second end-to-end walk-through** for *approving a claim* (admin → `MoveTo` → payment created) and *enrolling* (premium created). Same table shape as section 2. Say the word and I'll add them.
4. **Add a data model diagram** (User ↔ Enrollment ↔ Plan; Enrollment → Payment; Claim → History, Claim → Payment). Good for interviews because you can draw it from memory.
5. **Refresh after changes.** Ask me to "refresh the code map" after a big edit session and I'll re-verify every line number.
