using System;
using System.Net;
using System.Runtime.Serialization;

namespace Meshmakers.Octo.Frontend.Client;

[Serializable]
public class ServiceClientResultException : Exception
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public ServiceClientResultException(HttpStatusCode httpStatusCode)
        : this(null, httpStatusCode, null)
    {
    }

    public ServiceClientResultException(string message, HttpStatusCode httpStatusCode)
        : this(message, httpStatusCode, null)
    {
    }

    public ServiceClientResultException(string message, HttpStatusCode httpStatusCode, Exception inner) : base(
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
