using System.Text.Json;
using System.Text.Json.Serialization;
using Capstone.Core.Json;
using Capstone.Core.Model;
using SharpQuest.Contracts;

namespace Capstone.Core.Storage;

/// <summary>
/// Async JSON persistence for <see cref="SpinHistoryStore"/> preserving
/// polymorphic item types (<see cref="FreeSpinBonus"/>, <see cref="CreditBonus"/>, <see cref="Spin"/>)
/// and insertion order (Lab 08).
/// </summary>
public static class StoreJsonStorage
{
    private const string MagicFormat = "luckyspin-store-v1";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task SaveAsync(IItemStore store, string path)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var items = store.All().Select(ToDto).ToList();
        var envelope = new StoreEnvelopeDto(MagicFormat, items);

        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        await JsonSerializer.SerializeAsync(stream, envelope, Options).ConfigureAwait(false);
    }

    public static async Task<IItemStore> LoadAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            return new SpinHistoryStore();

        StoreEnvelopeDto? envelope;
        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            envelope = await JsonSerializer.DeserializeAsync<StoreEnvelopeDto>(stream, Options).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The file does not contain valid LuckySpin store JSON.", ex);
        }

        if (envelope is null || envelope.Format != MagicFormat || envelope.Items is null)
            throw new InvalidDataException("The JSON file does not match the LuckySpin store schema.");

        var store = new SpinHistoryStore();
        foreach (var dto in envelope.Items)
            store.Add(FromDto(dto));

        return store;
    }

    private static StoredItemDto ToDto(IItem item) => item switch
    {
        FreeSpinBonus freeSpin => new StoredItemDto
        {
            Kind = "free_spin",
            Id = freeSpin.Id,
            Reason = freeSpin.Reason,
            Spins = freeSpin.Spins,
        },
        CreditBonus credit => new StoredItemDto
        {
            Kind = "credit_bonus",
            Id = credit.Id,
            Reason = credit.Reason,
            Credits = credit.Credits,
        },
        Spin spin => new StoredItemDto
        {
            Kind = "spin",
            Id = spin.Id,
            Spin = SpinMapper.ToDto(spin),
        },
        _ => throw new InvalidDataException($"Unsupported item type '{item.GetType().Name}'."),
    };

    private static IItem FromDto(StoredItemDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Kind))
            throw new InvalidDataException("Stored item entry is missing required fields.");

        return dto.Kind switch
        {
            "free_spin" => new FreeSpinBonus
            {
                Id = dto.Id,
                Reason = dto.Reason ?? throw new InvalidDataException("FreeSpinBonus missing Reason."),
                Spins = dto.Spins ?? throw new InvalidDataException("FreeSpinBonus missing Spins."),
            },
            "credit_bonus" => new CreditBonus
            {
                Id = dto.Id,
                Reason = dto.Reason ?? throw new InvalidDataException("CreditBonus missing Reason."),
                Credits = dto.Credits ?? throw new InvalidDataException("CreditBonus missing Credits."),
            },
            "spin" => dto.Spin is not null
                ? SpinMapper.FromDto(dto.Spin) ?? throw new InvalidDataException("Spin entry could not be mapped.")
                : throw new InvalidDataException("Spin entry missing payload."),
            _ => throw new InvalidDataException($"Unknown item kind '{dto.Kind}'."),
        };
    }

    private sealed record StoreEnvelopeDto(string? Format, List<StoredItemDto>? Items);

    private sealed class StoredItemDto
    {
        public string? Kind { get; init; }
        public string? Id { get; init; }
        public string? Reason { get; init; }
        public int? Spins { get; init; }
        public int? Credits { get; init; }
        public SpinDto? Spin { get; init; }
    }
}
