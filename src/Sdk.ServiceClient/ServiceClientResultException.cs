using System;
using System.Net;
using System.Runtime.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
/// Expands the <see cref="ServiceClientResultException"/> with the HTTP status code.
/// </summary>
[Serializable]
public class ServiceClientResultException : Exception
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpStatusCode">The HTTP status code received</param>
    public ServiceClientResultException(HttpStatusCode httpStatusCode)
        : this(null, httpStatusCode)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="message">Message of exception</param>
    /// <param name="httpStatusCode">The HTTP status code received</param>
    /// <param name="inner">Optionally the inner exception</param>
    public ServiceClientResultException(string? message, HttpStatusCode httpStatusCode, Exception? inner = null) : base(
        string.IsNullOrEmpty(message)
            ? $"The service returned result '{httpStatusCode}'"
            : $"{httpStatusCode}: {message}", inner)
    {
        HttpStatusCode = httpStatusCode;
    }

    /// <inheritdoc />
    protected ServiceClientResultException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }

    /// <summary>
    /// Returns the status code
    /// </summary>
    public HttpStatusCode HttpStatusCode { get; }
}
