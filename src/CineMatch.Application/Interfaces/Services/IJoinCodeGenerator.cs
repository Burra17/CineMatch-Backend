namespace CineMatch.Application.Interfaces.Services;

public interface IJoinCodeGenerator
{
    Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken);
}
