using AutoMapper;
using CineMatch.Application.Features.WatchParties.Commands.CreateWatchParty;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.WatchParties.Commands.CreateWatchParty
{
    [TestFixture]
    public class CreateWatchPartyCommandHandlerTests
    {
        private ICurrentUserService _currentUserServiceMock;
        private IJoinCodeGenerator _joinCodeGeneratorMock;
        private IWatchPartyRepository _watchPartyRepositoryMock;
        private IMapper _mapperMock;
        private IUnitOfWork _unitOfWorkMock;
        private CreateWatchPartyCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _currentUserServiceMock = Substitute.For<ICurrentUserService>();
            _joinCodeGeneratorMock = Substitute.For<IJoinCodeGenerator>();
            _watchPartyRepositoryMock = Substitute.For<IWatchPartyRepository>();
            _mapperMock = Substitute.For<IMapper>();
            _unitOfWorkMock = Substitute.For<IUnitOfWork>();

            _handler = new CreateWatchPartyCommandHandler(
                _currentUserServiceMock,
                _joinCodeGeneratorMock,
                _watchPartyRepositoryMock,
                _mapperMock,
                _unitOfWorkMock);
        }

        [Test]
        public async Task Handle_AuthenticatedUser_ReturnsWatchPartyDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var generatedJoinCode = "ABC234";
            var expectedDto = new WatchPartyDto(
                Guid.NewGuid(),
                generatedJoinCode,
                "testuser",
                "popular",
                true,
                DateTime.UtcNow,
                1);

            // Setup: inloggad user, lyckad kodgenerering, reload returnerar något, mapper returnerar dto
            _currentUserServiceMock.UserId.Returns(userId);
            _joinCodeGeneratorMock.GenerateUniqueCodeAsync(Arg.Any<CancellationToken>()).Returns(generatedJoinCode);
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new WatchParty());
            _mapperMock.Map<WatchPartyDto>(Arg.Any<WatchParty>()).Returns(expectedDto);

            // Act
            var result = await _handler.Handle(new CreateWatchPartyCommand(), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False, "Resultatet borde vara framgångsrikt.");
            Assert.That(result.Value, Is.EqualTo(expectedDto), "Returnerad DTO matchar inte den förväntade.");
        }

        [Test]
        public async Task Handle_NoUser_ReturnsUnauthorizedError()
        {
            // Arrange
            // Setup: ingen inloggad user
            _currentUserServiceMock.UserId.Returns((Guid?)null);

            // Act
            var result = await _handler.Handle(new CreateWatchPartyCommand(), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True, "Resultatet borde vara ett fel.");
            Assert.That(result.FirstError, Is.EqualTo(WatchPartyErrors.Unauthorized), "Felet ska vara Unauthorized.");

            // Verifiera early return — ingen kodgenerering, inga repo-anrop, inget save
            await _joinCodeGeneratorMock.DidNotReceive().GenerateUniqueCodeAsync(Arg.Any<CancellationToken>());
            await _watchPartyRepositoryMock.DidNotReceive().AddAsync(Arg.Any<WatchParty>());
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_ValidCommand_CreatesPartyMemberForHost()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _currentUserServiceMock.UserId.Returns(userId);
            _joinCodeGeneratorMock.GenerateUniqueCodeAsync(Arg.Any<CancellationToken>()).Returns("ABC234");
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new WatchParty());

            // Act
            await _handler.Handle(new CreateWatchPartyCommand(), CancellationToken.None);

            // Assert
            // Verifiera att partyt sparats med exakt en aktiv member som matchar host
            await _watchPartyRepositoryMock.Received(1).AddAsync(Arg.Is<WatchParty>(wp =>
                wp.HostId == userId &&
                wp.PartyMembers.Count == 1 &&
                wp.PartyMembers.First().UserId == userId &&
                wp.PartyMembers.First().IsActive == true));

            // Verifiera att transaktionen committeras
            await _unitOfWorkMock.Received(1).SaveChangesAsync();
        }

        [Test]
        public async Task Handle_ValidCommand_GeneratesUniqueJoinCode()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var generatedCode = "ZK7N3M";

            _currentUserServiceMock.UserId.Returns(userId);
            _joinCodeGeneratorMock.GenerateUniqueCodeAsync(Arg.Any<CancellationToken>()).Returns(generatedCode);
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new WatchParty());

            // Act
            await _handler.Handle(new CreateWatchPartyCommand(), CancellationToken.None);

            // Assert
            // Verifiera att generatorn anropats exakt en gång
            await _joinCodeGeneratorMock.Received(1).GenerateUniqueCodeAsync(Arg.Any<CancellationToken>());

            // Verifiera att den genererade koden faktiskt hamnar på partyt som sparas
            await _watchPartyRepositoryMock.Received(1).AddAsync(Arg.Is<WatchParty>(wp =>
                wp.JoinCode == generatedCode));
        }
    }
}
