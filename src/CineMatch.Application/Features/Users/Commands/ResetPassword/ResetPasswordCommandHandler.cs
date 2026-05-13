using CineMatch.Application.Features.Users.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using ErrorOr;
using MediatR;
using System.Security.Cryptography;

namespace CineMatch.Application.Features.Users.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ErrorOr<Success>>
{
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<Success>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeTokenHash(request.Token);
        if (tokenHash is null)
            return PasswordResetErrors.InvalidOrExpiredToken;

        var resetToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (resetToken is null || resetToken.ExpiresAt < DateTime.UtcNow || resetToken.IsUsed)
            return PasswordResetErrors.InvalidOrExpiredToken;

        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user is null)
            return PasswordResetErrors.InvalidOrExpiredToken;

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        _passwordResetTokenRepository.Update(resetToken);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success;
    }

    // Returns null if the incoming token string is not valid Base64, which means the token is invalid.
    private static string? ComputeTokenHash(string rawToken)
    {
        try
        {
            var bytes = Convert.FromBase64String(rawToken);
            return Convert.ToHexString(SHA256.HashData(bytes));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
