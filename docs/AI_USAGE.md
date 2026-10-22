# How AI was used on OneCover

I built OneCover with **Claude Code** (an AI coding assistant running in VS Code). It acted as a tutor, a pair programmer and a code reviewer. This document is an honest record of what it did, what I changed, and where it was wrong.

## Ground rules I set

I wrote standing instructions in `CLAUDE.md` (project conventions, checked in) and `CLAUDE.local.md` (my personal working rules). They changed as the project went on:

| When | Rule | Why |
|---|---|---|
| Start | **"I write the code. You advise."** AI explains, shows code in chat, and reviews; it never edits project files or runs commands that change the project. | I'm here to learn. Early on, the AI edited a file I hadn't asked it to touch, so I made the boundary explicit. |
| Start | Point out the course concepts (encapsulation, DI, async/await, observables…) whenever code uses them. | Every file should teach me something I can explain in an interview. |
| Early | Reviews must give **file + line number** for every fix. | Vague feedback ("check your mapping") wasted time. |
| Day 4 | DTOs are built with **named arguments**, never positional ones. | A mapping with a missing value shifted everything after it, and the compiler only noticed because the count was off. Named arguments make that kind of swap impossible. |
| Day 5 | **Explicit Postman steps** (folder, name, method, URL, body, auth) for any new request. | The AI listed checks against a request it had never told me to create. |
| Oct 17 | **Delivery mode:** complete code in chat for me to type, with explanations focused on what's new. | The deadline was 5 days out, and finishing mattered more than hints. |
| Oct 18 | AI may write code directly **only for patterns I had already hand-coded** (DTOs, repositories, controllers). New concepts stay mine. | It saves time on repetition without skipping the learning. |
| Always | Git (commit, push, merge) stays with me. Secrets never go in the repo or in chat. | I own what ships. |

## Who wrote what

| Area | Who |
|---|---|
| Data model, `DbContext`, migrations, seeding | Me, with guidance |
| Auth (JWT, password hashing, `ICurrentUser`, error handler) | Me, with guidance |
| `BenefitClaim.Submit`, `ClaimWorkflow`, `MoveTo` (the core domain rules) | Me |
| Repetitive layers: DTOs, mappings, repositories, controllers | AI drafted them once I had hand-coded the first of each, and I reviewed them |
| `BenefitCalculator`, FMLA eligibility, payment rules, admin endpoints, xUnit tests (Day 5 crunch) | AI drafted and commented them, then walked me through them. I reviewed and tested them. |
| Angular pages and services | I typed them from AI-provided code, one step at a time, and tested each in the browser |
| Code review fixes on later PRs | AI applied them at my request, and I re-tested |
| README, this document, BRD | AI drafted them from the code and my dev journal; I edited them |

## Representative prompts

