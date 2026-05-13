using CineMatch.Application.Features.Users.Commands.ResetPassword;
using CineMatch.Application.Features.Users.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using NSubstitute;
using System.Security.Cryptography;

namespace CineMatch.Tests.Features.Users.Commands.ResetPassword
{
    [TestFixture]
    public class ResetPasswordCommandHandlerTests
    {
        private IPasswordResetTokenRepository _passwordResetTokenRepositoryMock;
        private IUserRepository _userRepositoryMock;
        private IPasswordHasher _passwordHasherMock;
        private IUnitOfWork _unitOfWorkMock;
        private ResetPasswordCommandHandler _handler;

        private static readonly Guid UserId = Guid.NewGuid();

        // A real token/hash pair so tests mirror the actual handler logic.
        private static readonly byte[] RawTokenBytes = RandomNumberGenerator.GetBytes(32);
        private static readonly string ValidRawToken = Convert.ToBase64String(RawTokenBytes);
        private static readonly string ValidTokenHash = Convert.ToHexString(SHA256.HashData(RawTokenBytes));

        private static readonly User ExistingUser = new User
        {
            Id = UserId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "old_hash",
        };

        private static readonly PasswordResetToken ValidResetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            TokenHash = ValidTokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
        };

        [SetUp]
        public void SetUp()
        {
            _passwordResetTokenRepositoryMock = Substitute.For<IPasswordResetTokenRepository>();
            _userRepositoryMock = Substitute.For<IUserRepository>();
            _passwordHasherMock = Substitute.For<IPasswordHasher>();
            _unitOfWorkMock = Substitute.For<IUnitOfWork>();

            _handler = new ResetPasswordCommandHandler(
                _passwordResetTokenRepositoryMock,
                _userRepositoryMock,
                _passwordHasherMock,
                _unitOfWorkMock);

            // Default happy-path setup
            _passwordResetTokenRepositoryMock
                .GetByTokenHashAsync(ValidTokenHash, Arg.Any<CancellationToken>())
                .Returns(ValidResetToken);

            _userRepositoryMock
                .GetByIdAsync(UserId)
                .Returns(ExistingUser);

            _passwordHasherMock
                .HashPassword(Arg.Any<string>())
                .Returns("new_hash");
        }

        [Test]
        public async Task Handle_InvalidToken_ReturnsError()
        {
            // Arrange — token not found in DB
            _passwordResetTokenRepositoryMock
                .GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((PasswordResetToken?)null);

            var command = new ResetPasswordCommand(ValidRawToken, "NewPassword123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True);
            Assert.That(result.FirstError, Is.EqualTo(PasswordResetErrors.InvalidOrExpiredToken));
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_ExpiredToken_ReturnsError()
        {
            // Arrange — same hash, but token expired
            var expiredToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                TokenHash = ValidTokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
            };

            _passwordResetTokenRepositoryMock
                .GetByTokenHashAsync(ValidTokenHash, Arg.Any<CancellationToken>())
                .Returns(expiredToken);

            var command = new ResetPasswordCommand(ValidRawToken, "NewPassword123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True);
            Assert.That(result.FirstError, Is.EqualTo(PasswordResetErrors.InvalidOrExpiredToken));
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_AlreadyUsedToken_ReturnsError()
        {
            // Arrange — token already consumed
            var usedToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                TokenHash = ValidTokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = true,
                UsedAt = DateTime.UtcNow.AddMinutes(-10),
                CreatedAt = DateTime.UtcNow.AddHours(-1),
            };

            _passwordResetTokenRepositoryMock
                .GetByTokenHashAsync(ValidTokenHash, Arg.Any<CancellationToken>())
                .Returns(usedToken);

            var command = new ResetPasswordCommand(ValidRawToken, "NewPassword123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True);
            Assert.That(result.FirstError, Is.EqualTo(PasswordResetErrors.InvalidOrExpiredToken));
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_ValidToken_UpdatesPasswordAndMarksTokenUsed()
        {
            // Arrange
            var command = new ResetPasswordCommand(ValidRawToken, "NewPassword123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert — success
            Assert.That(result.IsError, Is.False);

            // Password was hashed and written to the user
            _passwordHasherMock.Received(1).HashPassword(command.NewPassword);
            Assert.That(ExistingUser.PasswordHash, Is.EqualTo("new_hash"));

            // Token marked as used
            Assert.That(ValidResetToken.IsUsed, Is.True);
            Assert.That(ValidResetToken.UsedAt, Is.Not.Null);

            // Changes persisted
            await _unitOfWorkMock.Received(1).SaveChangesAsync();
        }
    }
}
