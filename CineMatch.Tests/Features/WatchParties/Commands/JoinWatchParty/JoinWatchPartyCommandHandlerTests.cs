using AutoMapper;
using CineMatch.Application.Features.WatchParties.Commands.JoinWatchParty;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.WatchParties.Commands.JoinWatchParty
{
    [TestFixture]
    public class JoinWatchPartyCommandHandlerTests
    {
        private IUnitOfWork _unitOfWorkMock;
        private IMapper _mapperMock;
        private IPartyMemberRepository _partyMemberRepositoryMock;
        private ICurrentUserService _currentUserServiceMock;
        private IWatchPartyRepository _watchPartyRepositoryMock;
        private JoinWatchPartyCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = Substitute.For<IUnitOfWork>();
            _mapperMock = Substitute.For<IMapper>();
            _partyMemberRepositoryMock = Substitute.For<IPartyMemberRepository>();
            _currentUserServiceMock = Substitute.For<ICurrentUserService>();
            _watchPartyRepositoryMock = Substitute.For<IWatchPartyRepository>();

            _handler = new JoinWatchPartyCommandHandler(
                _unitOfWorkMock,
                _mapperMock,
                _partyMemberRepositoryMock,
                _currentUserServiceMock,
                _watchPartyRepositoryMock);
        }

        [Test]
        public async Task Handle_ValidJoinCode_AddsUserAsPartyMember()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var partyId = Guid.NewGuid();
            var joinCode = "ABC234";
            var party = new WatchParty { Id = partyId, JoinCode = joinCode, IsActive = true };
            var expectedDto = new WatchPartyDto(partyId, joinCode, "host", "popular", true, DateTime.UtcNow, 2);

            // Setup: giltig user, party hittas, ingen tidigare membership → ska skapa ny
            _currentUserServiceMock.UserId.Returns(userId);
            _watchPartyRepositoryMock.GetByJoinCodeAsync(joinCode, Arg.Any<CancellationToken>()).Returns(party);
            _partyMemberRepositoryMock
                .GetMembershipAsync(userId, partyId, Arg.Any<CancellationToken>())
                .Returns((PartyMember?)null);
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>())
                .Returns(party);
            _mapperMock.Map<WatchPartyDto>(Arg.Any<WatchParty>()).Returns(expectedDto);

            // Act
            var result = await _handler.Handle(new JoinWatchPartyCommand(joinCode), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False, "Resultatet borde vara framgångsrikt.");

            // Verifiera att en ny aktiv PartyMember adderas för rätt user och party
            await _partyMemberRepositoryMock.Received(1).AddAsync(Arg.Is<PartyMember>(pm =>
                pm.UserId == userId &&
                pm.WatchPartyId == partyId &&
                pm.IsActive == true));

            await _unitOfWorkMock.Received(1).SaveChangesAsync();
        }

        [Test]
        public async Task Handle_InvalidJoinCode_ReturnsJoinCodeNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.UserId.Returns(userId);

            // Setup: repo hittar ingen party med den koden
            _watchPartyRepositoryMock
                .GetByJoinCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((WatchParty?)null);

            // Act
            var result = await _handler.Handle(new JoinWatchPartyCommand("NOTREAL"), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True, "Resultatet borde vara ett fel.");
            Assert.That(result.FirstError, Is.EqualTo(WatchPartyErrors.JoinCodeNotFound), "Felet ska vara JoinCodeNotFound.");

            // Early return — inga member-anrop, inget save
            await _partyMemberRepositoryMock.DidNotReceive()
                .GetMembershipAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
            await _partyMemberRepositoryMock.DidNotReceive().AddAsync(Arg.Any<PartyMember>());
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_InactiveParty_ReturnsJoinCodeNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.UserId.Returns(userId);

            // Setup: GetByJoinCodeAsync filtrerar bort inaktiva parties och returnerar null,
            // så inaktiva parties ser ut precis som okända koder för handlern
            _watchPartyRepositoryMock
                .GetByJoinCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((WatchParty?)null);

            // Act
            var result = await _handler.Handle(new JoinWatchPartyCommand("CLOSED"), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True);
            Assert.That(result.FirstError, Is.EqualTo(WatchPartyErrors.JoinCodeNotFound),
                "Inaktiva parties ska inte gå att joina och resultera i JoinCodeNotFound.");
        }

        [Test]
        public async Task Handle_UserAlreadyMember_ReturnsExistingPartyDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var partyId = Guid.NewGuid();
            var joinCode = "ABC234";
            var party = new WatchParty { Id = partyId, JoinCode = joinCode, IsActive = true };
            var existingMembership = new PartyMember
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WatchPartyId = partyId,
                IsActive = true
            };
            var expectedDto = new WatchPartyDto(partyId, joinCode, "host", "popular", true, DateTime.UtcNow, 2);

            _currentUserServiceMock.UserId.Returns(userId);
            _watchPartyRepositoryMock.GetByJoinCodeAsync(joinCode, Arg.Any<CancellationToken>()).Returns(party);
            _partyMemberRepositoryMock
                .GetMembershipAsync(userId, partyId, Arg.Any<CancellationToken>())
                .Returns(existingMembership);
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>())
                .Returns(party);
            _mapperMock.Map<WatchPartyDto>(Arg.Any<WatchParty>()).Returns(expectedDto);

            // Act
            var result = await _handler.Handle(new JoinWatchPartyCommand(joinCode), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False);
            Assert.That(result.Value, Is.EqualTo(expectedDto), "Returnerar befintlig party utan att skapa ny membership.");

            // Verifiera idempotens — ingen ny medlem, inget save
            await _partyMemberRepositoryMock.DidNotReceive().AddAsync(Arg.Any<PartyMember>());
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_UserPreviouslyLeft_ReactivatesMembership()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var partyId = Guid.NewGuid();
            var joinCode = "ABC234";
            var party = new WatchParty { Id = partyId, JoinCode = joinCode, IsActive = true };
            var previousMembership = new PartyMember
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WatchPartyId = partyId,
                IsActive = false,
                LeftAt = DateTime.UtcNow.AddHours(-1)
            };

            _currentUserServiceMock.UserId.Returns(userId);
            _watchPartyRepositoryMock.GetByJoinCodeAsync(joinCode, Arg.Any<CancellationToken>()).Returns(party);
            _partyMemberRepositoryMock
                .GetMembershipAsync(userId, partyId, Arg.Any<CancellationToken>())
                .Returns(previousMembership);
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>())
                .Returns(party);

            // Act
            await _handler.Handle(new JoinWatchPartyCommand(joinCode), CancellationToken.None);

            // Assert
            Assert.That(previousMembership.IsActive, Is.True, "Membership ska reaktiveras.");
            Assert.That(previousMembership.LeftAt, Is.Null, "LeftAt ska nollställas vid reaktivering.");

            // Verifiera att vi reaktiverar istället för att skapa ny
            await _partyMemberRepositoryMock.DidNotReceive().AddAsync(Arg.Any<PartyMember>());
            await _unitOfWorkMock.Received(1).SaveChangesAsync();
        }
    }
}
