using System;
using System.Net;
using System.Runtime.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient;

[Serializable]
public class ServiceClientResultException : Exception
{
    public ServiceClientResultException(HttpStatusCode httpStatusCode)
        : this(null, httpStatusCode, null)
    {
    }

    public ServiceClientResultException(string message, HttpStatusCode httpStatusCode)
        : this(message, httpStatusCode, null)
    {
    }

    public ServiceClientResultException(string? message, HttpStatusCode httpStatusCode, Exception? inner) : base(
        string.IsNullOrEmpty(message)
            ? $"The service returned result '{httpStatusCode}'"
            : $"{httpStatusCode}: {message}", inner)
    {
        HttpStatusCode = httpStatusCode;
    }

    protected ServiceClientResultException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }

    public HttpStatusCode HttpStatusCode { get; }
}
