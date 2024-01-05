using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Extensions of <see cref="RtEntityId" />.
/// </summary>
public static class RtEntityIdExtensions
{
    /// <summary>
    ///     Creates an instance of <see cref="RtEntityId" /> from <see cref="RtEntityDto" />.
    /// </summary>
    /// <param name="rtEntity"></param>
    /// <returns></returns>
    public static RtEntityId ToRtEntityId(this RtEntityDto rtEntity)
    {
        return new RtEntityId(rtEntity.CkTypeId, rtEntity.RtId);
    }
}