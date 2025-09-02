using Meshmakers.Octo.ConstructionKit.Contracts.DependencyGraph;

namespace Meshmakers.Octo.Communication.Contracts;

/// <summary>
/// Signals errors that occur during mapping operations.
/// </summary>
public class MapperException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapperException"/> class.
    /// </summary>
    public MapperException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapperException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MapperException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapperException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">Inner exception</param>
    public MapperException(string message, Exception inner) : base(message, inner)
    {
    }

    internal static Exception CkTypeIdNotSet()
    {
        return new MapperException("CkTypeId not set on RtEntity");
    }
}
