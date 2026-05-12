using AutoMapper;
using CineMatch.Application.Features.Users.Common.Dtos;
using CineMatch.Application.Features.Users.Common.Errors;
using CineMatch.Application.Features.Users.Queries.GetCurrentUser;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Enums;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.Users.Queries.GetCurrentUser
{
    [TestFixture]
    public class GetCurrentUserQueryHandlerTests
    {
        private ICurrentUserService _currentUserServiceMock;
        private IUserRepository _userRepositoryMock;
        private IMapper _mapperMock;
        private GetCurrentUserQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            // 1. Skapa mockar för alla beroenden
            _currentUserServiceMock = Substitute.For<ICurrentUserService>();
            _userRepositoryMock = Substitute.For<IUserRepository>();
            _mapperMock = Substitute.For<IMapper>();

            // 2. Injicera mockarna i handlern
            _handler = new GetCurrentUserQueryHandler(
                _currentUserServiceMock,
                _userRepositoryMock,
                _mapperMock);
        }

        [Test]
        public async Task Handle_AuthenticatedUser_ReturnsUserDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = "hashed_password",
                Role = UserRole.User,
                IsEmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var expectedDto = new UserDto(
                    Guid.NewGuid(),
                    user.Username,
                    user.Email,
                    UserRole.User,
                    DateTime.UtcNow
                );

            // Setup: Inloggad user finns, repository hittar user, mapper mappar
            _currentUserServiceMock.UserId.Returns(userId);
            _userRepositoryMock.GetByIdAsync(userId).Returns(user);
            _mapperMock.Map<UserDto>(user).Returns(expectedDto);

            // Act
            var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False, "Resultatet borde inte innehålla ett fel.");
            Assert.That(result.Value, Is.EqualTo(expectedDto), "Resultatet matchar inte den förväntade DTO:n.");
        }

        [Test]
        public async Task Handle_NoUserId_ReturnsError()
        {
            // Arrange
            // Setup: Ingen inloggad user — UserId returnerar null
            _currentUserServiceMock.UserId.Returns((Guid?)null);

            // Act
            var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True, "Resultatet borde vara ett fel.");
            Assert.That(result.FirstError, Is.EqualTo(UserErrors.InvalidCredentials), "Felet måste vara InvalidCredentials.");

            // Verifiera att vi avbryter tidigt — repository och mapper ska inte anropas
            await _userRepositoryMock.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
            _mapperMock.DidNotReceive().Map<UserDto>(Arg.Any<User>());
        }

        [Test]
        public async Task Handle_UserNotFoundInDatabase_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Setup: UserId finns i token, men user finns inte i databasen
            _currentUserServiceMock.UserId.Returns(userId);
            _userRepositoryMock.GetByIdAsync(userId).Returns((User?)null);

            // Act
            var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True, "Resultatet borde vara ett fel.");
            Assert.That(result.FirstError, Is.EqualTo(UserErrors.NotFound), "Felet måste vara NotFound.");

            // Verifiera att vi inte mappar när user inte finns
            _mapperMock.DidNotReceive().Map<UserDto>(Arg.Any<User>());
        }
    }
}