namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
///     Represents an Octo query response
/// </summary>
/// <typeparam name="TDto"></typeparam>
// ReSharper disable once ClassNeverInstantiated.Global
public class QlQueryConnection<TDto> where TDto : class
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public QlQueryConnection()
    {
    }
    
    /// <summary>
    ///     Constructor
    /// </summary>
    public QlQueryConnection(QlItemsContainer<TDto> connection)
    {
        Connection = connection;
    }

    /// <summary>
    ///     Returns the deserialize connection object data
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public QlItemsContainer<TDto>? Connection { get; private set; }
}