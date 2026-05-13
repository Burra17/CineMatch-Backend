using AutoMapper;
using CineMatch.Application.Features.Matches.Common.Dtos;
using CineMatch.Application.Features.Matches.Common.Errors;
using CineMatch.Application.Features.Matches.Queries.GetMatchesByParty;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.Matches.Queries.GetMatchesByParty;

[TestFixture]
public class GetMatchesByPartyQueryHandlerTests
{
    private ICurrentUserService _currentUserServiceMock;
    private IPartyMemberRepository _partyMemberRepositoryMock;
    private IMatchRepository _matchRepositoryMock;
    private IMapper _mapperMock;
    private GetMatchesByPartyQueryHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PartyId = Guid.NewGuid();

    private static readonly PartyMember ActiveMember = new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        WatchPartyId = PartyId,
        IsActive = true
    };

    [SetUp]
    public void SetUp()
    {
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();
        _partyMemberRepositoryMock = Substitute.For<IPartyMemberRepository>();
        _matchRepositoryMock = Substitute.For<IMatchRepository>();
        _mapperMock = Substitute.For<IMapper>();

        _currentUserServiceMock.UserId.Returns(UserId);
        _partyMemberRepositoryMock
            .GetMembershipAsync(UserId, PartyId, Arg.Any<CancellationToken>())
            .Returns(ActiveMember);
        _mapperMock
            .Map<MatchDto>(Arg.Any<Match>())
            .Returns(call =>
            {
                var m = call.Arg<Match>();
                return new MatchDto(m.Id, m.WatchPartyId, m.MovieId, m.MatchedAt, m.IsWatched, null, null);
            });

        _handler = new GetMatchesByPartyQueryHandler(
            _currentUserServiceMock,
            _partyMemberRepositoryMock,
            _matchRepositoryMock,
            _mapperMock);
    }

    [Test]
    public async Task Handle_NotMember_ReturnsForbidden()
    {
        // Arrange
        _partyMemberRepositoryMock
            .GetMembershipAsync(UserId, PartyId, Arg.Any<CancellationToken>())
            .Returns((PartyMember?)null);

        // Act
        var result = await _handler.Handle(new GetMatchesByPartyQuery(PartyId), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError, Is.EqualTo(MatchErrors.NotMemberOfParty));
    }

    [Test]
    public async Task Handle_Valid_ReturnsMatchesOrderedByMatchedAtDesc()
    {
        // Arrange — repository already returns sorted list (sorted by MatchedAt desc in MatchRepository)
        var now = DateTime.UtcNow;
        var matches = new List<Match>
        {
            new() { Id = Guid.NewGuid(), WatchPartyId = PartyId, MovieId = Guid.NewGuid(), MatchedAt = now },
            new() { Id = Guid.NewGuid(), WatchPartyId = PartyId, MovieId = Guid.NewGuid(), MatchedAt = now.AddMinutes(-5) },
        };
        _matchRepositoryMock
            .GetByPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(matches);

        // Act
        var result = await _handler.Handle(new GetMatchesByPartyQuery(PartyId), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value.Count, Is.EqualTo(2));
        Assert.That(result.Value[0].Id, Is.EqualTo(matches[0].Id));
        Assert.That(result.Value[1].Id, Is.EqualTo(matches[1].Id));
    }
}
