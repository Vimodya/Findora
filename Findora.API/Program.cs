using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Findora.API.Authorization;
using Findora.API.Configuration;
using Findora.API.Data;
using Findora.API.Middleware;
using Findora.API.Models;
using Findora.API.Repositories;
using Findora.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Enums (e.g. Models.UserStatus) serialize/deserialize as their name
    // ("Suspended") rather than a bare integer — friendlier over the wire
    // and in Swagger, and rejects an invalid/misspelled value as a clean
    // 400 during model binding instead of silently accepting any integer.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// PostgreSQL / EF Core (Module 1 — Database Foundation).
// Connection string is read via standard ASP.NET Core configuration
// conventions: appsettings.json/appsettings.{Environment}.json,
// user-secrets (local dev), or the ConnectionStrings__DefaultConnection
// environment variable (Docker/staging/production). No credentials are
// hard-coded here or committed to appsettings.*.json, and the connection
// string itself must never be logged.
builder.Services.AddDbContext<FindoraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------------
// CORS (Module 0 — scaffold for the future React frontend).
// ---------------------------------------------------------------------
// Allowed origins are configured per environment via the "Cors:AllowedOrigins"
// appsettings section (see CorsSettings + appsettings.Development.json) —
// never AllowAnyOrigin(), and never a production domain hard-coded here
// before one actually exists. With no origins configured, the policy allows
// no cross-origin requests (safe default).
//
// NOTE: the policy is configured lazily via AddOptions<CorsOptions>().Configure<>()
// rather than by eagerly reading builder.Configuration here, so it always
// reflects the final configuration — including test-time overrides that
// WebApplicationFactory injects at the Build() call, which happens after
// this point in the file executes.
const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>().Configure<IOptions<CorsSettings>>((corsOptions, corsSettings) =>
{
    var allowedOrigins = corsSettings.Value.AllowedOrigins;
    corsOptions.AddPolicy(FrontendCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// ---------------------------------------------------------------------
// Authentication & Authorization (Module 2)
// ---------------------------------------------------------------------

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Configured lazily (see CORS note above) so the signing-key validation
// and TokenValidationParameters always see the final, fully-merged
// configuration — never a value read too early.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((bearerOptions, jwtSettingsOptions) =>
    {
        var settings = jwtSettingsOptions.Value;

        if (string.IsNullOrWhiteSpace(settings.SigningKey) ||
            Encoding.UTF8.GetByteCount(settings.SigningKey) < JwtSettings.MinimumSigningKeyLength)
        {
            // Fail fast and loudly rather than start with a missing/weak
            // signing key — never fall back to a hard-coded default
            // secret. Set via the Jwt__SigningKey environment variable or
            // `dotnet user-secrets` (see README); never commit a real
            // value to appsettings.*.json.
            throw new InvalidOperationException(
                $"Jwt:SigningKey is missing or shorter than {JwtSettings.MinimumSigningKeyLength} bytes. " +
                "Set it via the Jwt__SigningKey environment variable or `dotnet user-secrets` " +
                "(see README's Authentication configuration section) before starting the API.");
        }

        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Module 3 — a suspended/deactivated account's authorization fails
// immediately (403), even with an otherwise-still-valid access token. Added
// to the default policy (so plain [Authorize] and [Authorize(Roles = ...)]
// shorthand both pick it up automatically) and to the two named policies
// below, so it applies uniformly everywhere [Authorize] is used, present
// and future.
builder.Services.AddScoped<IAuthorizationHandler, ActiveAccountAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(options.DefaultPolicy)
        .AddRequirements(new ActiveAccountRequirement())
        .Build();

    options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleNames.Admin).AddRequirements(new ActiveAccountRequirement()));
    options.AddPolicy("OrganizationStaffOrAdmin", policy => policy.RequireRole(RoleNames.OrganizationStaff, RoleNames.Admin).AddRequirements(new ActiveAccountRequirement()));
});

// Password hashing — Microsoft.AspNetCore.Identity's PasswordHasher<TUser>
// (PBKDF2-based) used standalone, without the rest of ASP.NET Core Identity
// (no IdentityDbContext/UserManager) since Findora already has its own User
// entity and FindoraDbContext from Module 1.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Development-only stub — logs the "email" instead of sending it (see
// LoggingEmailSender). Replaced by a real provider in Module 16.
builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();

// ---------------------------------------------------------------------
// User & Profile Management (Module 3)
// ---------------------------------------------------------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// ---------------------------------------------------------------------
// Lost Item Reporting (Module 4)
// ---------------------------------------------------------------------
builder.Services.AddScoped<IItemCategoryRepository, ItemCategoryRepository>();
builder.Services.AddScoped<IItemCategoryService, ItemCategoryService>();
builder.Services.AddScoped<ILostItemRepository, LostItemRepository>();
builder.Services.AddScoped<ILostItemService, LostItemService>();

// Rate limiting (Module 2) — a modest fixed-window limiter per client IP,
// applied to the auth controller via [EnableRateLimiting("auth")]. Values
// are configurable per environment via "RateLimiting:Auth" and are not
// secrets.
builder.Services.Configure<AuthRateLimitSettings>(builder.Configuration.GetSection(AuthRateLimitSettings.SectionName));
builder.Services.AddRateLimiter(_ => { });
builder.Services.AddOptions<RateLimiterOptions>().Configure<IOptions<AuthRateLimitSettings>>((rateLimiterOptions, authRateLimitSettings) =>
{
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    var settings = authRateLimitSettings.Value;

    rateLimiterOptions.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = 0
            }));
});

// Basic health check (Module 0). No dependencies are registered yet, so
// this simply confirms the process is up and able to respond. A
// PostgreSQL-specific check can be added later (e.g. the
// AspNetCore.HealthChecks.NpgSql package) without changing callers.
builder.Services.AddHealthChecks();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Findora API",
        Version = "v1",
        Description = "Intelligent Lost & Found Network API"
    });

    // Lets Swagger UI send "Authorization: Bearer <token>" — no secrets are
    // configured here, just the scheme description for the "Authorize" button.
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste only the JWT access token (no \"Bearer \" prefix — Swagger adds it)."
    });
    options.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Force JwtBearerOptions to resolve right now (rather than lazily on the
// first incoming request), so a missing/weak signing key fails fast at
// startup — using the app's fully-built configuration, which by this point
// includes any test-time overrides too.
app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

// Global exception handling (Module 0) — registered first so it wraps every
// other middleware and always returns a consistent ProblemDetails response
// instead of a raw stack trace.
app.UseFindoraExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// GET /api/health — deliberately anonymous; must stay reachable even after
// Module 2 adds authentication elsewhere.
app.MapHealthChecks("/api/health").AllowAnonymous();

app.Run();

// Exposes the generated Program class to the test project (WebApplicationFactory<Program>).
public partial class Program { }
