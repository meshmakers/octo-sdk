using System;
using System.Runtime.Serialization;

namespace Meshmakers.Octo.Frontend.Client.Tenants;

[Serializable]
public class QlQueryErrorException : Exception
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public QlQueryErrorException()
    {
    }

    public QlQueryErrorException(string message) : base(message)
    {
    }

    public QlQueryErrorException(string message, Exception inner) : base(message, inner)
    {
    }

    protected QlQueryErrorException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}
