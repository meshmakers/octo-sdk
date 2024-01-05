namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
///     Attribute to mark a property as a connection to another entity.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
// ReSharper disable once ClassNeverInstantiated.Global
public class QlConnectionAttribute : Attribute
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="associationName">Name of association, e. g. children (camelCase)</param>
    /// <param name="connectionName">Name of connection property, eg. meshmakersAssetsPhotovoltaicPvModulesConnection (camelCase)</param>
    public QlConnectionAttribute(string associationName, string connectionName)
    {
        AssociationName = associationName;
        ConnectionName = connectionName;
    }

    /// <summary>
    ///     Returns the name of the association property in camelCase.
    /// </summary>
    public string AssociationName { get; }

    /// <summary>
    ///     Returns the name of the connection property in camelCase.
    /// </summary>
    public string ConnectionName { get; }
}