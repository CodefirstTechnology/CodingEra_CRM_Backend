namespace CRM.Services
{
    public sealed class EmailSendResult
    {
        public bool Success { get; init; }

        /// <summary>Safe, client-facing message when <see cref="Success"/> is false.</summary>
        public string? ErrorMessage { get; init; }

        public static EmailSendResult Ok() => new() { Success = true };

        public static EmailSendResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }
}
