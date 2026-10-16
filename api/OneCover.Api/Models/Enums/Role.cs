// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     enum for what kind of user someone is
// Layer:    Models (domain), stored as text in SQL Server
// Concepts: role-based access. the role goes into the JWT as a claim, then [Authorize(Roles = "Admin")] checks it
// Related:  User.Role, AuthService (always assigns Member on register, adds the role claim to the token),
//           CurrentUser.IsAdmin
// ─────────────────────────────────────────────────────────────────────────────────────────
namespace OneCover.Api.Models.Enums;

public enum Role
{
    Member,
    Admin
}
