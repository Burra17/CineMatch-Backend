using CineMatch.Application.Features.WatchParties.Commands.LeaveWatchParty;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.WatchParties.Commands.LeaveWatchParty
{
    [TestFixture]
    public class LeaveWatchPartyCommandHandlerTests
    {
        private ICurrentUserService _currentUserServiceMock;
        private IPartyMemberRepository _partyMemberRepositoryMock;
        private IWatchPartyRepository _watchPartyRepositoryMock;
        private IUnitOfWork _unitOfWorkMock;
        private LeaveWatchPartyCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _currentUserServiceMock = Substitute.For<ICurrentUserService>();
            _partyMemberRepositoryMock = Substitute.For<IPartyMemberRepository>();
            _watchPartyRepositoryMock = Substitute.For<IWatchPartyRepository>();
            _unitOfWorkMock = Substitute.For<IUnitOfWork>();

            _handler = new LeaveWatchPartyCommandHandler(
                _currentUserServiceMock,
                _partyMemberRepositoryMock,
                _watchPartyRepositoryMock,
                _unitOfWorkMock);
        }

        [Test]
        public async Task Handle_ActiveMember_SetsLeftAtAndDeactivates()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var partyId = Guid.NewGuid();

            var membership = new PartyMember
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WatchPartyId = partyId,
                IsActive = true
            };

            // Party har två aktiva medlemmar — user är inte host, så leave ska gå igenom utan att stänga partyt
            var party = new WatchParty
            {
                Id = partyId,
                HostId = Guid.NewGuid(),
                IsActive = true,
                PartyMembers = new List<PartyMember>
                {
                    membership,
                    new PartyMember { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IsActive = true }
                }
            };

            _currentUserServiceMock.UserId.Returns(userId);
            _watchPartyRepositoryMock.GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(party);
            _partyMemberRepositoryMock
                .GetMembershipAsync(userId, partyId, Arg.Any<CancellationToken>())
                .Returns(membership);

            // Act
            var result = await _handler.Handle(new LeaveWatchPartyCommand(partyId), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False, "Leave ska lyckas.");
            Assert.That(membership.IsActive, Is.False, "Medlemskapet ska soft-deletas.");
            Assert.That(membership.LeftAt, Is.Not.Null, "LeftAt ska sättas.");

            // Partyt ska INTE stängas eftersom det fanns fler aktiva medlemmar kvar
            Assert.That(party.IsActive, Is.True, "Partyt ska fortfarande vara aktivt.");
            Assert.That(party.ClosedAt, Is.Null, "Partyt ska inte ha ClosedAt satt.");

            await _unitOfWorkMock.Received(1).SaveChangesAsync();
        }

        [Test]
        public async Task Handle_NotMember_ReturnsUserNotMemberError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var partyId = Guid.NewGuid();
            var party = new WatchParty
            {
                Id = partyId,
                HostId = Guid.NewGuid(),
                IsActive = true,
                PartyMembers = new List<PartyMember>()
            };

            _currentUserServiceMock.UserId.Returns(userId);
            _watchPartyRepositoryMock.GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(party);

            // Setup: ingen membership alls för denna user
            _partyMemberRepositoryMock
                .GetMembershipAsync(userId, partyId, Arg.Any<CancellationToken>())
                .Returns((PartyMember?)null);

            // Act
            var result = await _handler.Handle(new LeaveWatchPartyCommand(partyId), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True);
            Assert.That(result.FirstError, Is.EqualTo(WatchPartyErrors.UserNotMember));

            // Ingen save eftersom vi avbröt
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_HostWithOtherMembers_ReturnsHostCannotLeaveError()
        {
            // Arrange
            var hostId = Guid.NewGuid();
            var partyId = Guid.NewGuid();

            var hostMembership = new PartyMember
            {
                Id = Guid.NewGuid(),
                UserId = hostId,
                WatchPartyId = partyId,
                IsActive = true
            };

            // Host + en till aktiv medlem — host får inte lämna
            var party = new WatchParty
            {
                Id = partyId,
                HostId = hostId,
                IsActive = true,
                PartyMembers = new List<PartyMember>
                {
                    hostMembership,
                    new PartyMember { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IsActive = true }
                }
            };

            _currentUserServiceMock.UserId.Returns(hostId);
            _watchPartyRepositoryMock.GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(party);
            _partyMemberRepositoryMock
                .GetMembershipAsync(hostId, partyId, Arg.Any<CancellationToken>())
                .Returns(hostMembership);

            // Act
            var result = await _handler.Handle(new LeaveWatchPartyCommand(partyId), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True);
            Assert.That(result.FirstError, Is.EqualTo(WatchPartyErrors.HostCannotLeave));

            // Verifiera att inget muteras eller sparas vid host-blockering
            Assert.That(hostMembership.IsActive, Is.True, "Host-medlemskapet ska inte deaktiveras.");
            Assert.That(hostMembership.LeftAt, Is.Null, "LeftAt ska inte sättas på host.");
            await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
        }

        [Test]
        public async Task Handle_LastMember_DeactivatesParty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var partyId = Guid.NewGuid();

            var membership = new PartyMember
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WatchPartyId = partyId,
                IsActive = true
            };

            // Bara user själv är aktiv — när hen lämnar ska partyt stängas
            var party = new WatchParty
            {
                Id = partyId,
                HostId = userId,
                IsActive = true,
                PartyMembers = new List<PartyMember> { membership }
            };

            _currentUserServiceMock.UserId.Returns(userId);
            _watchPartyRepositoryMock.GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(party);
            _partyMemberRepositoryMock
                .GetMembershipAsync(userId, partyId, Arg.Any<CancellationToken>())
                .Returns(membership);

            // Act
            var result = await _handler.Handle(new LeaveWatchPartyCommand(partyId), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False);
            Assert.That(membership.IsActive, Is.False, "Medlemskapet ska soft-deletas.");
            Assert.That(party.IsActive, Is.False, "Partyt ska stängas när sista medlemmen lämnar.");
            Assert.That(party.ClosedAt, Is.Not.Null, "ClosedAt ska sättas.");

            await _unitOfWorkMock.Received(1).SaveChangesAsync();
        }
    }
}
