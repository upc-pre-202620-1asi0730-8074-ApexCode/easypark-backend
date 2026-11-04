namespace EasyPark.Platform.Shared.Domain.Model.Exceptions;

/// <summary>
///     Kinds of business errors. Each kind is translated to an HTTP status code by the
///     error handling middleware of the Interfaces layer.
/// </summary>
public enum DomainErrorKind
{
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Unprocessable
}

/// <summary>
///     Exception raised when a business rule or an invariant of the domain is violated.
///     <see cref="Code" /> is a stable, kebab-case identifier that the Web Application uses
///     to show a translated message (for example <c>no-availability</c>).
/// </summary>
public class DomainException : Exception
{
    public DomainException(DomainErrorKind kind, string code, string message) : base(message)
    {
        Kind = kind;
        Code = code;
    }

    public DomainErrorKind Kind { get; }

    public string Code { get; }

    public static DomainException BadRequest(string code, string message) =>
        new(DomainErrorKind.BadRequest, code, message);

    public static DomainException Unauthorized(string code, string message) =>
        new(DomainErrorKind.Unauthorized, code, message);

    public static DomainException Forbidden(string code, string message) =>
        new(DomainErrorKind.Forbidden, code, message);

    public static DomainException NotFound(string code, string message) =>
        new(DomainErrorKind.NotFound, code, message);

    public static DomainException Conflict(string code, string message) =>
        new(DomainErrorKind.Conflict, code, message);

    public static DomainException Unprocessable(string code, string message) =>
        new(DomainErrorKind.Unprocessable, code, message);
}
