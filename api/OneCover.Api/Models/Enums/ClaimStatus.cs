namespace OneCover.Api.Models.Enums;


// Where a claim is in the review workflow. Which moves are allowed is decided by ClaimWorkflow.

public enum ClaimStatus
{
    Submitted,
    InReview,
    InfoRequested,
    Approved,
    Denied,
    Closed
}
