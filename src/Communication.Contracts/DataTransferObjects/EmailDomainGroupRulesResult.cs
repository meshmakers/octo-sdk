namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Result containing email domain group rules.
/// </summary>
public class EmailDomainGroupRulesResult
{
    /// <summary>
    ///     The email domain group rules.
    /// </summary>
    public IEnumerable<EmailDomainGroupRuleDto> EmailDomainGroupRules { get; set; } = [];
}
