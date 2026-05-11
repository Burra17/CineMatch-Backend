using AutoMapper;
using CineMatch.Application.Features.Users.Commands.RegisterUser;
using CineMatch.Application.Features.Users.Common.Dtos;
using CineMatch.Application.Features.Users.Common.Errors;
using CineMatch.Application.Interfaces;
using CineMatch.Domain.Enums;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.Users.Commands.RegisterUser
{
    [TestFixture]
    public class RegisterUserCommandHandlerTests
    {
        private IMapper _mapperMock;
        private IUserRepository _userRepositoryMock;
        private IPasswordHasher _passwordHasherMock;
        private IUnitOfWork _unitOfWorkMock;
        private RegisterUserCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            // 1. Skapa mockar för alla beroenden
            _mapperMock = Substitute.For<IMapper>();
            _userRepositoryMock = Substitute.For<IUserRepository>();
            _passwordHasherMock = Substitute.For<IPasswordHasher>();
            _unitOfWorkMock = Substitute.For<IUnitOfWork>();

            // 2. Injicera mockarna i handlern
            _handler = new RegisterUserCommandHandler(
                _mapperMock,
                _userRepositoryMock,
                _passwordHasherMock,
                _unitOfWorkMock);
        }

        [Test]
        public async Task Handle_ValidCommand_ReturnsUserDto()
        {
            // Arrange
            var command = new RegisterUserCommand("testuser", "test@test.com", "Password123!");

            var expectedDto = new UserDto(
                    Guid.NewGuid(),
                    command.Username,
                    command.Email,
                    UserRole.User,
                    DateTime.UtcNow
                );

            // Setup: Säg åt våra mockar vad de ska svara när handlern anropar dem
            _userRepositoryMock.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
            _userRepositoryMock.ExistsByUsernameAsync(command.Username, Arg.Any<CancellationToken>()).Returns(false);
            _passwordHasherMock.HashPassword(command.Password).Returns("hashed_password");

            // När mappern anropas med vilken User-modell som helst, returnera vår expectedDto
            _mapperMock.Map<UserDto>(Arg.Any<User>()).Returns(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False, "Resultatet borde inte innehålla ett fel.");
            Assert.That(result.Value, Is.EqualTo(expectedDto), "Resultatet matchar inte den förväntade DTO:n.");
        }

        [Test]
        public async Task Handle_EmailAlreadyExists_ReturnsEmailAlreadyExistsError()
        {
            // Arrange
            var command = new RegisterUserCommand("testuser", "existing@test.com", "Password123!");

            // Setup: Här säger vi att e-postadressen REDAN finns (Returns(true))
            _userRepositoryMock.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True, "Resultatet borde vara ett fel.");
            Assert.That(result.FirstError, Is.EqualTo(UserErrors.EmailAlreadyExists), "Felet måste vara EmailAlreadyExists.");

            // Verifiera att vi avbryter tidigt och aldrig anropar koden nedanför i handlern
            await _userRepositoryMock.DidNotReceive().ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            _passwordHasherMock.DidNotReceive().HashPassword(Arg.Any<string>());
            await _userRepositoryMock.DidNotReceive().AddAsync(Arg.Any<User>());
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_UsernameAlreadyExists_ReturnsUsernameAlreadyExistsError()
        {
            // Arrange
            var command = new RegisterUserCommand("existinguser", "test@test.com", "Password123!");

            // Setup: E-posten är ledig, men användarnamnet är upptaget
            _userRepositoryMock.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
            _userRepositoryMock.ExistsByUsernameAsync(command.Username, Arg.Any<CancellationToken>()).Returns(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True, "Resultatet borde vara ett fel.");
            Assert.That(result.FirstError, Is.EqualTo(UserErrors.UsernameAlreadyExists), "Felet måste vara UsernameAlreadyExists.");

            // Verifiera att vi avbryter innan lösenordet hashas och användaren sparas
            _passwordHasherMock.DidNotReceive().HashPassword(Arg.Any<string>());
            await _userRepositoryMock.DidNotReceive().AddAsync(Arg.Any<User>());
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_ValidCommand_HashesPassword() // Verifiera att lösen hashas innan save
        {
            // Arrange
            var command = new RegisterUserCommand("testuser", "test@test.com", "Password123!");

            // Setup: Databasen tillåter att vi går vidare
            _userRepositoryMock.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
            _userRepositoryMock.ExistsByUsernameAsync(command.Username, Arg.Any<CancellationToken>()).Returns(false);

            // Vi ser till att hashern returnerar något (så att handlern inte kraschar på null)
            _passwordHasherMock.HashPassword(command.Password).Returns("some_hashed_string");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Verifiera att HashPassword anropades exakt en gång med exakt det lösenord som fanns i kommandot
            _passwordHasherMock.Received(1).HashPassword(command.Password);
        }

        [Test]
        public async Task Handle_ValidCommand_SavesUserToRepository()
        {
            // Arrange
            var command = new RegisterUserCommand("testuser", "test@test.com", "Password123!");
            var expectedHash = "my_super_secret_hash";

            // Setup: Giltigt flöde utan hinder
            _userRepositoryMock.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
            _userRepositoryMock.ExistsByUsernameAsync(command.Username, Arg.Any<CancellationToken>()).Returns(false);
            _passwordHasherMock.HashPassword(command.Password).Returns(expectedHash);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // 1. Verifiera att AddAsync anropas med ett User-objekt där fälten stämmer överens med kommandot
            await _userRepositoryMock.Received(1).AddAsync(Arg.Is<User>(u =>
                u.Username == command.Username &&
                u.Email == command.Email &&
                u.PasswordHash == expectedHash &&
                u.Role == UserRole.User &&
                u.IsEmailConfirmed == true
            ));

            // 2. Verifiera att UnitOfWork anropades för att bekräfta transaktionen till databasen
            await _unitOfWorkMock.Received(1).SaveChangesAsync();
        }
    }
}
