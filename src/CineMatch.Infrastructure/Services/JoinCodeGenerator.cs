using System.Security.Cryptography;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;

namespace CineMatch.Infrastructure.Services;

public class JoinCodeGenerator : IJoinCodeGenerator
{
    private readonly IWatchPartyRepository _watchPartyRepository;

    // Skips visually ambiguous characters (O/0, I/1/l) so users typing a code don't mistake one for another.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;
    private const int MaxAttempts = 10;

    public JoinCodeGenerator(IWatchPartyRepository watchPartyRepository)
    {
        _watchPartyRepository = watchPartyRepository;
    }

    public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            string candidateCode = GenerateRandomCode();

            // Uniqueness is enforced against active parties only (matches the filtered unique index).
            bool isUnique = await _watchPartyRepository.IsJoinCodeUniqueAsync(candidateCode, cancellationToken);

            if (isUnique)
            {
                return candidateCode;
            }
        }

        throw new InvalidOperationException(
            $"Could not generate a unique join code after {MaxAttempts} attempts. " +
            "The system may be under heavy load or running out of unique codes.");
    }

    private string GenerateRandomCode()
    {
        char[] codeChars = new char[CodeLength];

        for (int i = 0; i < CodeLength; i++)
        {
            int randomIndex = RandomNumberGenerator.GetInt32(Alphabet.Length);
            codeChars[i] = Alphabet[randomIndex];
        }

        return new string(codeChars);
    }
}
