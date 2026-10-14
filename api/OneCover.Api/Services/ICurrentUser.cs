namespace OneCover.Api.Services;

/// <summary>
/// Interface that reads in the logged-in user's id and role from the JWT on the curr. request. Services just ask for it via DI, so JWT-reading does
/// not get copied into every controller
/// </summary>
// an interface is the "what": it lists what any current-user class has to offer, w/o saying how.
// services depend on this, not on CurrentUser, so tests can hand them a fake user w/ any id
public interface ICurrentUser
{
    /// <summary>
    /// The logged-in user's id, from the JWT. Only works on [Authorize] endpoints.
    /// Always use this, never a user id from the URL or request body.
    /// </summary>
    int UserId { get; } // read only, no one needs to set userid / role

    /// <summary>True if the logged-in user has the Admin role.</summary>
    bool IsAdmin { get; }
}

