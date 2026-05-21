using AutoMapper;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Features.WatchParties.Queries.GetWatchPartyDetails;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.WatchParties.Queries.GetWatchPartyDetails
{
    [TestFixture]
    public class GetWatchPartyDetailsQueryHandlerTests
    {
        private ICurrentUserService _currentUserServiceMock;
        private IMapper _mapperMock;
        private IWatchPartyRepository _watchPartyRepositoryMock;
        private GetWatchPartyDetailsQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _currentUserServiceMock = Substitute.For<ICurrentUserService>();
            _mapperMock = Substitute.For<IMapper>();
            _watchPartyRepositoryMock = Substitute.For<IWatchPartyRepository>();

            _handler = new GetWatchPartyDetailsQueryHandler(
                _currentUserServiceMock,
                _mapperMock,
                _watchPartyRepositoryMock);
        }

        [Test]
        public async Task Handle_MemberRequests_ReturnsDetailsWithMembers()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var partyId = Guid.NewGuid();

            var party = new WatchParty
            {
                Id = partyId,
                HostId = userId,
                IsActive = true,
                PartyMembers = new List<PartyMember>
                {
                    new PartyMember { Id = Guid.NewGuid(), UserId = userId, IsActive = true }
                }
            };

            var expectedDto = new WatchPartyDetailsDto(
                partyId, "ABC234", "host", "popular", true, false, DateTime.UtcNow, 1,
                new List<PartyMemberDto>());

            _currentUserServiceMock.UserId.Returns(userId);
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>())
                .Returns(party);
            _mapperMock.Map<WatchPartyDetailsDto>(party).Returns(expectedDto);

            // Act
            var result = await _handler.Handle(new GetWatchPartyDetailsQuery(partyId), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.False, "Aktiv medlem ska få tillbaka details.");
            Assert.That(result.Value, Is.EqualTo(expectedDto));
        }

        [Test]
        public async Task Handle_NotMember_ReturnsUserNotMemberError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var partyId = Guid.NewGuid();

            // Setup: partyt finns men user är inte med i medlemslistan
            var party = new WatchParty
            {
                Id = partyId,
                HostId = Guid.NewGuid(),
                IsActive = true,
                PartyMembers = new List<PartyMember>
                {
                    new PartyMember { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IsActive = true }
                }
            };

            _currentUserServiceMock.UserId.Returns(userId);
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>())
                .Returns(party);

            // Act
            var result = await _handler.Handle(new GetWatchPartyDetailsQuery(partyId), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True);
            Assert.That(result.FirstError, Is.EqualTo(WatchPartyErrors.UserNotMember),
                "Icke-medlem ska få UserNotMember.");

            // Vi ska inte mappa något när access nekas
            _mapperMock.DidNotReceive().Map<WatchPartyDetailsDto>(Arg.Any<WatchParty>());
        }

        [Test]
        public async Task Handle_PartyNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var partyId = Guid.NewGuid();

            _currentUserServiceMock.UserId.Returns(userId);

            // Setup: repo hittar inget party
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(partyId, Arg.Any<CancellationToken>())
                .Returns((WatchParty?)null);

            // Act
            var result = await _handler.Handle(new GetWatchPartyDetailsQuery(partyId), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True);
            Assert.That(result.FirstError, Is.EqualTo(WatchPartyErrors.NotFound));

            // Early return — ingen mapping
            _mapperMock.DidNotReceive().Map<WatchPartyDetailsDto>(Arg.Any<WatchParty>());
        }
    }
}
