namespace AssesmentReservas.API.Common;

/// <summary>Resultado estándar de una operación de servicio (éxito + errores).</summary>
public class ServiceResult
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };
    public static ServiceResult Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
}

/// <summary>Resultado de servicio que además transporta un dato.</summary>
public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public new static ServiceResult<T> Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };
    public new static ServiceResult<T> Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
}
