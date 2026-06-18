namespace AuditAI.Application.Common.Results;

public class Result
{
    protected Result(bool isSuccess, Error? error, IReadOnlyList<ValidationError>? validationErrors)
    {
        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors ?? [];
    }

    public bool IsSuccess { get; }

    public bool IsValidationFailure => ValidationErrors.Count > 0;

    public bool IsNotFound => Error?.Code == "not_found";

    public bool IsUnauthorized => Error?.Code == "unauthorized";

    public Error? Error { get; }

    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public static Result Success()
    {
        return new Result(true, null, null);
    }

    public static Result Failure(string code, string message)
    {
        return new Result(false, new Error(code, message), null);
    }

    public static Result NotFound(string message)
    {
        return Failure("not_found", message);
    }

    public static Result Unauthorized(string message)
    {
        return Failure("unauthorized", message);
    }

    public static Result ValidationFailure(IReadOnlyList<ValidationError> validationErrors)
    {
        return new Result(false, null, validationErrors);
    }
}
