using System.Net;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;

/// <summary>
///     Error body of a request the caller is not allowed to make (HTTP 403), e.g. a refused scratch tenant (AB#7192).
/// </summary>
public class ForbiddenErrorDto : ApiErrorDto
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">A message that describes why the request is refused</param>
    public ForbiddenErrorDto(string message)
        : base(HttpStatusCode.Forbidden, nameof(HttpStatusCode.Forbidden), message)
    {
    }
}
