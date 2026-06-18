namespace AuditAI.Application.Common.Results;

public sealed class Result<T> : Result
{
    private Result(
        bool isSuccess,
        T? value,
        Error? error,
        IReadOnlyList<ValidationError>? validationErrors)
        : base(isSuccess, error, validationErrors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null, null);
    }

    public static new Result<T> NotFound(string message)
    {
        return new Result<T>(false, default, new Error("not_found", message), null);
    }

    public static new Result<T> Failure(string code, string message)
    {
        return new Result<T>(false, default, new Error(code, message), null);
    }

    public static new Result<T> ValidationFailure(IReadOnlyList<ValidationError> validationErrors)
    {
        return new Result<T>(false, default, null, validationErrors);
    }
}
