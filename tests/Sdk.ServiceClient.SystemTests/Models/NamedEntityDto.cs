using System.Text.Json.Serialization;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Sdk.ServiceClient.SystemTests.Models;

/// <summary>
/// Named entity data transfer object
/// </summary>
public class NamedEntityDto : RtEntityDto
{
    /// <summary>
    /// Name of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Name { get; set; } = default!;
    
    /// <summary>
    /// Description of the entity
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; } = default!;
}