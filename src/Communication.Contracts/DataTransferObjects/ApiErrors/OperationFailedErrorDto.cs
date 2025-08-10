using System.Net;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;

/// <summary>
///     Represents an internal server error data transfer object
/// </summary>
public class OperationFailedErrorDto : ApiErrorDto
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public OperationFailedErrorDto()
        : base(HttpStatusCode.BadRequest, nameof(HttpStatusCode.BadRequest))
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">A message that describes the error</param>
    public OperationFailedErrorDto(string message)
        : base(HttpStatusCode.BadRequest, nameof(HttpStatusCode.BadRequest), message)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">A message that describes the error</param>
    /// <param name="failedDetails">A list of that describes the error</param>
    public OperationFailedErrorDto(string message, IEnumerable<FailedDetailsDto> failedDetails)
        : base(HttpStatusCode.BadRequest, nameof(HttpStatusCode.BadRequest), message, failedDetails)
    {
    }
}