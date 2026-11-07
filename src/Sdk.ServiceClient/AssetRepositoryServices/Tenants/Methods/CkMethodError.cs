namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     A failed CK method call as the asset repository reports it (AB#8423): one GraphQL error whose
///     <c>extensions</c> carry the machine-readable contract (<c>code</c>, <c>parameters</c>, optional <c>details</c>).
///     For a declared error, <see cref="Code" /> is the code the method declares in its CK model (e.g.
///     <c>PASSWORD_POLICY_VIOLATION</c>).
/// </summary>
public sealed class CkMethodError
{
    /// <summary>
    ///     Creates the error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The (language-neutral) message of the GraphQL error</param>
    /// <param name="parameters">The error parameters</param>
    /// <param name="details">The per-item details</param>
    public CkMethodError(string code, string message, IReadOnlyDictionary<string, string> parameters,
        IReadOnlyList<CkMethodErrorDetail> details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
        Message = message;
        Parameters = parameters;
        Details = details;
    }

    /// <summary>
    ///     The error code (<c>extensions.code</c>).
    /// </summary>
    public string Code { get; }

    /// <summary>
    ///     The message of the GraphQL error. It is a fixed text; use <see cref="Code" /> and <see cref="Parameters" />.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     The language-neutral error parameters (<c>extensions.parameters</c>); empty when there are none.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>
    ///     The per-item details (<c>extensions.details</c>); empty when there are none.
    /// </summary>
    public IReadOnlyList<CkMethodErrorDetail> Details { get; }

    /// <inheritdoc />
    public override string ToString() => Code;
}

/// <summary>
///     One per-item detail of a <see cref="CkMethodError" /> (<c>extensions.details[]</c>).
/// </summary>
/// <param name="Code">The detail code</param>
/// <param name="Parameters">The detail parameters</param>
public sealed record CkMethodErrorDetail(string Code, IReadOnlyDictionary<string, string> Parameters);
