// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     CONTROLLER = the front door. maps HTTP requests (method + URL) to service calls
// Layer:    Controllers, the top layer. THIN on purpose: HTTP only, no business rules, no try/catch
//           (GlobalExceptionHandler catches whatever the service throws)
// Concepts: INHERITANCE (: ControllerBase gives Ok(), NotFound()...), DEPENDENCY INJECTION (IAuthService),
//           ABSTRACTION (only knows the interface), attribute routing ([Route] + [HttpPost] build the route table),
//           automatic model validation ([ApiController]), separation of concerns
// Related:  IAuthService / AuthService, Dtos/, Program.cs (AddControllers + MapControllers find this automatically)
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Mvc;
using OneCover.Api.Dtos;
using OneCover.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace OneCover.Api.Controllers;

// turns on API behavior, automatically validates the DTO ([Required], [EmailAddress]) and returns 400 with
// errors b4 method runs. Reads in complex params like LoginRequest from JSON body automatically.
[ApiController]  
[Route("api/auth")] // base URL for every action in this controller

// IAuthService: injected by DI, the controller only knows the interface
public class AuthController(IAuthService auth) : ControllerBase
// : ControllerBase give helpers like Ok(), NotFound(), Created(). and used instead of 'Controller' which adds web page views
{
        // POST for login vs GET
    // GET requests put data in the URL that show up in browser history + server logs, a place a password must *NEVER* go
    // POST sends it in the request body

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request) => 
    // ActionResult<AuthResponse> : an HTTP result whose body is an AuthResponse. Swagger reads it to show the response shape.
            Ok(await auth.LoginAsync(request)); // Ok: 200 OK, with the AuthResponse serialized to JSON as the body
    
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegistrationRequest request) => 
        Ok(await auth.RegisterAsync(request));

    // **TEMPORARY !! ** : proves JWT auth + ICurrentUser work. REMOVE once real protected endpoints exist ! **** 
    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI([FromServices] ICurrentUser currentUser) => Ok(new { currentUser.UserId, currentUser.IsAdmin});

    // **TEMPORARY !! ** : proves role-based [Authorize(Roles = "Admin")] works. REMOVE once real protected endpoints exist ! **** 
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-check")]
    public IActionResult AdminCheck() => Ok(new {message = "You're an admin."});
}