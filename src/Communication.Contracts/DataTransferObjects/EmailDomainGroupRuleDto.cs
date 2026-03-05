using System.ComponentModel.DataAnnotations;
using Meshmakers.Octo.ConstructionKit.Contracts;
using static Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ValidationConstants;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object for an email domain to group mapping rule.
/// </summary>
public class EmailDomainGroupRuleDto
{
    /// <summary>
    ///     Unique ID for the rule. Do not set when creating a new rule.
    /// </summary>
    public OctoObjectId? RtId { get; set; }

    /// <summary>
    ///     Email domain pattern to match (e.g., "meshmakers.com").
    ///     Matched against the domain part of the user's email.
    /// </summary>
    [Required]
    [StringLength(TextDefaultMaxLength)]
    public string? EmailDomainPattern { get; set; }

    /// <summary>
    ///     RtId of the group to which matching users are added.
    /// </summary>
    [Required]
    [StringLength(TextDefaultMaxLength)]
    public string? TargetGroupRtId { get; set; }

    /// <summary>
    ///     Optional description of this rule.
    /// </summary>
    [StringLength(DescriptionDefaultMaxLength)]
    public string? Description { get; set; }
}
