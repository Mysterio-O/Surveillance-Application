namespace SurveilWin.Api.Services;

public interface IEmailService
{
    Task SendInviteEmailAsync(string toEmail, string firstName, string inviterName, string orgName, string role, string inviteUrl);
}
