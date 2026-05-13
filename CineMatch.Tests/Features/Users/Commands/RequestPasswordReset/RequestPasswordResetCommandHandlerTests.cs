using CineMatch.Application.Features.Users.Commands.RequestPasswordReset;
using CineMatch.Application.Features.Users.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.Users.Commands.RequestPasswordReset
{
    [TestFixture]
    public class RequestPasswordResetCommandHandlerTests
    {
        private IUserRepository _userRepositoryMock;
        private IPasswordResetTokenRepository _passwordResetTokenRepositoryMock;
        private IUnitOfWork _unitOfWorkMock;
        private RequestPasswordResetCommandHandler _handler;

        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly User ExistingUser = new User
        {
            Id = UserId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
        };

        [SetUp]
        public void SetUp()
        {
            _userRepositoryMock = Substitute.For<IUserRepository>();
            _passwordResetTokenRepositoryMock = Substitute.For<IPasswordResetTokenRepository>();
            _unitOfWorkMock = Substitute.For<IUnitOfWork>();

            _handler = new RequestPasswordResetCommandHandler(
                _userRepositoryMock,
                _passwordResetTokenRepositoryMock,
                _unitOfWorkMock);

            _userRepositoryMock
                .GetByEmailAsync(ExistingUser.Email, Arg.Any<CancellationToken>())
                .Returns(ExistingUser);
        }

        [Test]
        public async Task Handle_UnknownEmail_ReturnsNotFoundError()
        {
            // Arrange
            _userRepositoryMock
                .GetByEmailAsync("nobody@example.com", Arg.Any<CancellationToken>())
                .Returns((User?)null);

            var command = new RequestPasswordResetCommand("nobody@example.com");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True);
            Assert.That(result.FirstError, Is.EqualTo(UserErrors.NotFound));

            await _passwordResetTokenRepositoryMock.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>());
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_ValidEmail_SavesTokenAndReturnsRawToken()
        {
            // Arrange
            var command = new RequestPasswordResetCommand(ExistingUser.Email);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False);
            Assert.That(result.Value, Is.Not.Null.And.Not.Empty);

            await _passwordResetTokenRepositoryMock.Received(1).AddAsync(Arg.Is<PasswordResetToken>(t =>
                t.UserId == UserId &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow));

            await _unitOfWorkMock.Received(1).SaveChangesAsync();
        }

        [Test]
        public async Task Handle_ValidEmail_TokenHashIsNotRawToken()
        {
            // Arrange
            var command = new RequestPasswordResetCommand(ExistingUser.Email);
            PasswordResetToken? capturedToken = null;

            await _passwordResetTokenRepositoryMock
                .AddAsync(Arg.Do<PasswordResetToken>(t => capturedToken = t));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert — the stored hash must differ from the raw token returned to the caller
            Assert.That(result.IsError, Is.False);
            Assert.That(capturedToken, Is.Not.Null);
            Assert.That(capturedToken!.TokenHash, Is.Not.EqualTo(result.Value));
        }
    }
}
