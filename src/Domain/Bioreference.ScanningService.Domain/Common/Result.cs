namespace Bioreference.ScanningService.Domain.Common;

/// <summary>
/// Represents the outcome of an operation: either a success or a failure with an error message.
/// Use this instead of throwing exceptions for expected business-rule failures.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && error != string.Empty)
            throw new InvalidOperationException("A successful result cannot have an error.");
        if (!isSuccess && error == string.Empty)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

/// <summary>
/// Strongly-typed result that carries a value when successful.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

    internal Result(T? value, bool isSuccess, string error) : base(isSuccess, error)
    {
        _value = value;
    }
}
