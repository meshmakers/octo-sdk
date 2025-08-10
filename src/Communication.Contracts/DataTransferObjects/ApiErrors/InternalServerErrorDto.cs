using System.Net;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;

/// <summary>
///     Represents an internal server error data transfer object
/// </summary>
public class InternalServerErrorDto : ApiErrorDto
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public InternalServerErrorDto()
        : base(HttpStatusCode.InternalServerError, nameof(HttpStatusCode.InternalServerError))
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">A message that describes the error</param>
    public InternalServerErrorDto(string message)
        : base(HttpStatusCode.InternalServerError, nameof(HttpStatusCode.InternalServerError), message)
    {
    }
}