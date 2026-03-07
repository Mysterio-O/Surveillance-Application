using System.Net;
using System.Net.Mail;

namespace SurveilWin.Api.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    public SmtpEmailService(IConfiguration config) { _config = config; }

    public async Task SendInviteEmailAsync(string toEmail, string firstName, string inviterName, string orgName, string role, string inviteUrl)
    {
        var smtp = _config.GetSection("Email:Smtp");
        using var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"] ?? "587"))
        {
            Credentials = new NetworkCredential(smtp["Username"], smtp["Password"]),
            EnableSsl = true
        };

        var body = $"""
            Hi {firstName},

            {inviterName} has invited you to join {orgName} on SurveilWin as {role}.

            Click the link below to set up your account (valid for 48 hours):
            {inviteUrl}

            If you did not expect this invitation, you can safely ignore this email.

            — The SurveilWin Team
            """;

        var msg = new MailMessage(
            new MailAddress(smtp["FromAddress"]!, smtp["FromName"] ?? "SurveilWin"),
            new MailAddress(toEmail))
        {
            Subject = $"You've been invited to SurveilWin — {orgName}",
            Body = body
        };

        await client.SendMailAsync(msg);
    }
}
