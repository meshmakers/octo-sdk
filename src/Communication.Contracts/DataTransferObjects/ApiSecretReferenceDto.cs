namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Request body that names one stored client or API resource secret by its stored value, for
///     <c>POST {tenant}/v1/apiSecrets/{client|apiResource}/{id}/delete</c> and <c>.../lookup</c>. The stored value is a
///     credential-equivalent hash, so it travels in the body, never in the URL.
/// </summary>
public class ApiSecretReferenceDto
{
    /// <summary>
    ///     The stored value of the secret (<see cref="ApiSecretDto.ValueEncrypted" />).
    /// </summary>
    public string ValueEncrypted { get; set; } = null!;

    /// <summary>
    ///     Never carries the value.
    /// </summary>
    public override string ToString() => nameof(ApiSecretReferenceDto);
}
