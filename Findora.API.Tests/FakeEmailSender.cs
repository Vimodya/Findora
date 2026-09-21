using System.Collections.Concurrent;
using Findora.API.Services;

namespace Findora.API.Tests;

/// <summary>
/// Test double for <see cref="IEmailSender"/> — captures every "sent"
/// message instead of logging it, so tests can pull the raw verification/
/// reset token out of the body (the real app only ever has that token in
/// the email body — see <c>LoggingEmailSender</c> — so this is the test
/// equivalent of "checking your inbox").
/// </summary>
public class FakeEmailSender : IEmailSender
{
    public record SentEmail(string To, string Subject, string Body);

    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> SentEmails => _sent.ToArray();

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(toEmail, subject, body));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Extracts the raw opaque token from a captured email body — every
    /// verification/reset email body is "...:\n\n{token}\n\n...".
    /// </summary>
    public static string ExtractToken(SentEmail email)
    {
        var lines = email.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines[1];
    }
}