| Prompt (paraphrased) | What AI produced | What I did with it |
|---|---|---|
| "Explain navigation properties / the DbContext builder / indexes" | Plain-language explanations tied to my own entities | Wrote my own comments in the code, in my words |
| "Review my DbSeeder / AuthService / mapping" | Line-numbered findings: typos, wrong enums, a wrong auto-imported `using` (Microsoft's own `LoginRequest` type) | Fixed them myself |
| "Generate the Enrollment repository + service" | Code following my Plans pattern | Reviewed it, checking in particular that the enrollment and its first premium save in one `SaveChangesAsync` (one transaction) |
| "Claims can be filed against Dental. That's wrong." | An API rule (STD/LTD only) plus the matching UI filter | I reviewed the API change and typed the Angular change myself |
| "How hard would it be to click a dashboard card and see those claims?" | A trade-off answer (about 10 lines, one query parameter) and the code | Built it. A later review asked for URL syncing, which I added. |
| `/code-review` on each PR | A ranked list of findings | Triaged them: fixed the real ones, explained the ones that didn't apply |

## Where the AI was wrong (and how it was caught)

- **A change that broke error consistency (Day 3).** The AI suggested declaring the API's response format in a way that changed the content type of some errors. The automated code review caught it, and I reverted it.
- **A wrong assumption about the JWT role claim.** The code expected a claim named `role`. Decoding a real token showed ASP.NET Core writes the long URI (`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`). We now accept both, with a comment explaining why.
- **Invented test data (Day 8).** A test step said the STD weekly maximum was $1,000. The seed data says $1,500. The AI had made the number up instead of checking. The test itself was still valid, but I now verify numbers against the code.
- **Checks against a request that didn't exist (Day 5).** The AI listed Postman checks for a "Me" request it had never told me to create. This led to the explicit-steps rule.
- **Confusing instructions.** Test steps for a new feature were numbered in a way that ran into the next build step, so I tried a dialog test as a member (who never sees that dialog). Test steps now name the role first.
- **Bugs in AI-written code, caught by later reviews:** timestamps shown in the wrong time zone (the database returned dates without a UTC marker), a load failure shown as "you have no coverage", a dialog that could be closed mid-save (losing the result), and a plan form that wiped benefit terms when the type was toggled. All are fixed. Each one is a reminder that "it compiles and the happy path works" isn't the same as "it's right."
- **Azure deploy zip (twice).** The AI's first two zip fixes still wrote Windows `\` paths, which Linux App Service rejected. The third was tested locally before I ran it.
- **Verified instead of trusted.** Angular 22's new `@Service()` decorator looked like it could be a hallucination, so we checked that it exists in `@angular/core` before using it.

## What I caught that the AI didn't

- **Claims against non-disability plans.** Clicking through the claim form, I noticed a member could file a "leave claim" against Dental coverage. No test or review had flagged it, because it was a missing *business* rule, not a code defect. The API now rejects it (`Plan.IsDisability`, unit-tested) and the UI only offers disability coverage.
- **Testing the rule, not just the screen.** I opened the same claim in two tabs and made a move that had become stale. The API refused it with the workflow's message, which shows the server enforces the rules even when the UI is out of date.

## AI code review findings, by PR

| PR | Real findings fixed |
|---|---|
| Auth (first PR) | Added `UseStatusCodePages` so empty 401/403 responses also return ProblemDetails. Reverted a change that made error content types inconsistent. |
| Plans / Enrollments / Claims APIs | `[Range(0, 1)]` on a decimal silently rounds (1.4 → 1 passed), so it was switched to the decimal-aware `Range`. Added `[EnumDataType]` to reject made-up enum numbers. Added `AsNoTracking()` for read-only lists. |
| Business logic | No upper bound on claim length (a 9999-12-31 end date produced a $376M benefit), so claims are now capped at one year. Future "paid" dates are rejected. A dictionary lookup that could crash with a 500 was fixed. The history trigger was moved into a migration. |
| Angular foundation | Read the role from the token instead of a separate stored value (which could be edited), check token expiry, and only attach the token to our own API's requests. |
| Member pages | UTC timestamps shifted by the browser's time zone, an error state shown as "no coverage", a client-side start-date check, a route parameter ignored when the page is reused, a missing test for the disability rule, duplicated styles. |
| Admin pages | A dialog closable mid-save, concurrent saves re-enabling each other's buttons, the URL and filter disagreeing, a form value carrying over between claims, hidden fields wiping plan terms, retired plans still editable. |

## What I learned about working with AI

- **Context is limited and gets summarized.** In long sessions, earlier conversation is compressed. Anything that has to survive (rules, decisions) belongs in a file the AI re-reads every session (`CLAUDE.md`, `CLAUDE.local.md`), not in chat. My dev journal served as the project's memory between sessions.
- **Specific prompts get specific answers.** "Review `PlanMappings.cs` and give line numbers" beats "does this look right?" Naming the file, the goal and the constraint (for example "don't add features that aren't in the plan") cut down on wasted output.
- **Tokens cost time and money.** Bundling the explanatory comments into the generated code, instead of a second pass, saved a whole round trip per file.
- **AI is confident even when it's wrong.** The fix is verification: build it, run the tests, decode the token, check the seed data. I never accepted a claim about my own code without looking.
- **Review is input, not orders.** Not every finding applies; on my first PR, some targeted temporary test endpoints that were about to be deleted. Saying why I skipped a finding was part of the job.
