# OneCover Project Quiz

A self-quiz on **your own project**: ASP.NET Core and Angular fundamentals as they show up in OneCover. The goal is to answer quickly, out loud, and point at the file.

## How to use this

1. **Cover the answer.** Each answer is hidden under a "Show answer" arrow. Say your answer out loud first (a real sentence, like you would in an interview), *then* open it.
2. **Two parts to every answer:** the *concept* (what it is) and the *where* (file and line, from [CODE_MAP.md](CODE_MAP.md)). Try to name both.
3. **Score yourself** per question: ✅ got it · 🟡 partly · ❌ missed. Redo the 🟡 and ❌ the next day.
4. **Speed rounds:** after you're comfortable, do a section with a 20-second limit per question.

> Line numbers are a snapshot from Oct 22, 2026. If one is off, search the symbol name.

**Sections:** [A. Big picture](#a-big-picture) · [B. Startup, DI, middleware](#b-startup-di-and-middleware) · [C. Auth and security](#c-auth-and-security) · [D. Controllers, DTOs, validation, errors](#d-controllers-dtos-validation-and-errors) · [E. Services, entities, OOP](#e-services-entities-and-oop) · [F. EF Core and SQL Server](#f-ef-core-and-sql-server) · [G. Angular fundamentals](#g-angular-fundamentals) · [H. Angular forms and RxJS](#h-angular-forms-and-rxjs) · [I. Tests](#i-tests) · [J. "What happens when…" walk-throughs](#j-what-happens-when-walk-throughs) · [K. Debugging scenarios](#k-debugging-scenarios) · [L. Say-it-out-loud prompts](#l-say-it-out-loud-prompts) · [Score sheet](#score-sheet)

---

## A. Big picture

**A1.** In one or two sentences, what is OneCover and who uses it?

<details><summary>Show answer</summary>

An employee benefits enrollment and claims app. **Members** browse and enroll in plans, file disability leave claims, and see their payment statements. **Admins** manage plans, review claims through a workflow, and mark payments paid or failed. Stack: ASP.NET Core Web API (.NET 10) + EF Core + SQL Server on the back end, Angular + Angular Material on the front end, JWT for auth. (See [README.md](../README.md), [CLAUDE.md](../CLAUDE.md).)
</details>

**A2.** Name the API's layers in order, and say what each one is allowed to do.

<details><summary>Show answer</summary>

Controller → Service → Repository → (AppDbContext/SQL Server).
- **Controller:** HTTP only. Receives the request, calls a service, returns a status code. No business logic. (e.g. [ClaimsController.cs](../api/OneCover.Api/Controllers/ClaimsController.cs#L18))
- **Service:** business rules, ownership checks, coordinating several repositories. (e.g. [ClaimService.cs](../api/OneCover.Api/Services/ClaimService.cs#L20))
- **Repository:** the only place that queries the database with EF Core. (e.g. [ClaimRepository.cs](../api/OneCover.Api/Repositories/ClaimRepository.cs#L16))
- **Entities** hold their own rules, and **DTOs** are what leave the API.
</details>

**A3.** Why do you return DTOs instead of entities?

<details><summary>Show answer</summary>

Entities have things clients must never see or control (e.g. `User.PasswordHash`), and they're shaped for the database, not for the UI. A DTO is a deliberate contract: only the fields the client needs, in the shape it needs, and I can change the database without breaking the front end. Entities are mapped to DTOs in the `*Mappings.cs` files ([ClaimMappings.cs:16](../api/OneCover.Api/Dtos/ClaimMappings.cs#L16)). The same idea on the way in: request DTOs like [SubmitClaimRequest](../api/OneCover.Api/Dtos/SubmitClaimRequest.cs#L16) stop a client from setting fields like `Status` or `Role`.
</details>

**A4.** Where does the Angular app live, and what's the Angular equivalent of `Program.cs`?

<details><summary>Show answer</summary>

`web/src/app`. The startup/provider half of `Program.cs` is [app.config.ts](../web/src/app/app.config.ts#L45) (router, `HttpClient`, interceptor); the "route table" part is [app.routes.ts](../web/src/app/app.routes.ts#L11); [main.ts](../web/src/main.ts) boots it.
</details>

**A5.** Name three things on the web side that mirror something on the API side.

<details><summary>Show answer</summary>

- `Program.cs` services ⇄ `app.config.ts` providers.
- Middleware ⇄ `authInterceptor` ([auth.interceptor.ts:18](../web/src/app/core/auth.interceptor.ts#L18)).
- `[Authorize(Roles=…)]` ⇄ `roleGuard` ([role.guard.ts:17](../web/src/app/core/role.guard.ts#L17)). The guard is only UX; the API is the real gate.
- DTOs ⇄ TypeScript interfaces in [models.ts](../web/src/app/shared/models.ts).
- Controller routes ⇄ URLs in `core/*.service.ts`.
</details>

---

## B. Startup, DI, and middleware

**B1.** What is dependency injection, and where do you register things in this project?

<details><summary>Show answer</summary>

Instead of a class creating its own dependencies (`new ClaimRepository()`), it *asks* for them in its constructor and the DI container supplies them. That makes classes loosely coupled and testable (swap in a fake). Registration happens in [Program.cs:50-74](../api/OneCover.Api/Program.cs#L50), e.g. `AddScoped<IClaimService, ClaimService>()` ("when something asks for `IClaimService`, give it a `ClaimService`"). On the web side the equivalent is `inject(...)` with providers in [app.config.ts](../web/src/app/app.config.ts#L45).
</details>

**B2.** What are the three DI lifetimes? Which do you use, and why?

<details><summary>Show answer</summary>

- **Transient:** new instance every time (not used here).
- **Scoped:** one instance per HTTP request. Used for `AppDbContext`, all repositories, all services, and `ICurrentUser`, because they depend on the per-request DbContext and the per-request user ([Program.cs:34, 50-69](../api/OneCover.Api/Program.cs#L34)).
- **Singleton:** one instance for the whole app. Used for `TimeProvider`, `IBenefitCalculator`, `IEligibilityService` because they're pure rules with no per-request state ([Program.cs:38, 73-74](../api/OneCover.Api/Program.cs#L73)).

Rule of thumb: a service can't safely outlive what it depends on, so a singleton must not depend on a scoped service.
</details>

**B3.** Why are the repositories Scoped and not Singleton?

<details><summary>Show answer</summary>

They use `AppDbContext`, which is scoped (one per request, so each request gets its own change tracker and connection). A singleton repository would hold on to one DbContext forever and share it across requests, which is unsafe (DbContext isn't thread-safe). [Program.cs:52-60](../api/OneCover.Api/Program.cs#L52) has the comment.
</details>

**B4.** What's `TimeProvider` doing in the DI container, and why not just call `DateTime.UtcNow`?

<details><summary>Show answer</summary>

It's an injected clock ([Program.cs:38](../api/OneCover.Api/Program.cs#L38)). Services call `time.GetUtcNow()` (e.g. [ClaimService.cs:78](../api/OneCover.Api/Services/ClaimService.cs#L78)). In tests I can pass a fake clock so date-based rules (FMLA's 12 months, claim dates) are deterministic. `DateTime.UtcNow` can't be controlled in a test.
</details>

**B5.** What is the middleware pipeline, and does the order matter in your `Program.cs`?

<details><summary>Show answer</summary>

A chain of components every request passes through, top to bottom ([Program.cs:108-134](../api/OneCover.Api/Program.cs#L108)). Order matters:
1. `UseExceptionHandler` first, so it catches errors from everything after it.
2. `UseStatusCodePages` turns empty error responses (like the JWT middleware's bare 401) into ProblemDetails.
3. `UseHttpsRedirection`, then `UseCors("web")`.
4. `UseAuthentication` (who are you?) **before** `UseAuthorization` (are you allowed?). You can't check permissions before you know the user.
5. `MapControllers` last.
</details>

**B6.** What's the split at `builder.Build()`?

<details><summary>Show answer</summary>

Above it ([Program.cs:23-101](../api/OneCover.Api/Program.cs#L23)) you **register services** in the DI container. Below it ([:104-136](../api/OneCover.Api/Program.cs#L104)) you **configure the request pipeline**. Services are registered once at startup; the pipeline runs for each request.
</details>

**B7.** How does the app avoid keeping secrets in source control?

<details><summary>Show answer</summary>

`Jwt:Key` is not in `appsettings.json`. Locally it comes from `dotnet user-secrets`; on Azure it's an App Service setting (environment variable). Configuration is layered (appsettings → appsettings.Development → user-secrets → env vars). If the key is missing the app **fails fast** at startup with a clear message ([Program.cs:77-78](../api/OneCover.Api/Program.cs#L77)).
</details>

**B8.** What is CORS, and how is it configured here?

<details><summary>Show answer</summary>

Browsers block a page from calling an API on a different origin unless the API says it's allowed. Angular runs on `localhost:4200` and the API on another port, so the API has a CORS policy named `"web"` ([Program.cs:99-101](../api/OneCover.Api/Program.cs#L99)), applied with `UseCors("web")` at [:129](../api/OneCover.Api/Program.cs#L129). The allowed origins come from config (`Cors:Origins`, [appsettings.json:13](../api/OneCover.Api/appsettings.json#L13)), so on Azure an app setting swaps in the deployed site's URL without a code change.
</details>

**B9.** What does `AddControllers().AddJsonOptions(... JsonStringEnumConverter ...)` do?

<details><summary>Show answer</summary>

Sends and accepts enums as text (`"InReview"`) instead of numbers (`1`) ([Program.cs:28-29](../api/OneCover.Api/Program.cs#L28)). The Angular models use matching string unions like `'Submitted' | 'InReview' | …` ([models.ts:74](../web/src/app/shared/models.ts#L74)). The database also stores enums as strings ([AppDbContext.cs:42](../api/OneCover.Api/Data/AppDbContext.cs#L42)).
</details>

---

## C. Auth and security

**C1.** Walk me through login, from the form to being "logged in".

<details><summary>Show answer</summary>

1. [login.ts:76](../web/src/app/features/auth/login.ts#L76) calls `auth.login(...)` and subscribes.
2. [auth.service.ts:43](../web/src/app/core/auth.service.ts#L43) POSTs to `/api/auth/login`, and `tap()` saves the session.
3. API: [AuthController.cs:29](../api/OneCover.Api/Controllers/AuthController.cs#L29) → [AuthService.LoginAsync :84](../api/OneCover.Api/Services/AuthService.cs#L84): find user by email, verify the password hash, throw `UnauthorizedException` (401) if wrong, else build a JWT ([:107](../api/OneCover.Api/Services/AuthService.cs#L107)) and return it.
4. Angular stores the token in `localStorage` and sets the `token` signal ([auth.service.ts:84-86](../web/src/app/core/auth.service.ts)); `role` and `isLoggedIn` are `computed` from it, so the nav updates automatically.
</details>

**C2.** What's in your JWT, how is it protected, and how long does it last?

<details><summary>Show answer</summary>

Three dot-separated parts: header, payload, signature. The payload has claims: user id (`NameIdentifier`), email, and role ([AuthService.cs:111-116](../api/OneCover.Api/Services/AuthService.cs#L111)), plus issuer, audience, and expiry (2 hours, [:133](../api/OneCover.Api/Services/AuthService.cs#L133)). It's **signed** (HMAC-SHA256 with the secret key), not encrypted, so anyone can read it but nobody can change it without breaking the signature. That's why nothing secret (password, SSN) goes in it.
</details>

**C3.** What does the API check on every incoming token?

<details><summary>Show answer</summary>

[Program.cs:81-91](../api/OneCover.Api/Program.cs#L81): `ValidateIssuer` (made by OneCover.Api?), `ValidateAudience` (meant for OneCover.Web?), `ValidateLifetime` (expired?), `ValidateIssuerSigningKey` (signature matches the secret?).
</details>

**C4.** Authentication vs authorization: what's the difference, and where is each in your project?

<details><summary>Show answer</summary>

Authentication = *who are you?* (valid token) → `AddAuthentication().AddJwtBearer` ([Program.cs:81](../api/OneCover.Api/Program.cs#L81)) and `UseAuthentication` ([:131](../api/OneCover.Api/Program.cs#L131)). Authorization = *are you allowed?* → `[Authorize]` / `[Authorize(Roles = "Admin")]` on controllers and `UseAuthorization` ([:132](../api/OneCover.Api/Program.cs#L132)).
</details>

**C5.** How do you guarantee a member can only see their own claims?

<details><summary>Show answer</summary>

The user id **always comes from the JWT**, never from the URL or request body. `ICurrentUser.UserId` reads it from the token's claims ([CurrentUser.cs:27](../api/OneCover.Api/Services/CurrentUser.cs#L27)). Services filter by it: `GetMineAsync` uses `currentUser.UserId` ([ClaimService.cs:91](../api/OneCover.Api/Services/ClaimService.cs#L91)), and `GetByIdAsync` rejects a claim that isn't theirs unless they're an admin ([:101](../api/OneCover.Api/Services/ClaimService.cs#L101)). `/api/me` has no id in the URL on purpose ([MeController.cs](../api/OneCover.Api/Controllers/MeController.cs#L16)).
</details>

**C6.** Why does a member asking for someone else's claim get a **404**, not a 403?

<details><summary>Show answer</summary>

A 403 would confirm "that claim exists, it's just not yours", which lets someone probe for valid ids. "Doesn't exist" and "not yours" look identical from outside ([ClaimService.cs:99-104](../api/OneCover.Api/Services/ClaimService.cs#L99)). Same trick for enrollments at [:37-41](../api/OneCover.Api/Services/ClaimService.cs#L37).
</details>

**C7.** How are passwords stored?

<details><summary>Show answer</summary>

Hashed with ASP.NET Core's `PasswordHasher<User>`: one-way and salted (same password → different hash each time). Only the hash is saved ([AuthService.cs:73](../api/OneCover.Api/Services/AuthService.cs#L73)); login verifies with `VerifyHashedPassword` ([:93](../api/OneCover.Api/Services/AuthService.cs#L93)).
</details>

**C8.** Why does login return the same message for "no such email" and "wrong password"?

<details><summary>Show answer</summary>

So an attacker can't use the login form to discover which emails have accounts ([AuthService.cs:89-96](../api/OneCover.Api/Services/AuthService.cs#L89)). The `||` also short-circuits: if `user` is null the password check never runs.
</details>

**C9.** Can someone register themselves as an Admin? Why not?

<details><summary>Show answer</summary>

No. `RegisterAsync` hard-codes `Role = Role.Member` ([AuthService.cs:62](../api/OneCover.Api/Services/AuthService.cs#L62)); `RegistrationRequest` has no role field at all ([RegistrationRequest.cs](../api/OneCover.Api/Dtos/RegistrationRequest.cs#L16)). Role is never taken from the body.
</details>

**C10.** How is the SSN handled?

<details><summary>Show answer</summary>

Only the **last 4 digits** are ever stored (`User.SsnLast4`, fixed-length 4 column [AppDbContext.cs:62](../api/OneCover.Api/Data/AppDbContext.cs#L62)); the request is validated to exactly 4 digits ([RegistrationRequest.cs:40](../api/OneCover.Api/Dtos/RegistrationRequest.cs#L40)). In the UI it's displayed masked through `MaskSsnPipe` → `***-**-1234` ([mask-ssn.pipe.ts](../web/src/app/shared/mask-ssn.pipe.ts#L12), used at [profile.ts:37](../web/src/app/features/member/profile.ts#L37)).
</details>

**C11.** What does the Angular `authInterceptor` do, and what are its safety rules?

<details><summary>Show answer</summary>

On every HTTP request it adds `Authorization: Bearer <token>` ([auth.interceptor.ts:18-27](../web/src/app/core/auth.interceptor.ts#L18)) by *cloning* the request (requests are immutable). Rules: only send the token to **our** API (`API_URL`), never to other hosts, and not on `/auth/` login/register calls. If a request that carried a token comes back **401**, it logs the user out ([:133](../web/src/app/core/auth.interceptor.ts#L133)). It's the front-end counterpart to middleware.
</details>

**C12.** Is the Angular `roleGuard` a security feature? What is?

<details><summary>Show answer</summary>

No, it's UX: it keeps people out of pages they can't use ([role.guard.ts:17](../web/src/app/core/role.guard.ts#L17)). Anyone can edit front-end code in their browser. The real gatekeeper is the API's `[Authorize(Roles=…)]` plus the ownership checks in services. The guard also checks token **expiry** at navigation time (`hasValidSession`, [auth.service.ts:59](../web/src/app/core/auth.service.ts#L59)).
</details>

**C13.** Where does the Angular app get the user's role from, and why isn't it stored separately?

<details><summary>Show answer</summary>

It's decoded **from the token** ([jwt.ts](../web/src/app/shared/jwt.ts) `roleFromToken` `:39`), then exposed as a `computed` signal ([auth.service.ts:36](../web/src/app/core/auth.service.ts#L36)). If role were saved separately in localStorage, someone could edit it to "Admin" and see admin screens. With the token as the single source of truth that can't be faked (and the API would reject their token anyway). A leftover `role` key from older versions is cleaned up ([auth.service.ts:24](../web/src/app/core/auth.service.ts#L24)).
</details>

**C14.** Where is the token stored, and what's the trade-off?

<details><summary>Show answer</summary>

`localStorage` ([auth.service.ts:23, 84](../web/src/app/core/auth.service.ts#L23)). Simple, fine for a training project, but readable by any script on the page (XSS risk). Production apps often prefer **httpOnly cookies**. That's an honest talking point: say you knew the trade-off and picked the simple option on purpose.
</details>

---

## D. Controllers, DTOs, validation, and errors

**D1.** What does `[ApiController]` give you?

<details><summary>Show answer</summary>

Automatic model validation (a bad DTO gets a 400 *before* your action runs), automatic reading of the JSON body into the parameter, and ProblemDetails-shaped errors. Example: [AuthController.cs:18](../api/OneCover.Api/Controllers/AuthController.cs#L18).
</details>

**D2.** How does attribute routing build the URL `PUT /api/admin/claims/5/status`?

<details><summary>Show answer</summary>

`[Route("api/admin/claims")]` on the class ([AdminClaimsController.cs:18](../api/OneCover.Api/Controllers/AdminClaimsController.cs#L18)) + `[HttpPut("{id:int}/status")]` on the action ([:33](../api/OneCover.Api/Controllers/AdminClaimsController.cs#L33)). `{id:int}` is a route constraint: the segment must be an integer, and it binds to the `int id` parameter. The body binds to `UpdateClaimStatusRequest`.
</details>

**D3.** Why are your controllers so short?

<details><summary>Show answer</summary>

Controllers are HTTP-only (separation of concerns): bind the request, call a service, pick the status code. Business logic belongs in services so it can be tested without HTTP. Most actions are a single expression, e.g. [AdminClaimsController.cs:33-34](../api/OneCover.Api/Controllers/AdminClaimsController.cs#L33).
</details>

**D4.** Which status codes does your API return, and where do the 201 and the 404 come from?

<details><summary>Show answer</summary>

- **200** normal reads/updates (`Ok(...)`).
- **201 Created** for new resources, with a `Location` header: `CreatedAtAction` in [ClaimsController.cs:34-38](../api/OneCover.Api/Controllers/ClaimsController.cs#L34) and [PlansController.cs:38](../api/OneCover.Api/Controllers/PlansController.cs#L38).
- **204** for retiring a plan ([PlansController.cs:54](../api/OneCover.Api/Controllers/PlansController.cs#L54)).
- **400** validation or business rule broken. **401** bad login/token. **403** wrong role. **404** not found / not yours. **500** unexpected.
- 404/400/401 come from custom exceptions mapped in [GlobalExceptionHandler.cs:36-44](../api/OneCover.Api/Errors/GlobalExceptionHandler.cs#L36).
</details>

**D5.** Describe your error-handling approach.

<details><summary>Show answer</summary>

**One approach for the whole API:** services and entities *throw* typed exceptions (`NotFoundException`, `BusinessRuleException`, `UnauthorizedException`, in [Errors/](../api/OneCover.Api/Errors/)). One `GlobalExceptionHandler` ([GlobalExceptionHandler.cs:26](../api/OneCover.Api/Errors/GlobalExceptionHandler.cs#L26)) catches anything that escapes, maps the type to a status code, and writes a ProblemDetails JSON body. Expected errors show their message; **unexpected** errors are logged with the stack trace server-side and the client only gets "An unexpected error occurred." (no leaking internals, [:30-32, 54](../api/OneCover.Api/Errors/GlobalExceptionHandler.cs#L30)). It's registered at [Program.cs:41-43](../api/OneCover.Api/Program.cs#L41) and plugged in at [:108](../api/OneCover.Api/Program.cs#L108).
</details>

**D6.** Why do the request DTOs use `int?` / `DateOnly?` / `decimal?` with `[Required]`?

<details><summary>Show answer</summary>

A plain `int` silently becomes `0` when the client omits it, so `[Required]` would never fire. Making it nullable lets `[Required]` catch "missing" and return a 400 instead of quietly using 0. The service then unwraps with `!.Value` knowing validation already ran (e.g. [ClaimService.cs:37, 62](../api/OneCover.Api/Services/ClaimService.cs#L37); [SubmitClaimRequest.cs](../api/OneCover.Api/Dtos/SubmitClaimRequest.cs#L16)).
</details>

**D7.** What's `[EnumDataType]` doing on `UpdateClaimStatusRequest`?

<details><summary>Show answer</summary>

JSON can send an enum as a bare number, and `[Required]` alone would accept `99`. `[EnumDataType(typeof(ClaimStatus))]` makes it reject any value that isn't a real `ClaimStatus` ([UpdateClaimStatusRequest.cs:17](../api/OneCover.Api/Dtos/UpdateClaimStatusRequest.cs#L17)).
</details>

**D8.** What's a `record`, and why use it for DTOs?

<details><summary>Show answer</summary>

A concise immutable-by-default type with value-based equality, ideal for data carriers. `public record ClaimDto(...)` ([ClaimDto.cs:14](../api/OneCover.Api/Dtos/ClaimDto.cs#L14)) declares properties in one line. Request DTOs use `init` properties so the binder can set them but nothing can change them afterward.
</details>

**D9.** Why do the mapping methods use **named arguments**?

<details><summary>Show answer</summary>

`new ClaimDto(Id: claim.Id, …)` instead of positional. Two values of the same type (two `decimal`s, two `int`s) can be swapped silently with positional arguments and still compile. Named arguments make a swap obvious. Mappings are extension methods like `ToDto(this BenefitClaim claim)` ([ClaimMappings.cs:16](../api/OneCover.Api/Dtos/ClaimMappings.cs#L16)).
</details>

**D10.** `GET /api/admin/claims` takes `status`, `page`, `pageSize`. Where do they come from, and what stops someone asking for a million rows?

<details><summary>Show answer</summary>

`[FromQuery]` parameters with defaults ([AdminClaimsController.cs:24-28](../api/OneCover.Api/Controllers/AdminClaimsController.cs#L24)): `?status=InReview&page=2&pageSize=20`. The service clamps them: page ≥ 1 and pageSize between 1 and `MaxPageSize = 100` ([ClaimService.cs:29, 114-115](../api/OneCover.Api/Services/ClaimService.cs#L29)). Paging happens **in SQL** (`Skip`/`Take`), not in memory ([ClaimRepository.cs:44-65](../api/OneCover.Api/Repositories/ClaimRepository.cs#L44)).
</details>

---

## E. Services, entities, and OOP

**E1.** Show me **encapsulation** in your project.

<details><summary>Show answer</summary>

`BenefitClaim` ([BenefitClaim.cs](../api/OneCover.Api/Models/BenefitClaim.cs#L18)): `Status` has a **private setter** `:38`; the history list is **private** and exposed as `IReadOnlyCollection` `:49-52`; the constructor is **private** `:56`. So the *only* ways to create or change a claim are `BenefitClaim.Submit` (`:65`) and `MoveTo` (`:121`), and both enforce the rules. Nobody can write `claim.Status = Approved` from a service. `Payment` does the same with `MarkPaid` / `MarkFailed` ([Payment.cs:78, 90](../api/OneCover.Api/Models/Payment.cs#L78)).
</details>

**E2.** What's the claim workflow, and where does it live?

<details><summary>Show answer</summary>

A state machine: Submitted → InReview → (InfoRequested ↔ InReview) / Approved / Denied → Closed. The allowed-moves table is a dictionary in [ClaimWorkflow.cs:21-29](../api/OneCover.Api/Models/ClaimWorkflow.cs#L21); `CanMove` ([:34](../api/OneCover.Api/Models/ClaimWorkflow.cs#L34)) answers "is this move legal?" and `MoveTo` calls it ([BenefitClaim.cs:124](../api/OneCover.Api/Models/BenefitClaim.cs#L124)) and throws `BusinessRuleException` otherwise. Putting it in one class means the rules aren't scattered through the service and controller.
</details>

**E3.** What does `BenefitClaim.MoveTo` do, in order, and why that order?

<details><summary>Show answer</summary>

([BenefitClaim.cs:121-141](../api/OneCover.Api/Models/BenefitClaim.cs#L121)) 1) Ask the workflow if the move is legal, throw if not (so an invalid move changes nothing). 2) Add a history row *recording the old status* **before** 3) changing `Status`, because after the change the old value is gone.
</details>

**E4.** Why is `BenefitClaim.Submit` a static factory instead of a public constructor?

<details><summary>Show answer</summary>

So an invalid claim can't exist: the constructor is private, and `Submit` is the only door in, validating the dates (end ≥ start, not before coverage began, ≤ 1 year) and creating the first history row ([BenefitClaim.cs:65-113](../api/OneCover.Api/Models/BenefitClaim.cs#L65)). "The claim protects itself", whoever calls it (Angular checks the same rules too, but this is the last line of defense). EF Core can still use the private constructor when loading from the DB.
</details>

**E5.** Walk through what `ClaimService.UpdateStatusAsync` does when an admin approves a claim.

<details><summary>Show answer</summary>

([ClaimService.cs:127-150](../api/OneCover.Api/Services/ClaimService.cs#L127)) Load the claim (404 if missing) → `claim.MoveTo(...)` (workflow check + history row + status change) → if now **Approved** and `TotalBenefit > 0`, create `Payment.PendingBenefit(...)` ([:138-141](../api/OneCover.Api/Services/ClaimService.cs#L138)) → **one** `SaveChangesAsync` so the status UPDATE, the history INSERT, and the payment INSERT succeed or fail together (one transaction) → reload for the response DTO. The service never touches `claim.Status` itself; it just coordinates.
</details>

**E6.** How is the weekly and total benefit calculated?

<details><summary>Show answer</summary>

[BenefitCalculator.cs](../api/OneCover.Api/Services/BenefitCalculator.cs): **weekly** = annual salary ÷ 52 × benefit percent, rounded to cents, capped at the plan's weekly maximum ([:16](../api/OneCover.Api/Services/BenefitCalculator.cs#L16)). **Total** = weekly ÷ 7 × payable days, where payable days = (days from start to end, counting both ends) − the elimination (waiting) period, never below 0 ([:25](../api/OneCover.Api/Services/BenefitCalculator.cs#L25)). Example in the file: $700/week, Oct 1–14 (14 days), 7-day wait → 7 payable days → $700.
</details>

**E7.** What's the FMLA eligibility rule and where is it used?

<details><summary>Show answer</summary>

Eligible if the member has worked at least 12 months **and** at least 1,250 hours in the last 12 months ([EligibilityService.cs:15](../api/OneCover.Api/Services/EligibilityService.cs#L15)). It's a singleton, uses the injected `TimeProvider`, and is called by `ProfileService` ([ProfileService.cs:35](../api/OneCover.Api/Services/ProfileService.cs#L35)) so `/api/me` returns `isFmlaEligible`.
</details>

**E8.** Why is `ClaimService` given six dependencies — isn't that too many?

<details><summary>Show answer</summary>

It coordinates across several aggregates in one use case (claims, enrollments, users for salary, payments for the benefit payment, the calculator, current user, clock). Each dependency is an interface, so each is replaceable in a test. It's a talking point: the service stays thin because the rules live in `BenefitClaim`, `ClaimWorkflow`, `Payment`, and `BenefitCalculator`. If it kept growing, splitting member vs admin claim services would be the next step.
</details>

**E9.** Show me **abstraction** and **polymorphism** in the project.

<details><summary>Show answer</summary>

**Abstraction:** controllers and services depend on interfaces (`IClaimService`, `IClaimRepository`, …), not concrete classes, so they know *what* not *how* ([ClaimsController.cs:18](../api/OneCover.Api/Controllers/ClaimsController.cs#L18)). **Polymorphism:** the same interface can have different implementations at runtime (real vs fake in tests); `AppDbContext` *overrides* `ConfigureConventions` and `OnModelCreating` and EF calls our versions ([AppDbContext.cs:39, 49](../api/OneCover.Api/Data/AppDbContext.cs#L39)). **Inheritance:** `AppDbContext : DbContext`, controllers `: ControllerBase`, custom exceptions `: Exception`.
</details>

**E10.** What are the premium and benefit payments, and how do they get created?

<details><summary>Show answer</summary>

A `Payment` is either a **Premium** (member pays) linked to an enrollment, or a **Benefit** (member receives) linked to a claim. Premium: created with the enrollment in `EnrollAsync`, due the 1st of next month ([EnrollmentService.cs:55-59](../api/OneCover.Api/Services/EnrollmentService.cs#L55)) via `Payment.PendingPremium` ([Payment.cs:54](../api/OneCover.Api/Models/Payment.cs#L54)). Benefit: created when a claim is approved via `Payment.PendingBenefit` ([Payment.cs:66](../api/OneCover.Api/Models/Payment.cs#L66)). Admin then marks them Paid or Failed ([PaymentService.cs:33](../api/OneCover.Api/Services/PaymentService.cs#L33)); only a Pending payment can change status ([Payment.cs:80, 92](../api/OneCover.Api/Models/Payment.cs#L80)).
</details>

**E11.** What's "soft delete", and where do you use it?

<details><summary>Show answer</summary>

Instead of deleting the row, flip a flag. "Retiring" a plan sets `IsActive = false` ([PlanService.cs:65-71](../api/OneCover.Api/Services/PlanService.cs#L65)); members only see active plans ([PlanRepository.cs:28](../api/OneCover.Api/Repositories/PlanRepository.cs#L28)), but existing enrollments still point to the row. Backed up at the DB level: all foreign keys use `Restrict`, so nothing can be cascade-deleted ([AppDbContext.cs:91-92](../api/OneCover.Api/Data/AppDbContext.cs#L91)).
</details>

---

## F. EF Core and SQL Server

**F1.** What is `AppDbContext`, and what is a `DbSet`?

<details><summary>Show answer</summary>

The DbContext is EF Core's working session with the database: it knows the tables, runs queries, tracks changes, and saves them ([AppDbContext.cs:21](../api/OneCover.Api/Data/AppDbContext.cs#L21)). Each `DbSet<T>` is a handle on one table (`Users`, `Plans`, `Claims`, `Enrollments`, `ClaimStatusHistories`, `Payments`, [:25-35](../api/OneCover.Api/Data/AppDbContext.cs#L25)). Only repositories use it.
</details>

**F2.** What does "code first" mean, and how do tables get created?

<details><summary>Show answer</summary>

The C# classes (entities + `AppDbContext`) are the source of truth; **migrations** generate the SQL to build/alter the tables from them ([Data/Migrations/](../api/OneCover.Api/Data/Migrations/)). Three so far: InitialCreate, UniqueActiveEnrollment, PaymentLinkCheckAndHistoryTrigger. Applied with `dotnet ef database update`.
</details>

**F3.** What's the difference between `Add()` and `SaveChangesAsync()`?

<details><summary>Show answer</summary>

`Add` only *marks* an entity for insertion in the change tracker; nothing hits the database. `SaveChangesAsync` sends all pending changes (inserts, updates) to SQL Server in **one transaction**. That's why in `EnrollAsync` I add the enrollment and its premium payment and save once ([EnrollmentService.cs:58-63](../api/OneCover.Api/Services/EnrollmentService.cs#L58)).
</details>

**F4.** `PlanService.UpdateAsync` never calls an "update" method. How does the change get saved?

<details><summary>Show answer</summary>

The **change tracker**. `GetByIdAsync` loads the plan *tracked*, EF remembers the original values, you modify properties, and `SaveChangesAsync` compares and sends an UPDATE for only the changed columns ([PlanService.cs:46-60](../api/OneCover.Api/Services/PlanService.cs#L46)). That's why `PlanRepository.GetByIdAsync` must **not** use `AsNoTracking` ([PlanRepository.cs:36-38](../api/OneCover.Api/Repositories/PlanRepository.cs#L36)).
</details>

**F5.** When do you use `AsNoTracking()`, and why?

<details><summary>Show answer</summary>

For **read-only** queries (lists shown to the user): it skips change-tracking bookkeeping, so it's faster and uses less memory. Examples: [PlanRepository.cs:29](../api/OneCover.Api/Repositories/PlanRepository.cs#L29), [ClaimRepository.cs:29](../api/OneCover.Api/Repositories/ClaimRepository.cs#L29). Not used where the entity will be modified and saved.
</details>

**F6.** What's eager loading, and show me `Include` / `ThenInclude`.

<details><summary>Show answer</summary>

Loading related data in the same query (a SQL JOIN) so the DTO mapping has everything and there are no extra queries later. [ClaimRepository.cs:23-26](../api/OneCover.Api/Repositories/ClaimRepository.cs#L23): claim → `User`; claim → `Enrollment` → `Plan` (`ThenInclude`); claim → `History` → `ChangedByUser`. It's written once in `ClaimsWithDetails()` and reused by every read (DRY).
</details>

**F7.** How does paging work in `ClaimRepository.GetPageAsync`?

<details><summary>Show answer</summary>

[ClaimRepository.cs:44-65](../api/OneCover.Api/Repositories/ClaimRepository.cs#L44): optionally filter by status, `CountAsync()` for the total, then `OrderByDescending(SubmittedAt).ThenBy(Id)` (an order is **required** or page 3 could contain different rows each time; the tiebreaker keeps it stable), `Skip((page-1)*pageSize).Take(pageSize)`, and return a **tuple** `(Items, TotalCount)`. The service wraps it in `PagedResult<ClaimDto>`.
</details>

**F8.** How does the dashboard count claims by status?

<details><summary>Show answer</summary>

`GroupBy(c => c.Status)` with a `Count()` in SQL ([ClaimRepository.cs:67-70](../api/OneCover.Api/Repositories/ClaimRepository.cs#L67)); the service fills in `0` for statuses with no claims using `Enum.GetValues<ClaimStatus>()` ([ClaimService.cs:152-161](../api/OneCover.Api/Services/ClaimService.cs#L152)).
</details>

**F9.** What database rules exist *beyond* your C# code, and why bother?

<details><summary>Show answer</summary>

"Defense in depth": the DB enforces rules even if code is bypassed (SSMS, a script, a bug).
- **Unique index on `Users.Email`** ([AppDbContext.cs:60](../api/OneCover.Api/Data/AppDbContext.cs#L60)).
- **Filtered unique index**: one *Active* enrollment per user per plan ([:71-74](../api/OneCover.Api/Data/AppDbContext.cs#L71)), so a double-click race can't create duplicates. The repo translates SQL error 2601/2627 into the friendly 400 ([EnrollmentRepository.cs:47-60](../api/OneCover.Api/Repositories/EnrollmentRepository.cs#L47)).
- **Check constraint**: a payment links to exactly one of enrollment/claim ([:79-81](../api/OneCover.Api/Data/AppDbContext.cs#L79)).
- **Append-only trigger** on claim history ([ClaimStatusHistory_AppendOnly.sql](../sql/ClaimStatusHistory_AppendOnly.sql), declared for EF at [:86](../api/OneCover.Api/Data/AppDbContext.cs#L86)).
</details>

**F10.** What are `ConfigureConventions` and `OnModelCreating`?

<details><summary>Show answer</summary>

Both are overrides in `AppDbContext`. `ConfigureConventions` sets rules **by type** once for every matching property: decimals are `(18,2)`, enums stored as strings, DateTimes converted to UTC ([:39-45](../api/OneCover.Api/Data/AppDbContext.cs#L39)). `OnModelCreating` sets rules for **specific** properties/relationships via the Fluent API: max lengths, indexes, constraints, delete behavior ([:49-93](../api/OneCover.Api/Data/AppDbContext.cs#L49)).
</details>

**F11.** What does `DbSeeder` do, and why does it create its own scope?

<details><summary>Show answer</summary>

Fills an empty DB with 5 plans and 3 demo users (admin, FMLA-eligible member, new hire), skipping any table that already has data so it's safe every startup ([DbSeeder.cs:28](../api/OneCover.Api/Data/DbSeeder.cs#L28), called at [Program.cs:115-118](../api/OneCover.Api/Program.cs#L115)). `AppDbContext` is scoped but at startup there's no request, so the seeder manually creates a scope (`CreateScope`) and asks the container for what it needs; `using var` disposes it afterward. Passwords are hashed in code (salted, so not via `HasData`).
</details>

---

## G. Angular fundamentals

**G1.** What's a standalone component? Show one.

<details><summary>Show answer</summary>

A component that declares what it uses in its own `imports` array (no NgModule). The root: [app.ts:14-23](../web/src/app/app.ts#L14) imports `RouterOutlet`, `RouterLink`, Material modules, and its template uses exactly those. Pages like [claims.ts](../web/src/app/features/member/claims.ts#L19) do the same.
</details>

**G2.** What's lazy loading, and where is it in your app?

<details><summary>Show answer</summary>

A page's code is only downloaded when someone navigates to it. `loadComponent: () => import('./features/auth/login')…` and `loadChildren` for the member and admin areas in [app.routes.ts:13-26](../web/src/app/app.routes.ts#L13). A member never downloads admin code.
</details>

**G3.** How are `/member/claims/new` and `/member/claims/:id` ordered, and why?

<details><summary>Show answer</summary>

`claims/new` is listed **before** `claims/:id` ([member.routes.ts:13-15](../web/src/app/features/member/member.routes.ts#L13)). Routes match top-down, first match wins; if `:id` came first, the URL `/claims/new` would match it with id = `"new"`.
</details>

**G4.** What is a signal? Which signals does `AuthService` have?

<details><summary>Show answer</summary>

A reactive value: anything reading it (in a template or `computed`) updates automatically when it changes. `token = signal(...)` ([auth.service.ts:33](../web/src/app/core/auth.service.ts#L33)); `role` and `isLoggedIn` are `computed` from it ([:36, 40](../web/src/app/core/auth.service.ts#L36)), so on login `token.set(...)` and the nav re-renders ([app.html](../web/src/app/app.html)). Read a signal by calling it: `auth.role()`.
</details>

**G5.** `signal` vs `computed` vs `effect`: when do you use each?

<details><summary>Show answer</summary>

- `signal`: state you set (`loading`, `claim`, `pageIndex`).
- `computed`: **derived**, read-only, recalculated when its inputs change (`role`, the `query` in [claims-queue.ts:128](../web/src/app/features/admin/claims-queue.ts#L128), `selectedEnrollment` in [file-claim.ts:185](../web/src/app/features/member/file-claim.ts#L185)).
- `effect`: run a *side effect* when signals change. Used once: re-validate the start date when the selected coverage changes ([file-claim.ts:203](../web/src/app/features/member/file-claim.ts#L203)).
</details>

**G6.** How does DI work in Angular here? Compare it to the API.

<details><summary>Show answer</summary>

`private http = inject(HttpClient);` ([plan.service.ts:16](../web/src/app/core/plan.service.ts#L16)) instead of constructor parameters (it's the same idea: ask the container, don't `new` it). `AuthService` is declared an app-wide singleton with `@Service()` ([auth.service.ts:27](../web/src/app/core/auth.service.ts#L27)), so every page and the interceptor share one login state. `inject()` also works inside functions such as the interceptor and guard, because Angular runs them in an injection context.
</details>

**G7.** What does an HTTP service method return, and when does the request actually go out?

<details><summary>Show answer</summary>

An **Observable** (e.g. [claim.service.ts:19](../web/src/app/core/claim.service.ts#L19)). Observables are **lazy**: nothing is sent until something calls `.subscribe()` (e.g. [claims.ts:74](../web/src/app/features/member/claims.ts#L74)). Subscribing a second time sends a second request.
</details>

**G8.** Why is `API_URL` a constant imported from one file, and how does it change between dev and prod?

<details><summary>Show answer</summary>

DRY: one place instead of the URL typed in every service ([api.ts:13](../web/src/app/core/api.ts#L13)). It reads `environment.apiUrl`. Angular's `fileReplacements` ([angular.json:54-57](../web/angular.json#L54)) swaps in [environment.development.ts](../web/src/environments/environment.development.ts) (localhost:5173) for `ng serve`; a production build uses [environment.ts](../web/src/environments/environment.ts) (the Azure API). No secrets go in these files; they're shipped to every browser.
</details>

**G9.** How does the admin dashboard link into the claims queue with a filter applied?

<details><summary>Show answer</summary>

Each status card is `routerLink="/admin/claims" [queryParams]="{ status: s.status }"` ([dashboard.ts:41](../web/src/app/features/admin/dashboard.ts#L41)), producing `/admin/claims?status=Submitted`. The queue reads it from `route.queryParamMap` ([claims-queue.ts:138](../web/src/app/features/admin/claims-queue.ts#L138)).
</details>

**G10.** The claims queue treats the **URL** as the source of truth for the filter. What does that buy you?

<details><summary>Show answer</summary>

The dropdown *navigates* to `?status=…` ([claims-queue.ts:178](../web/src/app/features/admin/claims-queue.ts#L178)) instead of setting the signal directly; the `queryParamMap` subscription then sets the signal ([:138-147](../web/src/app/features/admin/claims-queue.ts#L138)). So refresh, Back/Forward, bookmarks, and links from the dashboard all show what the URL says. Junk like `?status=junk` falls back to "all" (validate at the edge). `replaceUrl: true` avoids stuffing the Back history.
</details>

**G11.** What do the shared `StatusBadge` and `ClaimSummary` components show about reuse?

<details><summary>Show answer</summary>

Small presentational components used on several pages: `<app-status-badge>` ([status-badge.ts](../web/src/app/shared/status-badge.ts#L31)) turns a status into a colored label via `computed`; `<app-claim-summary>` ([claim-summary.ts](../web/src/app/shared/claim-summary.ts#L63)) is used on both the member's claim detail and the admin's claim review. Control flow in templates uses the new `@if` / `@for` blocks ([claim-summary.ts:27, 44](../web/src/app/shared/claim-summary.ts#L27)).
</details>

**G12.** What's a pipe? Show your custom ones.

<details><summary>Show answer</summary>

A template helper that transforms a value for display: `{{ p.ssnLast4 | maskSsn }}`. `MaskSsnPipe` ([mask-ssn.pipe.ts:12](../web/src/app/shared/mask-ssn.pipe.ts#L12)) implements `PipeTransform`; `StatusLabelPipe` ([status-label.ts:18](../web/src/app/shared/status-label.ts#L18)) makes `InReview` read "In review". Built-ins like `CurrencyPipe` and `DatePipe` are used in tables.
</details>

---

## H. Angular forms and RxJS

**H1.** Reactive forms: show a form and its validators.

<details><summary>Show answer</summary>

Forms built in the class with `NonNullableFormBuilder`, validators as functions, template bound with `[formGroup]`. Login: [login.ts:62-65](../web/src/app/features/auth/login.ts#L62) (`required`, `email`). Register: [register.ts:100-108](../web/src/app/features/auth/register.ts#L100). These mirror the API's DTO attributes: validate at the edges, on **both** sides.
</details>

**H2.** Why validate in Angular if the API validates too?

<details><summary>Show answer</summary>

Angular validation is **UX**: instant feedback, no wasted request. The API validation is **security/correctness**: a user can bypass the UI with Postman. Neither replaces the other. Example: date rules live in [validators.ts](../web/src/app/shared/validators.ts) *and* in `BenefitClaim.Submit` ([:70-87](../api/OneCover.Api/Models/BenefitClaim.cs#L70)).
</details>

**H3.** What's the difference between a control validator and a group validator? Show both.

<details><summary>Show answer</summary>

A **control** validator checks one field: `startOnOrAfter` ([validators.ts:38](../web/src/app/shared/validators.ts#L38)) is a *factory* returning a validator for the start date. A **group** validator checks fields *together*: `endAfterStart` ([:15](../web/src/app/shared/validators.ts#L15)) and `withinOneYear` ([:22](../web/src/app/shared/validators.ts#L22)) compare start and end dates; they're attached to the form group ([file-claim.ts:196](../web/src/app/features/member/file-claim.ts#L196)), and their errors show in the page, not under one field.
</details>

**H4.** In `FileClaimPage`, why is there an `effect()`?

<details><summary>Show answer</summary>

Validators only re-run when *their own control* changes. If the member goes back and picks a different coverage, the start date's minimum changes but the start date control didn't, so it wouldn't be re-checked. The `effect` watches `selectedEnrollment()` and calls `updateValueAndValidity()` on the start date ([file-claim.ts:203-206](../web/src/app/features/member/file-claim.ts#L203)).
</details>

**H5.** What does `toSignal` do, and where do you use it?

<details><summary>Show answer</summary>

Converts an Observable into a signal and unsubscribes automatically. `toSignal(coverageForm.controls.enrollmentId.valueChanges, …)` ([file-claim.ts:179](../web/src/app/features/member/file-claim.ts#L179)) lets `selectedEnrollment` be a `computed` instead of a manual subscription. Also in [plan-dialog.ts:128](../web/src/app/features/admin/plan-dialog.ts#L128). The reverse, `toObservable`, is in [claims-queue.ts:149](../web/src/app/features/admin/claims-queue.ts#L149).
</details>

**H6.** What does `switchMap` do, and where does it matter?

<details><summary>Show answer</summary>

It maps each new value to a new Observable and **cancels the previous one still in flight**. In the claims queue, changing the filter or page cancels the old request so a slow old response can't overwrite a newer one ([claims-queue.ts:149-170](../web/src/app/features/admin/claims-queue.ts#L149)). In claim detail/review it reacts to `:id` route changes ([claim-review.ts:98-120](../web/src/app/features/admin/claim-review.ts#L98)).
</details>

**H7.** What's `takeUntilDestroyed()` for?

<details><summary>Show answer</summary>

Automatically unsubscribes when the component is destroyed, so long-lived subscriptions (route params, form `valueChanges`) don't leak memory or keep running after you leave the page. e.g. [claim-detail.ts:65](../web/src/app/features/member/claim-detail.ts#L65), [claims-queue.ts](../web/src/app/features/admin/claims-queue.ts#L141). One-shot HTTP calls complete on their own and don't need it.
</details>

**H8.** What does `forkJoin` do in `PlansPage`?

<details><summary>Show answer</summary>

Runs two requests in parallel (the active plans and my enrollments) and emits once when **both** finish ([plans.ts:88-91](../web/src/app/features/member/plans.ts#L88)). The page needs both to show "Enroll" vs "Already enrolled" on each plan.
</details>

**H9.** In the interceptor, why `catchError` and then `throwError` again?

<details><summary>Show answer</summary>

The interceptor wants to *react* to a 401 (log out) but must still let the error continue to the page so it can show a message ([auth.interceptor.ts:129-137](../web/src/app/core/auth.interceptor.ts#L129)). `catchError` intercepts it; re-throwing with `throwError(() => err)` passes it on.
</details>

**H10.** How does the UI show the API's error message?

<details><summary>Show answer</summary>

`errorMessage(err, fallback)` ([problem-details.ts:21](../web/src/app/shared/problem-details.ts#L21)) reads the ProblemDetails `detail` (our `BusinessRuleException` text), else the first field validation error, else the title, else a fallback. Status `0` means the server couldn't be reached: "Can't reach the server. Is the API running?". Pages show it in a Material snackbar, e.g. [file-claim.ts:239](../web/src/app/features/member/file-claim.ts#L239).
</details>

**H11.** How does `PlanDialog` serve both "add" and "edit"?

<details><summary>Show answer</summary>

It receives an optional plan via `MAT_DIALOG_DATA`; the form is pre-filled from it ([plan-dialog.ts:113-124](../web/src/app/features/admin/plan-dialog.ts#L113)), and one line picks the call: `this.plan ? planService.update(...) : planService.create(...)` ([:175](../web/src/app/features/admin/plan-dialog.ts#L175)). Disability-only fields (percent, weekly max, elimination days) are enabled/disabled when the type changes ([:133](../web/src/app/features/admin/plan-dialog.ts#L133)), mirroring the API's `Plan.IsDisability`.
</details>

---

## I. Tests

**I1.** What do you test, and what framework on each side?

<details><summary>Show answer</summary>

API: **xUnit** in [api/OneCover.Tests](../api/OneCover.Tests/): the business rules that live in plain classes: [BenefitCalculatorTests](../api/OneCover.Tests/BenefitCalculatorTests.cs), [BenefitClaimTests](../api/OneCover.Tests/BenefitClaimTests.cs), [ClaimWorkflowTests](../api/OneCover.Tests/ClaimWorkflowTests.cs), [EligibilityServiceTests](../api/OneCover.Tests/EligibilityServiceTests.cs), [PaymentTests](../api/OneCover.Tests/PaymentTests.cs), [PlanTests](../api/OneCover.Tests/PlanTests.cs). Web: **Vitest** (`*.spec.ts`): auth service, nav per role, validators, SSN pipe. Run with `dotnet test` and `ng test`.
</details>

**I2.** Why is it easy to unit test `BenefitClaim` and `BenefitCalculator`?

<details><summary>Show answer</summary>

Because the rules are in plain classes with no database or HTTP, and dependencies (like the clock) are injected. No mocking framework needed: just call `Submit`/`MoveTo`/`WeeklyBenefit` and assert. That's a payoff of putting business rules inside the entities and a pure calculator, instead of in controllers.
</details>

**I3.** What's `[Theory]` vs `[Fact]`?

<details><summary>Show answer</summary>

`[Fact]` = one test. `[Theory]` + `[InlineData(...)]` = the same test method run once per data row. [ClaimWorkflowTests.cs:16-36](../api/OneCover.Tests/ClaimWorkflowTests.cs#L16) checks many from→to pairs; [PlanTests.cs:15](../api/OneCover.Tests/PlanTests.cs#L15) runs once per plan type.
</details>

**I4.** How do you test code that depends on "today"?

<details><summary>Show answer</summary>

The code takes a `TimeProvider`; tests pass a fake one set to a fixed date. [EligibilityServiceTests.cs](../api/OneCover.Tests/EligibilityServiceTests.cs) checks the exact boundaries (exactly 12 months, one day short, exactly 1,250 hours, one hour short).
</details>

**I5.** What would you test next if you had more time?

<details><summary>Show answer</summary>

Honest answer: service-level tests with fake repositories (ownership rules: a member can't read another member's claim → 404; approving creates a benefit payment; duplicate enrollment is rejected), and integration tests for controllers with a test database (auth, 401/403, validation 400). The entity and calculator tests already cover the most important rules.
</details>

---

## J. "What happens when…" walk-throughs

Say each one start to finish before opening the answer.

**J1.** A member clicks **Enroll** on a plan.

<details><summary>Show answer</summary>

1. [plans.ts:109](../web/src/app/features/member/plans.ts#L109) → `EnrollmentService.enroll(planId)` ([enrollment.service.ts:23](../web/src/app/core/enrollment.service.ts#L23)) → POST `/api/enrollments` with the interceptor adding the token.
2. [EnrollmentsController.cs:27](../api/OneCover.Api/Controllers/EnrollmentsController.cs#L27) (class-level `[Authorize(Roles="Member")]`) → [EnrollmentService.EnrollAsync :25](../api/OneCover.Api/Services/EnrollmentService.cs#L25).
3. User id from the JWT; plan must exist (404) and be active; no existing Active enrollment (400).
4. New `Enrollment` + `Payment.PendingPremium` (due the 1st of next month); both added, **one** save.
5. If two clicks race, the filtered unique index rejects the second and the repo turns it into a friendly 400 ([EnrollmentRepository.cs:47](../api/OneCover.Api/Repositories/EnrollmentRepository.cs#L47)).
6. Entity → `EnrollmentDto` → back to Angular → snackbar "Enrolled in …".
</details>

**J2.** An admin moves a claim from **Submitted** straight to **Approved**.

<details><summary>Show answer</summary>

Angular [claim-review.ts:132](../web/src/app/features/admin/claim-review.ts#L132) → PUT `/api/admin/claims/{id}/status` → [AdminClaimsController.cs:33](../api/OneCover.Api/Controllers/AdminClaimsController.cs#L33) → [ClaimService.UpdateStatusAsync :127](../api/OneCover.Api/Services/ClaimService.cs#L127) → `claim.MoveTo(Approved, …)`. `ClaimWorkflow.CanMove(Submitted, Approved)` is false → `BusinessRuleException("Can't move a claim from Submitted to Approved")` → `GlobalExceptionHandler` → **400** ProblemDetails → `errorMessage()` → snackbar. Nothing was saved: the exception fires before any change, and the payment code never runs.
</details>

**J3.** A user opens `/admin/payments` while logged in as a Member.

<details><summary>Show answer</summary>

The `roleGuard('Admin')` on the `admin` route ([app.routes.ts:22-26](../web/src/app/app.routes.ts#L22)) sees `auth.role() !== 'Admin'` and redirects to their own home route (`/member`) ([role.guard.ts:163-165](../web/src/app/core/role.guard.ts) / `homeRoute` [auth.service.ts:75](../web/src/app/core/auth.service.ts#L75)). The admin page's code isn't even downloaded (lazy loading). Even if they bypassed the guard and called the API directly, `[Authorize(Roles="Admin")]` returns **403**.
</details>

**J4.** The token expires while the tab is open and the user clicks a link.

<details><summary>Show answer</summary>

Navigating runs the guard; `hasValidSession()` sees the token's expiry has passed ([auth.service.ts:59-66](../web/src/app/core/auth.service.ts#L59)), clears the session, and the guard redirects to `/login`. If they'd instead triggered an API call with the expired token, the API's JWT validation (`ValidateLifetime`) returns 401 and the interceptor logs them out ([auth.interceptor.ts:133](../web/src/app/core/auth.interceptor.ts#L133)).
</details>

**J5.** A new user registers.

<details><summary>Show answer</summary>

[register.ts](../web/src/app/features/auth/register.ts#L100) form (validated) → `POST /api/auth/register` → [AuthService.RegisterAsync :47](../api/OneCover.Api/Services/AuthService.cs#L47): normalize email, reject duplicate (400), build `User` (role forced to Member, SSN last 4), hash the password, save, **then** create the token (the user id only exists after save, [:77](../api/OneCover.Api/Services/AuthService.cs#L77)) and return it, so they're logged in immediately and Angular routes them to `homeRoute()`.
</details>

**J6.** Describe the full path of a claim: filed → reviewed → paid.

<details><summary>Show answer</summary>

1. Member files ([ClaimService.SubmitAsync :33](../api/OneCover.Api/Services/ClaimService.cs#L33)): `Submitted`, benefit calculated, first history row.
2. Admin: Submitted → InReview → (InfoRequested ↔ InReview) → Approved/Denied → Closed, each move validated by `ClaimWorkflow` and logged in `ClaimStatusHistory`.
3. On **Approved** with money owed, a **Pending benefit Payment** is created in the same transaction ([ClaimService.cs:138-144](../api/OneCover.Api/Services/ClaimService.cs#L138)).
4. Admin marks the payment Paid/Failed on `/admin/payments` ([PaymentService.cs:33](../api/OneCover.Api/Services/PaymentService.cs#L33)); the member sees it on **Statements**.
</details>

---

## K. Debugging scenarios

Where would you look first?

**K1.** Angular shows "Can't reach the server. Is the API running?"

<details><summary>Show answer</summary>

HTTP status `0` means the request never got a response ([problem-details.ts:21](../web/src/app/shared/problem-details.ts#L21)): API not running, wrong `apiUrl` in [environment.development.ts](../web/src/environments/environment.development.ts) (should match the API's http port 5173), or a **CORS** block (check [Program.cs:99-101](../api/OneCover.Api/Program.cs#L99) and `Cors:Origins`). Also: start the API with the plain `http` profile because the https redirect breaks the CORS preflight.
</details>

**K2.** Every API call returns 401 right after login.

<details><summary>Show answer</summary>

Check that the interceptor is attaching the token ([auth.interceptor.ts:120-127](../web/src/app/core/auth.interceptor.ts#L120); `req.url` must start with `API_URL`), and that issuer/audience/key in the JWT settings ([Program.cs:81-91](../api/OneCover.Api/Program.cs#L81), [appsettings.json](../api/OneCover.Api/appsettings.json)) match what `CreateToken` used ([AuthService.cs:128-135](../api/OneCover.Api/Services/AuthService.cs#L128)). A different `Jwt:Key` between runs invalidates old tokens.
</details>

**K3.** A member sees "Claim 7 not found" for a claim that exists.

<details><summary>Show answer</summary>

It belongs to another user, and that is by design: not-yours and not-found are both 404 ([ClaimService.cs:101](../api/OneCover.Api/Services/ClaimService.cs#L101)). Log in as the owner (or an admin).
</details>

**K4.** The app crashes at startup with "Jwt:Key is not configured".

<details><summary>Show answer</summary>

The fail-fast check at [Program.cs:77-78](../api/OneCover.Api/Program.cs#L77). Set the secret: `dotnet user-secrets set "Jwt:Key" "<long random string>"` in `api/OneCover.Api` (or the `Jwt__Key` app setting on Azure).
</details>

**K5.** Filing a claim says "A claim can't start before the coverage began."

<details><summary>Show answer</summary>

That's `BenefitClaim.Submit`'s rule ([BenefitClaim.cs:76-79](../api/OneCover.Api/Models/BenefitClaim.cs#L76)): start date < `Enrollment.EnrolledOn`. The Angular `startOnOrAfter` validator ([validators.ts:38](../web/src/app/shared/validators.ts#L38)) should have caught it first; if it reached the API, it came from a bypass or a stale form.
</details>

**K6.** Enrolling twice quickly produces an error instead of two rows. Where is that protection?

<details><summary>Show answer</summary>

Three layers: service check ([EnrollmentService.cs:39](../api/OneCover.Api/Services/EnrollmentService.cs#L39)), the filtered unique index ([AppDbContext.cs:71-74](../api/OneCover.Api/Data/AppDbContext.cs#L71)), and the repository's duplicate-key catch ([EnrollmentRepository.cs:55-60](../api/OneCover.Api/Repositories/EnrollmentRepository.cs#L55)).
</details>

**K7.** The claims queue shows page 3 of "Denied" after switching filters, and it's empty.

<details><summary>Show answer</summary>

A new filter must reset to page 1; the code does `pageIndex.set(0)` when the status changes ([claims-queue.ts:143-146](../web/src/app/features/admin/claims-queue.ts#L143)). If that line is missing, page 3 of the new filter may not exist.
</details>

---

## L. Say-it-out-loud prompts

No right answers here: practice speaking for 30–60 seconds each, then check yourself against [CODE_MAP.md](CODE_MAP.md) and [PROJECT_TALKING_POINTS.md](../PROJECT_TALKING_POINTS.md).

1. "Give me a 60-second overview of OneCover."
2. "Walk me through your architecture, front to back."
3. "What design decision are you most proud of, and what was the alternative?" (Good candidates: rules inside `BenefitClaim`, defense in depth for duplicate enrollment, one global exception handler, ownership from the JWT.)
4. "What would you do differently or add if you had another week?"
5. "How did you handle security?"
6. "How would you deploy and configure this for different environments?" (appsettings layers, user-secrets, Azure app settings, Angular environment files.)
7. "Where did you choose the simple option on purpose?" (localStorage token, no refresh tokens, claims counted via a single service, no separate integration tests.)
8. "Explain one concept you had to learn for this project and how it shows up in the code."

---

## Score sheet

Copy this table into your notes and tick as you go.

| Section | Questions | ✅ | 🟡 | ❌ | Redo on |
|---|---|---|---|---|---|
| A. Big picture | 5 | | | | |
| B. Startup, DI, middleware | 9 | | | | |
| C. Auth and security | 14 | | | | |
| D. Controllers, DTOs, errors | 10 | | | | |
| E. Services, entities, OOP | 11 | | | | |
| F. EF Core and SQL Server | 11 | | | | |
| G. Angular fundamentals | 12 | | | | |
| H. Forms and RxJS | 11 | | | | |
| I. Tests | 5 | | | | |
| J. Walk-throughs | 6 | | | | |
| K. Debugging | 7 | | | | |

**Study order suggestion:** B → C → E → F (the API story), then G → H (the Angular story), then J (tie them together), then L out loud.
