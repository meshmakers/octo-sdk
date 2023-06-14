using System;
using System.Runtime.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient;

[Serializable]
public class UnauthorizedServiceAccessException : ServiceClientException
{
    public UnauthorizedServiceAccessException()
    {
    }

    public UnauthorizedServiceAccessException(Exception? inner)
        : this("Access to the requested resource is not allowed.", inner)
    {
    }

    public UnauthorizedServiceAccessException(string message) : base(message)
    {
    }

    public UnauthorizedServiceAccessException(string message, Exception? inner) : base(message, inner)
    {
    }

    protected UnauthorizedServiceAccessException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}
