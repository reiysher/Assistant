namespace Assistant.Api.Services.Mailing;

internal sealed class MailSettings
{
    public string? EmailSmtp { get; set; } = "smtp.gmail.com";
    public string? EmailLogin { get; set; } = "some_email@gmail.com";
    public string? EmailPassword { get; set; } = "some_password";
    public int? EmailPort { get; set; } = 465;
    public string? DisplayName { get; set; } = "Assistant";
}
