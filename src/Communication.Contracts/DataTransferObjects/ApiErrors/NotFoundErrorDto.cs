using System.Net;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;

/// <summary>
///     Represents a not found error data transfer object
/// </summary>
public class NotFoundErrorDto : ApiErrorDto
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public NotFoundErrorDto()
        : base(HttpStatusCode.NotFound, nameof(HttpStatusCode.NotFound))
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">A message that describes the error</param>
    public NotFoundErrorDto(string message)
        : base(HttpStatusCode.NotFound, nameof(HttpStatusCode.NotFound), message)
    {
    }
}