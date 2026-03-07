namespace SurveilWin.Api.Services;

public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;
    public LoggingEmailService(ILogger<LoggingEmailService> logger) { _logger = logger; }

    public Task SendInviteEmailAsync(string toEmail, string firstName, string inviterName, string orgName, string role, string inviteUrl)
    {
        _logger.LogInformation(
            "📧 [DEV EMAIL] To: {Email} | Invite URL: {Url}",
            toEmail, inviteUrl);
        return Task.CompletedTask;
    }
}
