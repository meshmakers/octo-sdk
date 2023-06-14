using System;
using System.Runtime.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

[Serializable]
public class AuthenticationFailedException : Exception
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public AuthenticationFailedException(string error)
    {
        Error = error;
    }

    public AuthenticationFailedException(string error, string message) : base(message)
    {
        Error = error;
    }

    public AuthenticationFailedException(string? error, string? message, Exception? inner) : base(message, inner)
    {
        Error = error;
    }

    protected AuthenticationFailedException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }

    public string? Error { get; }
}
