namespace ERP.Shared.Models
{
    /// <summary>
    /// Generic API response envelope for consistent response shapes across modules.
    /// Controllers may use this for non-list endpoints that need structured error details.
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>Whether the request succeeded.</summary>
        public bool Success { get; init; }

        /// <summary>Response payload (null on failure).</summary>
        public T? Data { get; init; }

        /// <summary>Human-readable summary message.</summary>
        public string? Message { get; init; }

        /// <summary>Field-level validation errors (key = field name, value = error messages).</summary>
        public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

        /// <summary>Creates a successful response with data.</summary>
        public static ApiResponse<T> Ok(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message };

        /// <summary>Creates a failure response with an error message.</summary>
        public static ApiResponse<T> Fail(string message, IReadOnlyDictionary<string, string[]>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };

        /// <summary>Creates a failure response from a single field error.</summary>
        public static ApiResponse<T> FieldError(string field, string error) =>
            Fail(error, new Dictionary<string, string[]> { [field] = [error] });
    }

    /// <summary>Non-generic convenience alias for responses without a payload.</summary>
    public class ApiResponse : ApiResponse<object?>
    {
        /// <summary>Creates a successful response with no payload.</summary>
        public static ApiResponse OkEmpty(string? message = null) =>
            new() { Success = true, Message = message };

        /// <summary>Creates a failure response with no payload.</summary>
        public static new ApiResponse Fail(string message, IReadOnlyDictionary<string, string[]>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }
}
