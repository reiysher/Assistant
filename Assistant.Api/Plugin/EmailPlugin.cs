using System.ComponentModel;
using Assistant.Api.Services.Mailing;
using Microsoft.SemanticKernel;

namespace Assistant.Api.Plugin;

public class EmailPlugin(EmailService emailService)
{
    [KernelFunction("SendEmail")]
    [Description("Отправляет письмо на почту")]
    public async Task<string> SendEmailAsync(
        [Description("Адрес электронной почты, куда отправить письмо")]
        string to,
        [Description("Тема письма")]
        string subject,
        [Description("Тело письма")]
        string body)
    {
        await emailService.SendEmailAsync(new MailRequest(to, subject, Body: body));
        return "Email успешно отправлен!";
    }
}