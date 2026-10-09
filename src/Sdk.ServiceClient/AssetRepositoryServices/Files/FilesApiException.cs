using System.Net;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Files;

/// <summary>
///     Stable error codes of the file API. REST answers carry them as the <c>code</c> extension of the
///     problem details, GraphQL answers as <c>extensions.code</c> of the error.
/// </summary>
public static class FileErrorCodes
{
    /// <summary>The folder root does not exist (404).</summary>
    public const string RootNotFound = "ROOT_NOT_FOUND";

    /// <summary>The path or rtId does not exist or is hidden by data permissions (404).</summary>
    public const string PathNotFound = "PATH_NOT_FOUND";

    /// <summary>Legacy duplicate names in a folder; address the entry by rtId (409).</summary>
    public const string AmbiguousPath = "AMBIGUOUS_PATH";

    /// <summary>The name exists already in the folder (409).</summary>
    public const string NameConflict = "NAME_CONFLICT";

    /// <summary>The name is invalid (400).</summary>
    public const string InvalidName = "INVALID_NAME";

    /// <summary>The well-known name is reserved for a route segment (409).</summary>
    public const string ReservedName = "RESERVED_NAME";

    /// <summary>The root belongs to a service or a blueprint and cannot be renamed or deleted (409).</summary>
    public const string ProtectedRoot = "PROTECTED_ROOT";

    /// <summary>Denied by data permissions or missing role (403).</summary>
    public const string Forbidden = "FORBIDDEN";

    /// <summary>The entry is not a folder (409).</summary>
    public const string NotAFolder = "NOT_A_FOLDER";

    /// <summary>The entry is not a file (409).</summary>
    public const string NotAFile = "NOT_A_FILE";

    /// <summary>A folder cannot be moved into itself or below itself (409).</summary>
    public const string MoveIntoItself = "MOVE_INTO_ITSELF";

    /// <summary>Upload size or zip limits exceeded (413).</summary>
    public const string LimitExceeded = "LIMIT_EXCEEDED";

    /// <summary>The request is invalid (400).</summary>
    public const string InvalidRequest = "INVALID_REQUEST";

    /// <summary>The server answered an error without a file API code.</summary>
    public const string Unknown = "UNKNOWN";

    internal static HttpStatusCode StatusOf(string code)
    {
        return code switch
        {
            RootNotFound or PathNotFound => HttpStatusCode.NotFound,
            AmbiguousPath or NameConflict or ReservedName or ProtectedRoot or NotAFolder or NotAFile
                or MoveIntoItself => HttpStatusCode.Conflict,
            Forbidden => HttpStatusCode.Forbidden,
            LimitExceeded => HttpStatusCode.RequestEntityTooLarge,
            InvalidName or InvalidRequest => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };
    }

    internal static string FallbackFor(HttpStatusCode status)
    {
        return status switch
        {
            HttpStatusCode.Forbidden => Forbidden,
            HttpStatusCode.RequestEntityTooLarge => LimitExceeded, // e.g. the ingress body-size limit
            HttpStatusCode.NotFound => PathNotFound,
            _ => Unknown
        };
    }
}

/// <summary>
///     A file API call failed. Carries the stable <see cref="Code" /> (see <see cref="FileErrorCodes" />) and
///     the HTTP status. For GraphQL errors (which travel with HTTP 200) the status is derived from the code,
///     so callers can treat REST and GraphQL failures alike.
/// </summary>
[Serializable]
public class FilesApiException : ServiceClientResultException
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="code">Stable error code</param>
    /// <param name="httpStatusCode">HTTP status (or the status derived from the code)</param>
    /// <param name="detail">Human readable detail</param>
    /// <param name="isGraphQlError">True when the error came from the GraphQL endpoint</param>
    /// <param name="inner">Inner exception</param>
    public FilesApiException(string code, HttpStatusCode httpStatusCode, string? detail,
        bool isGraphQlError = false, Exception? inner = null)
        : base(string.IsNullOrEmpty(detail) ? code : $"{code}: {detail}", httpStatusCode, inner)
    {
        Code = code;
        Detail = detail;
        IsGraphQlError = isGraphQlError;
    }

    /// <summary>Stable error code (see <see cref="FileErrorCodes" />).</summary>
    public string Code { get; }

    /// <summary>Human readable detail of the server.</summary>
    public string? Detail { get; }

    /// <summary>True when the error came from the GraphQL endpoint (metadata operations).</summary>
    public bool IsGraphQlError { get; }
}
