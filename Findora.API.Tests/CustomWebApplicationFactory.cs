using Findora.API.Data;
using Findora.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Findora.API.Tests;

/// <summary>
/// Boots the real <c>Program</c> (all of Module 0/1/2's wiring — exception
/// handling, CORS, JWT auth, rate limiting, etc.) against an isolated
/// in-memory database instead of the real PostgreSQL container, so tests
/// are fast and hermetic. Config overrides supply a fixed test-only JWT
/// signing key (never a real secret) and a configurable auth rate limit.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Unique per factory instance, so parallel test classes never share state.</summary>
    public string DatabaseName { get; } = $"findora-tests-{Guid.NewGuid()}";

    /// <summary>
    /// Requests permitted per IP per window for the "auth" rate-limit
    /// policy. Overridable per test fixture (see RateLimitingTests) —
    /// generous by default so unrelated tests never trip it.
    /// </summary>
    protected virtual int AuthPermitLimit => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "Findora.Tests",
                ["Jwt:Audience"] = "Findora.Tests.Client",
                // Fixed, clearly-fake test-only key — never a real secret,
                // never used outside this test process.
                ["Jwt:SigningKey"] = "test-only-signing-key-not-for-production-use-0123456789ABCDEF",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["RateLimiting:Auth:PermitLimit"] = AuthPermitLimit.ToString(),
                ["RateLimiting:Auth:WindowSeconds"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextOptionsDescriptor = services
                .SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<FindoraDbContext>));
            if (dbContextOptionsDescriptor is not null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            services.AddDbContext<FindoraDbContext>(options => options.UseInMemoryDatabase(DatabaseName));

            // Replace the real (logging) email stub with a capturing test
            // double, so tests can pull verification/reset tokens straight
            // out of the "sent" email body instead of scraping log output.
            var emailSenderDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
            if (emailSenderDescriptor is not null)
            {
                services.Remove(emailSenderDescriptor);
            }
            services.AddSingleton<IEmailSender, FakeEmailSender>();

            // EF Core's InMemory provider doesn't run migrations, but it
            // does apply model-level HasData seeds (the three roles) when
            // the database is created — so create it once, here.
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FindoraDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public FakeEmailSender EmailSender =>
        (FakeEmailSender)Services.GetRequiredService<IEmailSender>();
}
