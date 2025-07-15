using MailKit.Net.Smtp;
using MimeKit;

namespace Assistant.Api.Services.Mailing;

public class EmailService
{
    private readonly MailSettings _options = new MailSettings();

    public async Task SendEmailAsync(MailRequest request)
    {
        var emailMessage = new MimeMessage();

        // From (name and email)
        emailMessage.From.Add(new MailboxAddress(_options.DisplayName, request.From ?? _options.EmailLogin));
        emailMessage.To.Add(MailboxAddress.Parse(request.To));
        emailMessage.Subject = request.Subject;
        emailMessage.Sender = new MailboxAddress(_options.DisplayName, request.From ?? _options.EmailLogin);

        var builder = new BodyBuilder();
        builder.HtmlBody = request.Body;

        // Create the file attachments for this e-mail message
        if (request.AttachmentData != null)
        {
            foreach (var attachmentInfo in request.AttachmentData)
                builder.Attachments.Add(attachmentInfo.Key, attachmentInfo.Value);
        }

        emailMessage.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.EmailSmtp, _options.EmailPort ?? throw new ArgumentNullException(nameof(_options.EmailPort)), true);
        await client.AuthenticateAsync(_options.EmailLogin, _options.EmailPassword);
        await client.SendAsync(emailMessage);
        await client.DisconnectAsync(true);
    }
}
