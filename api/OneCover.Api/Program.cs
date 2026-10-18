// ── ANATOMY ──────────────────────────────────────────────────────────────────────────────
// What:     the app's entry point + composition root: where every piece gets wired together
// Layer:    startup. runs once when the app starts, then the pipeline handles every request
// Concepts: two halves, split at builder.Build():
//             1. builder.Services.AddX(...) = register services in the DI CONTAINER (interface -> class, lifetime)
//             2. app.UseX() = build the MIDDLEWARE PIPELINE (order matters, outermost first)
//           service lifetimes: Singleton (one for the app), Scoped (one per request), builder pattern,
//           layered configuration (appsettings -> appsettings.Development -> user-secrets -> env vars), fail fast
// Related:  every AddScoped line points to an interface + class pair (ICurrentUser/CurrentUser,
//           IUserRepository/UserRepository, IPlanRepository/PlanRepository, IAuthService/AuthService),
//           AppDbContext, GlobalExceptionHandler, DbSeeder
// ─────────────────────────────────────────────────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using OneCover.Api.Data;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OneCover.Api.Errors;
using OneCover.Api.Services;
using OneCover.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

// --- services: register everything the DI container needs to know about ---

// controllers, plus send enums as text in JSON ("InReview", not 1) so Angular gets readable values
builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

// EF Core context, one per request, pointed at SQL Server via the connection string in appsettings
builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// one shared clock for the whole app, injected so tests can swap in a fake "today"
builder.Services.AddSingleton(TimeProvider.System);

// one place that turns exceptions into consistent ProblemDetails error responses

builder.Services.AddProblemDetails(); // AddProb... registers IProblemDetailsService, what the handler receives in its constructor and uses to write JSON

builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // AddExce.... tells ASP.NET Core to call this class when an exception escapes

// lets classes like CurrentUser see the current HTTP request (services normally can't)
builder.Services.AddHttpContextAccessor();

// "when something asks for ICurrentUser, give it a CurrentUser". scoped = a new one per request,
// since every request has its own logged-in user
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// "when something asks for IUserRepository, give it a UserRepository". scoped b/c it uses AppDbContext,
// which is one per request, and a service can't safely outlive what it depends on
builder.Services.AddScoped<IUserRepository, UserRepository>();

// same idea for plans: IPlanRepository -> PlanRepository, scoped for the same reason (it uses AppDbContext)
builder.Services.AddScoped<IPlanRepository, PlanRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();

// fail fast: if the key were ever missing the ?? throw fails at startup w/ a message stating the exact fix needed
var jwtKey = builder.Configuration["Jwt:Key"] 
        ?? throw new InvalidOperationException("Jwt:Key is not configured. Set it with dotnet user-secrets.");

// JWT auth: checks every incoming token before we trust who the user says they are
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => 
        o.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true, // was it created by OneCover.Api?
    ValidateAudience = true, // was it meant for OneCover.Web?
    ValidateLifetime = true, // has it expired?
    ValidateIssuerSigningKey = true, // does the signature match the secret key? Or, has someone forged or edited it?
    ValidIssuer = builder.Configuration["Jwt:Issuer"],
    ValidAudience = builder.Configuration["Jwt:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
});

// authorization = are you allowed? (authentication above = who are you?)
builder.Services.AddAuthorization();

// CORS: let the Angular app (localhost:4200) call the API, which runs on a different origin (localhost:7020)
builder.Services.AddCors(o => 
        o.AddPolicy("web", p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

// everything above registers services, everything below sets up how requests are handled
var app = builder.Build();


// placed here first in the pipeline so it catches errors from everything after it
app.UseExceptionHandler();

// turns any empty error response (like JWT middleware's bare 401) into a standard ProblemDetails body.
app.UseStatusCodePages();

// dev only: fill an empty DB with demo plans + users
if (app.Environment.IsDevelopment())
{
    await DbSeeder.SeedAsync(app.Services);
}

// dev only: API docs + Swagger UI for quick testing
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "OneCover API"));
}

// request pipeline: every request runs through these in order, top to bottom 
app.UseHttpsRedirection();
app.UseCors("web");
// authentication has to come before authorization, can't check permissions before we know who you are
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
