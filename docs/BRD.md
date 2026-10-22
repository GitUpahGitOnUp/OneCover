# OneCover: Business Requirements Document

**Version:** 1.0 · **Date:** October 22, 2026 · **Status:** Implemented

## 1. Purpose

OneCover lets employees enroll in workplace benefit plans and file disability leave claims online, and gives benefits administrators one place to review those claims and track the money involved. It replaces a paper or email process, where claims are hard to track and members can't see where their claim stands.

## 2. Users

| User | Needs |
|---|---|
| **Member** (employee) | See available coverage and enroll; file a claim when they need disability leave; know what they'll be paid and where their claim stands; see what they owe and what they've been paid. |
| **Administrator** (benefits staff) | Maintain the plan catalog; work through incoming claims in order; record decisions with a reason; record when payments go out or fail; see the overall workload at a glance. |

## 3. Scope

**In scope:** member self-registration and login; plan enrollment; disability leave claims (short-term and long-term); the claim review workflow with an audit history; premium and benefit payment records; an FMLA eligibility indicator; an admin dashboard.

**Out of scope (this release):** actual payment processing (payments are recorded, not sent); document uploads (e.g. doctor's notes); notifications by email or text; claims for non-disability plans (life, accident, dental); payroll or HR system integration; reactivating retired plans.

## 4. Business requirements

**Enrollment**
- **BR-1** Members can view all active plans with their monthly premium and, for disability plans, the benefit terms.
- **BR-2** Members can enroll in a plan. A member can't hold two active enrollments in the same plan.
- **BR-3** Enrolling creates the first premium bill, due on the 1st of the next month.

**Claims**
- **BR-4** Members can file a claim against their own active short-term (STD) or long-term (LTD) disability coverage, giving leave dates and a reason.
- **BR-5** The system calculates the weekly and total benefit when the claim is submitted, and shows the amount to the member immediately. Members can't enter or change amounts.
- **BR-6** Members can see each of their claims, its current status, and its full history.

**Claim review**
- **BR-7** Administrators see a queue of all claims, newest first, which they can filter by status.
- **BR-8** Administrators can move a claim to a new status and add a note. Only valid moves are allowed (see 5.3).
- **BR-9** Every status change is recorded permanently: from which status, to which status, who made the change, when, and the note. History can't be edited or deleted.
- **BR-10** Approving a claim with a benefit owed creates a pending benefit payment.

**Payments and plans**
- **BR-11** Members can see a statement of every premium and benefit payment, with its status.
- **BR-12** Administrators can mark a pending payment as Paid (with today's date) or Failed. A payment that is no longer pending can't be changed.
- **BR-13** Administrators can add and edit plans, and retire plans. Retired plans are hidden from members, but existing enrollments and history are kept. Retired plans can't be edited.
- **BR-14** Administrators see a dashboard with the number of claims in each status.

**Profile**
- **BR-15** Members can see their profile, including whether they're currently eligible for FMLA leave.

## 5. Business rules

**5.1 Disability benefit**
- Weekly benefit = annual salary ÷ 52 × the plan's benefit percentage, capped at the plan's weekly maximum.
- Leave days inside the plan's elimination (waiting) period aren't paid. Total benefit = the daily rate (weekly ÷ 7) × the payable days.
- The benefit is fixed at submission. Later changes to the plan don't change existing claims.
- *Example:* a $78,000 salary on a 60% plan with a 7-day waiting period earns $900/week; a 14-day leave pays 7 days, or $900.

**5.2 Claim validity:** the leave can't end before it starts, can't begin before the coverage started, and can't exceed one year in a single claim.

**5.3 Claim workflow**

| From | Allowed next status |
|---|---|
| Submitted | In Review |
| In Review | Info Requested, Approved, Denied |
| Info Requested | In Review |
| Approved | Closed |
| Denied | Closed |
| Closed | None (final) |

**5.4 FMLA eligibility:** at least 12 months since the hire date, and at least 1,250 hours worked in the last 12 months.

## 6. Security and privacy requirements

- **SR-1** Members can only see and act on their own data. A request for another member's record is answered as "not found".
- **SR-2** Only administrators can manage plans, review claims and update payments.
- **SR-3** Only the last four digits of a Social Security number are stored, and they're displayed masked (`***-**-1234`).
- **SR-4** Passwords are stored only as salted hashes. Secrets (such as the token signing key) are kept out of source code.
- **SR-5** Every rule above is enforced by the server, not just hidden in the user interface.

## 7. Assumptions and constraints

- Salary and hours worked come from the member's registration. In a real deployment they would come from HR or payroll.
- One currency (USD) and one time zone basis (timestamps stored in UTC, shown in the user's local time).
- Built as a training portfolio project on a fixed timeline (October 6–22, 2026). Scope was chosen to complete a polished core rather than a broad, partial one.

## 8. Success criteria

- A member can register, enroll, file a claim, and follow it through to approval and payment, entirely in the browser.
- An administrator can take a claim from Submitted to Closed, with every step recorded in its history.
- Invalid actions (an invalid status move, an out-of-range leave, another member's data) are refused with a clear message.
- The business rules are covered by automated tests.
