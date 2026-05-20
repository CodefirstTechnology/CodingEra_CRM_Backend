namespace CRM.Services
{
    public interface IEmailService
    {
        Task<EmailSendResult> SendAsync(
            string to,
            string subject,
            string body,
            bool isHtml = true,
            CancellationToken cancellationToken = default);
    }
}
