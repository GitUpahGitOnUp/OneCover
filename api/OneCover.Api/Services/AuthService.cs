// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     registers members + logs users in: hashes/verifies passwords, builds the signed JWT
// Layer:    Services (business logic). controller above it, repository below it
// Concepts: implements an INTERFACE (: IAuthService), DEPENDENCY INJECTION (3 injected deps),
//           ABSTRACTION (only knows IUserRepository, never EF Core), ENCAPSULATION (private CreateToken +
//           NormalizeEmail), async/await on DB calls, password hashing, JWT creation, throwing typed exceptions
// Related:  IAuthService, IUserRepository, User, Dtos/, BusinessRuleException + UnauthorizedException,
//           Program.cs (JWT settings it signs w/ are the same ones the middleware validates), CurrentUser (reads the claims)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Identity;
using OneCover.Api.Dtos;
using OneCover.Api.Models;
using OneCover.Api.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using OneCover.Api.Errors;
using OneCover.Api.Models.Enums;

namespace OneCover.Api.Services;

/// <summary>
/// Registers members and logs users in. Passwords are hashed, and successful logins
/// get a signed JWT that expires in 2 hours.
/// </summary>
// only runs at register + login. after that, the JWT middleware in Program.cs checks the token on every
// request by itself, AuthService isn't involved again until the next login.
//
// 3 things get injected by DI (primary constructor):
//   users  = the repository, all DB access goes through it (never AppDbContext directly)
//   config = reads Jwt:Key / Jwt:Issuer / Jwt:Audience (key from user-secrets, the rest from appsettings.json)
//   time   = the clock, injected instead of DateTime.UtcNow so tests can fake "now"
public class AuthService(IUserRepository users, IConfiguration config, TimeProvider time) : IAuthService
{
    // hashes + verifies passwords. one-way (can't be reversed) and salted (same password, different hash every time).
    // created directly, not injected, b/c it has no dependencies and nothing worth faking in tests
    private readonly PasswordHasher<User> _hasher = new();

    // trims spaces + lowercases, so " Member@OneCover.dev" and "member@onecover.dev" are the same account
    // static = doesn't use anything from the object (users, config, time), so it doesn't need one
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    // REGISTER: new member sign-up -> saves the user -> returns a token so they're logged in right away.
    // by the time this runs, [ApiController] has already checked the DTO's validation attributes,
    // so required fields are guaranteed to be there
    public async Task<AuthResponse> RegisterAsync(RegistrationRequest request)
    {
        var email = NormalizeEmail(request.Email);

        // 1. email has to be unique. (the unique index on Users.Email is the DB-level backup for this)
        if (await users.EmailExistsAsync(email))
        {
            throw new BusinessRuleException("An account with this email already exists.");
        }

        // 2. build the entity from the DTO. the DTO is what came in over HTTP, the entity is what gets saved.
        //    !.Value unwraps the nullable DTO props (DateOnly? -> DateOnly). safe b/c [Required] already ran
        var user = new User
        {
            Email = email,
            Role = Role.Member,     // always a Member, role is never taken from the request body
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            HireDate = request.HireDate!.Value,
            AnnualSalary = request.AnnualSalary!.Value,
            HoursWorkedLast12Months = request.HoursWorkedLast12Months!.Value,
            SsnLast4 = request.SsnLast4,
            CreatedAt = time.GetUtcNow().UtcDateTime
        };

        // 3. hash the password. only the hash gets stored, the real password is never saved anywhere
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        // 4. save. Add only marks it for insert, SaveChangesAsync actually sends the INSERT to SQL Server
        users.Add(user);
        await users.SaveChangesAsync(); // SQL Server assigns user.Id *here* so the token has to come after

        // 5. hand back the token + role (a DTO, never the User entity, which has the password hash on it)
        return new AuthResponse(CreateToken(user), user.Role.ToString());
    }

    // LOGIN: find the user -> check the password -> return a token
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // null if no account has this email
        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email));

        // same message for 'no email' or 'wrong password', so bad actors cannot fish for existing emails
        // VerifyHashedPassword hashes what they typed w/ the salt stored inside the saved hash, then compares.
        // || short-circuits: if user is null, the right side never runs (so no crash reading user.PasswordHash)
        // UnauthorizedException -> GlobalExceptionHandler turns it into a 401
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return new AuthResponse(CreateToken(user), user.Role.ToString());
    }

    // BUILDS THE JWT. a JWT has 3 parts separated by dots: header.payload.signature
    //   header    = which algorithm signed it (HMAC-SHA256)
    //   payload   = the claims + issuer, audience, and the valid-from / expires times
    //   signature = header + payload run through HMAC-SHA256 w/ our secret key
    // the payload is only encoded, NOT encrypted, so anyone can read it (try jwt.io).
    // that's why nothing secret (password, SSN) ever goes in a claim
    private string CreateToken(User user)
    {
        // claims = facts about the user baked into the token. CurrentUser reads these back on later requests
        // Claim here is System.Security.Claims.Claim, the reason our entity is named BenefitClaim
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),   // who: CurrentUser.UserId reads this
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())            // [Authorize(Roles = "Admin")] checks this
        };

        // ! is safe here: Program.cs already refuses to start if Jwt:Key is missing (fail fast)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var now = time.GetUtcNow().UtcDateTime;

        // issuer   = who made the token (OneCover.Api)
        // audience = which app it's meant for (OneCover.Web, the Angular front end), not the user
        // notBefore / expires = the window the token is valid in: from now until 2 hours from now
        // signingCredentials = our secret key + the algorithm. this is what produces the signature.
        // Program.cs checks all of these on every request: same issuer? same audience? not expired?
        // signature still matches? if someone edited the payload (say, Role -> Admin), the signature won't match
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: now.AddHours(2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        // turns the token obj into the compact "xxxxx.yyyyy.zzzzz" string the client stores
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
} 