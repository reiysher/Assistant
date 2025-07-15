namespace Assistant.Api.Services.Mailing;

public sealed record MailRequest(
    string? To,
    string? Subject,
    string? From = null,
    string? Body = null,
    IDictionary<string, byte[]>? AttachmentData = null);