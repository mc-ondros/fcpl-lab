using System.Text.Json;
using System.Text.Json.Serialization;
using Capstone.Core.Model;
using SharpQuest.Contracts;

namespace Capstone.Core.Json;

/// <summary>
/// Converts Spin entities to JSON and parses external JSON back to domain models.
/// </summary>
public sealed class SpinMapper : IItemMapper
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ToJson(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is not Spin spin)
            throw new ArgumentException($"This mapper only supports Spin items, not {item.GetType().Name}.", nameof(item));

        return JsonSerializer.Serialize(ToDto(spin), Options);
    }

    public IItem? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        SpinDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<SpinDto>(json, Options);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }

        return dto is null ? null : FromDto(dto);
    }

    public static SpinDto ToDto(Spin spin) =>
        new(
            spin.Id,
            spin.Timestamp,
            spin.Bet,
            spin.Reels.First,
            spin.Reels.Second,
            spin.Reels.Third,
            spin.Payout,
            spin.CreditsAfter,
            spin.IsFreeSpin,
            spin.SessionId);

    public static Spin? FromDto(SpinDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id)) return null;
        if (dto.Bet is null or <= 0) return null;
        if (dto.Payout is null or < 0) return null;
        if (dto.CreditsAfter is null or < 0) return null;
        if (dto.Reel1 is not { } r1 || dto.Reel2 is not { } r2 || dto.Reel3 is not { } r3) return null;

        return new Spin
        {
            Id = dto.Id,
            Timestamp = dto.Timestamp ?? DateTimeOffset.UtcNow,
            Bet = dto.Bet.Value,
            Reels = new Reels(r1, r2, r3),
            Payout = dto.Payout.Value,
            CreditsAfter = dto.CreditsAfter.Value,
            IsFreeSpin = dto.IsFreeSpin ?? false,
            SessionId = dto.SessionId,
        };
    }
}
