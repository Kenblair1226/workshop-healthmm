namespace HisModern.Application.Common;

/// <summary>
/// 表示操作成功或失敗的結果型別。取代 legacy「吞例外後回傳 {ok:false,msg:"ERROR:..."}」的反模式，
/// 讓失敗成為明確、型別安全的回傳值。
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("成功的結果不應帶有錯誤訊息。");
        }

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException("失敗的結果必須帶有錯誤訊息。");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    /// <summary>失敗時的使用者可見訊息；成功時為 null。</summary>
    public string? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Ok(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Fail(error);
}

/// <summary>帶有成功值的結果型別。</summary>
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, string? error) : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>成功時的回傳值；失敗時為 default。</summary>
    public T? Value { get; }

    internal static Result<T> Ok(T value) => new(true, value, null);
    internal static Result<T> Fail(string error) => new(false, default, error);
}
