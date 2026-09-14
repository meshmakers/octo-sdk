using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#4924 §9.2 / §10 — the queue entry every surface reads.
///     <para>
///         The load-bearing pin is the <b>absence</b> of a global rank. Round-robin serves borrowing
///         tenants in turns, so "this item is 7th" is not a fact that exists; the truthful answer is
///         the pair <c>PositionInTenant</c> + <c>TenantsAheadInRotation</c>. A rank field is the one
///         thing a well-meaning future change is most likely to add, because it is the number a user
///         asks for first — so it is pinned here rather than left to review.
///     </para>
/// </summary>
public class AdapterPoolQueueEntryDtoTests
{
    /// <summary>Exactly what the controller serialises, with ASP.NET's camelCase naming.</summary>
    private const string ControllerWireShape =
        """
        {
          "executionId": "8f1c0b9a-0001-4a8f-9d21-2f0c9c4f5a11",
          "borrowerTenantId": "borrower-a",
          "pipelineRtId": "68c1a2b3c4d5e6f701020304",
          "pipelineName": "Nightly billing",
          "executionClass": 1,
          "queuedAtUtc": "2026-09-14T08:00:00Z",
          "positionInTenant": 2,
          "tenantsAheadInRotation": 1,
          "leasedOnMemberId": null,
          "leaseExpiresAtUtc": null
        }
        """;

    [Fact]
    public void CarriesNoGlobalRankShapedMember()
    {
        var members = typeof(AdapterPoolQueueEntryDto).GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.DoesNotContain(members, n => n.Contains("Rank", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, n => n.Equals("Position", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(members, n => n.Contains("GlobalPosition", StringComparison.OrdinalIgnoreCase));

        // ...and both halves of the truthful answer are present.
        Assert.Contains(nameof(AdapterPoolQueueEntryDto.PositionInTenant), members);
        Assert.Contains(nameof(AdapterPoolQueueEntryDto.TenantsAheadInRotation), members);
    }

    [Fact]
    public void DeserialisesTheControllerWireShape()
    {
        var dto = JsonSerializer.Deserialize<AdapterPoolQueueEntryDto>(
            ControllerWireShape, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(dto);
        Assert.Equal("borrower-a", dto!.BorrowerTenantId);
        Assert.Equal("Nightly billing", dto.PipelineName);
        Assert.Equal(1, dto.ExecutionClass);
        Assert.Equal(2, dto.PositionInTenant);
        Assert.Equal(1, dto.TenantsAheadInRotation);
        Assert.False(dto.IsLeased);
    }

    [Fact]
    public void ALeasedEntryIsTheOnlyPlaceAMemberIdAppears()
    {
        const string wireShape =
            """
            {
              "executionId": "8f1c0b9a-0002-4a8f-9d21-2f0c9c4f5a11",
              "borrowerTenantId": "borrower-b",
              "executionClass": 0,
              "queuedAtUtc": "2026-09-14T07:59:00Z",
              "positionInTenant": 0,
              "tenantsAheadInRotation": 0,
              "leasedOnMemberId": "member-3",
              "leaseExpiresAtUtc": "2026-09-14T08:10:00Z"
            }
            """;

        var dto = JsonSerializer.Deserialize<AdapterPoolQueueEntryDto>(
            wireShape, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(dto);
        Assert.True(dto!.IsLeased);
        Assert.Equal("member-3", dto.LeasedOnMemberId);
        // A leased entry is not in a position any more — it is what the queue waits behind.
        Assert.Equal(0, dto.PositionInTenant);
    }

    /// <summary>
    ///     An <c>ExecutionClass</c> the reading side does not know must deserialize rather than throw,
    ///     the same rule <see cref="NodeDescriptorDto.ExecutionClass" /> follows: this is a wire
    ///     contract and a newer controller may name a third class.
    /// </summary>
    [Fact]
    public void AnUnknownExecutionClassDeserialisesRatherThanThrowing()
    {
        var dto = JsonSerializer.Deserialize<AdapterPoolQueueEntryDto>(
            """{"executionId":"e","borrowerTenantId":"t","executionClass":7,"positionInTenant":1}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(dto);
        Assert.Equal(7, dto!.ExecutionClass);
    }
}
