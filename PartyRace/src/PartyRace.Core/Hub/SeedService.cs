using System.Security.Cryptography;
using PartyRace.Core.Core;

namespace PartyRace.Core.Hub;

public sealed class SeedService
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string GenerateSharedRandomSeed(int length = 12)
    {
        if (length < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Seed length must be at least 4.");
        }

        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);

        char[] result = new char[length];
        for (int i = 0; i < bytes.Length; i++)
        {
            result[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(result);
    }

    public void Validate(string? seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            throw new PartyRaceException(PartyRaceErrorCode.InvalidSeed, "Seed must not be empty.");
        }

        if (seed.Length > 32)
        {
            throw new PartyRaceException(PartyRaceErrorCode.InvalidSeed, "Seed must be 32 characters or shorter.");
        }

        if (seed.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new PartyRaceException(PartyRaceErrorCode.InvalidSeed, "Seed contains unsupported characters.");
        }
    }
}
