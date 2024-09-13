namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

/// <summary>
/// Exception thrown when there is an error serializing or deserializing a pipeline.
/// </summary>
public class PipelineSerializationException : DataPipelineException
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public PipelineSerializationException()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public PipelineSerializationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public PipelineSerializationException(string message, Exception inner) : base(message, inner)
    {
    }

    internal static Exception DeserializeError(Exception exception)
    {
        string message = "Error deserializing pipeline." + Environment.NewLine;
        Exception? tmpException = exception;
        while (tmpException != null)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                message += Environment.NewLine;
            }

            message += tmpException.Message;
            
            tmpException = tmpException.InnerException;
        }
        return new PipelineSerializationException(message, exception);
    }

    internal static Exception SerializeError(Exception exception)
    {
        string message = "Error serializing pipeline." + Environment.NewLine;
        Exception? tmpException = exception;
        while (tmpException != null)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                message += Environment.NewLine;
            }

            message += tmpException.Message;
            
            tmpException = tmpException.InnerException;
        }
        return new PipelineSerializationException(message, exception);
    }
}