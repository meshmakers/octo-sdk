// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a socket configuration for data transfer.
/// </summary>
public record SocketConfigurationDto
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SocketConfigurationDto" /> class.
    /// </summary>
    /// <param name="socketRtId">Id of the socket.</param>
    public SocketConfigurationDto(OctoObjectId socketRtId)
    {
        SocketRtId = socketRtId;
    }

    /// <summary>
    ///     Gets or sets the id of the socket.
    /// </summary>
    public OctoObjectId SocketRtId { get; }

    /// <inheritdoc />
    public virtual bool Equals(SocketConfigurationDto? other)
    {
        if (other == null)
        {
            return false;
        }

        return SocketRtId.Equals(other.SocketRtId);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return SocketRtId.GetHashCode();
    }
}