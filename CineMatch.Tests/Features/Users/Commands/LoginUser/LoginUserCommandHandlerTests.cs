using AutoMapper;
using CineMatch.Application.Features.Users.Commands.LoginUser;
using CineMatch.Application.Features.Users.Common.Dtos;
using CineMatch.Application.Features.Users.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Enums;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.Users.Commands.LoginUser
{
    [TestFixture]
    public class LoginUserCommandHandlerTests
    {
        private IMapper _mapperMock;
        private IPasswordHasher _passwordHasherMock;
        private IJwtService _jwtServiceMock;
        private IUserRepository _userRepositoryMock;
        private LoginUserCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            // 1. Skapa mockar för beroendena
            _mapperMock = Substitute.For<IMapper>();
            _passwordHasherMock = Substitute.For<IPasswordHasher>();
            _jwtServiceMock = Substitute.For<IJwtService>();
            _userRepositoryMock = Substitute.For<IUserRepository>();

            // 2. Injicera dem i handlern
            _handler = new LoginUserCommandHandler(
                _mapperMock,
                _passwordHasherMock,
                _jwtServiceMock,
                _userRepositoryMock);
        }

        [Test]
        public async Task Handle_ValidCredentials_ReturnsTokenAndUser()
        {
            // Arrange
            var command = new LoginUserCommand("test@test.com", "Password123!");

            // Skapa en fiktiv användare som databasen hittar
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = command.Email,
                PasswordHash = "correct_hashed_password",
                Role = UserRole.User
            };

            var expectedToken = "super_secret_jwt_token";
            var expectedUserDto = new UserDto(user.Id, user.Username, user.Email, user.Role, DateTime.UtcNow);

            // Setup: Säg åt mockarna vad de ska göra för att flödet ska lyckas ("Happy Path")
            _userRepositoryMock.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasherMock.VerifyPassword(command.Password, user.PasswordHash).Returns(true);
            _jwtServiceMock.GenerateToken(user).Returns(expectedToken);
            _mapperMock.Map<UserDto>(user).Returns(expectedUserDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False, "Resultatet borde vara framgångsrikt utan fel.");
            Assert.That(result.Value.Token, Is.EqualTo(expectedToken), "JWT-token stämmer inte överens.");
            Assert.That(result.Value.User, Is.EqualTo(expectedUserDto), "UserDto stämmer inte överens.");
        }

        [Test]
        public async Task Handle_UserNotFound_ReturnsInvalidCredentialsError()
        {
            // Arrange
            var command = new LoginUserCommand("nonexistent@test.com", "Password123!");

            // Setup: Databasen hittar ingen användare och returnerar null
            _userRepositoryMock.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
                .Returns((User?)null); // Vi castar till (User) för att NSubstitute ska förstå returtypen korrekt

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True, "Resultatet borde vara ett fel.");
            Assert.That(result.FirstError, Is.EqualTo(UserErrors.InvalidCredentials), "Felet måste vara InvalidCredentials.");

            // Verifiera att vi avbryter direkt (early return) och inte anropar hashern eller jwt-servicen
            _passwordHasherMock.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
            _jwtServiceMock.DidNotReceive().GenerateToken(Arg.Any<User>());
        }

        [Test]
        public async Task Handle_WrongPassword_ReturnsInvalidCredentialsError()
        {
            // Arrange
            var command = new LoginUserCommand("test@test.com", "WrongPassword123!");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                PasswordHash = "correct_hashed_password"
            };

            // Setup: Databasen hittar användaren
            _userRepositoryMock.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);

            // Men lösenordsverifieringen säger att det inte stämmer (Returns(false))
            _passwordHasherMock.VerifyPassword(command.Password, user.PasswordHash).Returns(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True, "Resultatet borde vara ett fel.");
            Assert.That(result.FirstError, Is.EqualTo(UserErrors.InvalidCredentials), "Felet måste vara InvalidCredentials.");

            // Verifiera att vi avbryter innan någon JWT-token skapas
            _jwtServiceMock.DidNotReceive().GenerateToken(Arg.Any<User>());
        }

        [Test]
        public async Task Handle_ValidCredentials_GeneratesJwtToken()
        {
            // Arrange
            var command = new LoginUserCommand("test@test.com", "Password123!");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                PasswordHash = "correct_hashed_password"
            };

            // Setup: Giltigt flöde (Happy Path)
            _userRepositoryMock.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasherMock.VerifyPassword(command.Password, user.PasswordHash).Returns(true);

            // Vi måste returnera någon sträng så att det inte blir null längre ner i handlern
            _jwtServiceMock.GenerateToken(user).Returns("generated_token_string");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Verifiera att GenerateToken anropades exakt en gång och att det var just vår användare som skickades in
            _jwtServiceMock.Received(1).GenerateToken(user);
        }

        [Test]
        public async Task Handle_UserNotFound_DoesNotCallPasswordHasher()
        {
            // Arrange
            var command = new LoginUserCommand("nonexistent@test.com", "Password123!");

            _userRepositoryMock
                .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
                .Returns((User?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Verifiera att vi inte slösar CPU på BCrypt om användaren inte finns
            _passwordHasherMock.DidNotReceive().VerifyPassword(
                Arg.Any<string>(),
                Arg.Any<string>());

            // Och att ingen token genereras
            _jwtServiceMock.DidNotReceive().GenerateToken(Arg.Any<User>());
        }
    }
}
