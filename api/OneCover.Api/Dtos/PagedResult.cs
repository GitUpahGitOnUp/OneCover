// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     one page of results + the info a table needs to show paging ("page 2 of 5, 87 claims")
// Layer:    Dtos, the API boundary
// Concepts: GENERICS: <T> is a placeholder type. PagedResult<ClaimDto> = a page of claims, PagedResult<PaymentDto>
//           would be a page of payments. one class, reused for any item type, still fully type-safe.
//           TotalPages is a computed property (=> ...), so it's never out of sync w/ TotalCount
// Related:  ClaimRepository.GetPageAsync (does the paging in SQL), ClaimService.GetPageAsync,
//           AdminClaimsController, Angular admin claims table w/ a paginator (Day 8)
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Dtos;

/// <summary>A single page of items, plus the totals a paginator needs.</summary>
/// <typeparam name="T">The item type, e.g. ClaimDto.</typeparam>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>How many pages there are in total (rounds up: 87 items / 20 per page = 5 pages).</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
