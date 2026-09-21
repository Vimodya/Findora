namespace Findora.API.Services;

/// <summary>
/// Development-only <see cref="IEmailSender"/> stub: logs the "email"
/// instead of sending it, exactly as Module 2's TODO specifies ("actual
/// email sending can be a stub/log in dev"). This is how a developer
/// retrieves a verification/reset link locally without a real mail
/// provider — replace with a real implementation in Module 16.
///
/// Defensive guard: outside Development, the message body (which may
/// contain a live verification/reset link) is never logged — only the
/// fact that an email would have been sent — so this stub can never leak a
/// working token if it's accidentally still wired up in a deployed
/// environment.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly IHostEnvironment _environment;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "[DEV EMAIL STUB] To: {ToEmail} | Subject: {Subject}\n{Body}",
                toEmail,
                subject,
                body);
        }
        else
        {
            _logger.LogInformation(
                "[DEV EMAIL STUB] Would send an email to {ToEmail} (subject: {Subject}); " +
                "body withheld from logs outside Development. Wire up a real provider (Module 16).",
                toEmail,
                subject);
        }

        return Task.CompletedTask;
    }
}
