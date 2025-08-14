namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Defines the import strategy for a model.
/// </summary>
public enum ImportStrategyDto
{
    /// <summary>
    /// Will only insert new entities. If an entity already exists, an error will be thrown.
    /// </summary>
    InsertOnly = 0,

    /// <summary>
    /// Replaces existing entities with the same ID. If an entity does not exist, it will be inserted.
    /// </summary>
    Upsert = 1,
}