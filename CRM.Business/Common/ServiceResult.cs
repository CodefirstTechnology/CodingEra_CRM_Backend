namespace CRM.Business.Common;

public enum ServiceStatus
{
    Success,
    BadRequest,
    NotFound,
    Conflict,
    Unauthorized,
}

public sealed class ServiceResult<T>
{
    public ServiceStatus Status { get; init; }
    public T? Value { get; init; }
    public string? Message { get; init; }

    public static ServiceResult<T> Ok(T value) =>
        new() { Status = ServiceStatus.Success, Value = value };

    public static ServiceResult<T> Fail(ServiceStatus status, string? message = null) =>
        new() { Status = status, Message = message };
}

public sealed class ServiceResult
{
    public ServiceStatus Status { get; init; }
    public string? Message { get; init; }

    public static ServiceResult Ok() =>
        new() { Status = ServiceStatus.Success };

    public static ServiceResult Fail(ServiceStatus status, string? message = null) =>
        new() { Status = status, Message = message };
}
