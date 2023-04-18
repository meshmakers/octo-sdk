using System;
using System.Runtime.Serialization;

namespace Meshmakers.Octo.Frontend.Client;

[Serializable]
public class UnauthorizedServiceAccessException : ServiceClientException
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public UnauthorizedServiceAccessException()
        : this(default(Exception))
    {
    }

    public UnauthorizedServiceAccessException(Exception inner)
        : this("Access to the requested resource is not allowed.", inner)
    {
    }

    public UnauthorizedServiceAccessException(string message) : base(message)
    {
    }

    public UnauthorizedServiceAccessException(string message, Exception inner) : base(message, inner)
    {
    }

    protected UnauthorizedServiceAccessException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}
