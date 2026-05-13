using CineMatch.Application.Features.Users.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Domain.Models;
using ErrorOr;
using MediatR;
using System.Security.Cryptography;

namespace CineMatch.Application.Features.Users.Commands.RequestPasswordReset;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, ErrorOr<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RequestPasswordResetCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<string>> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return UserErrors.NotFound;

        var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(rawTokenBytes);

        // SHA-256 is deterministic so we can look up the token later by hashing the incoming value.
        // BCrypt is unsuitable here because its random salt produces a different hash on each call.
        var tokenHash = Convert.ToHexString(SHA256.HashData(rawTokenBytes));

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return rawToken;
    }
}
