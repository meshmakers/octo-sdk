using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Base class for all GraphQL DTOs
/// </summary>
public class GraphQlDto
{
    /// <summary>
    /// A user context object that can be used to transport user specific data
    /// </summary>
    [JsonIgnore] public object? UserContext { get; set; }
}