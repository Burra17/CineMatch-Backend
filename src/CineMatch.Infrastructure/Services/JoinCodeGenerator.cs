using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CineMatch.Application.Interfaces;

namespace CineMatch.Infrastructure.Services
{
    public class JoinCodeGenerator : IJoinCodeGenerator
    {
        private readonly IWatchPartyRepository _watchPartyRepository;

        // Specifikt alfabet utan förväxlingsbara tecken (O/0, I/1/l)
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int CodeLength = 6;
        private const int MaxAttempts = 10;

        // Injicera repositoryt för att kunna anropa IsJoinCodeUniqueAsync
        public JoinCodeGenerator(IWatchPartyRepository watchPartyRepository)
        {
            _watchPartyRepository = watchPartyRepository;
        }

        public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                // Generera en ny 6-teckens kod
                string candidateCode = GenerateRandomCode();

                // Garantera uniqueness mot aktiva parties
                bool isUnique = await _watchPartyRepository.IsJoinCodeUniqueAsync(candidateCode, cancellationToken);

                if (isUnique)
                {
                    return candidateCode;
                }
            }

            // Misslyckas tydligt om uniqueness inte kan garanteras efter 10 försök
            throw new InvalidOperationException(
                $"Kunde inte generera en unik join-kod för Watch Party efter {MaxAttempts} försök. Systemet kan vara under tung belastning eller ha slut på unika koder.");
        }

        private string GenerateRandomCode()
        {
            // Vi använder en array för att bygga upp strängen, vilket är minneseffektivt
            char[] codeChars = new char[CodeLength];

            for (int i = 0; i < CodeLength; i++)
            {
                // Använd kryptografisk randomization (RandomNumberGenerator.GetInt32)
                // Detta är mycket säkrare och ger en jämnare spridning än klassiska Random
                int randomIndex = RandomNumberGenerator.GetInt32(Alphabet.Length);
                codeChars[i] = Alphabet[randomIndex];
            }

            return new string(codeChars);
        }
    }
}
