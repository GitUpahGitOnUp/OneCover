# OneCover

Employee benefits enrollment and claims app (member + admin).

## Stack
ASP.NET Core Web API (.NET 10), EF Core, SQL Server, Angular + Angular Material, JWT, xUnit, Vitest.

## Layout
- `api/OneCover.Api` – controllers → services → repositories
- `api/OneCover.Tests` – xUnit
- `web/` – Angular app (see `web/CLAUDE.md`)

## Commands
- API: `cd api/OneCover.Api && dotnet run`
- Web: `cd web && ng serve` (http://localhost:4200)
- Tests: `dotnet test` · `ng test`

## Conventions
- Name the claim entity `BenefitClaim` (avoids a clash with `System.Security.Claims.Claim`).
- Return DTOs, never entities.
- Members only get their own data. Take the user id from the JWT, never from the URL or body.
- Store only the last 4 of the SSN.
