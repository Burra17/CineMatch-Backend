namespace CineMatch.Application.Interfaces
{
    public interface IJoinCodeGenerator
    {
        Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken);
    }
}
