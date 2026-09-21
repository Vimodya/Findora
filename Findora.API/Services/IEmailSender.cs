namespace Findora.API.Services;

/// <summary>
/// Abstraction over "sending" an email. Module 2 ships only a logging stub
/// (<see cref="LoggingEmailSender"/>) — real delivery (SMTP/SES/etc.) is
/// Module 16's job. Callers (AuthService) code against this interface so
/// swapping in a real provider later touches no calling code.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
