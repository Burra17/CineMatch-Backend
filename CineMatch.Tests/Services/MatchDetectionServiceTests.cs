using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Services;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Services;

[TestFixture]
public class MatchDetectionServiceTests
{
    private ISwipeRepository _swipeRepositoryMock;
    private IMatchRepository _matchRepositoryMock;
    private IPartyMemberRepository _partyMemberRepositoryMock;
    private MatchDetectionService _service;

    private static readonly Guid PartyId = Guid.NewGuid();
    private static readonly Guid MovieId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _swipeRepositoryMock = Substitute.For<ISwipeRepository>();
        _matchRepositoryMock = Substitute.For<IMatchRepository>();
        _partyMemberRepositoryMock = Substitute.For<IPartyMemberRepository>();

        _service = new MatchDetectionService(
            _swipeRepositoryMock,
            _matchRepositoryMock,
            _partyMemberRepositoryMock);
    }

    [Test]
    public async Task DetectMatchAsync_NotAllMembersLiked_ReturnsNull()
    {
        // Arrange — 2 active members, only 1 like
        _partyMemberRepositoryMock
            .GetByPartyIdAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(ActiveMembers(2));
        _swipeRepositoryMock
            .GetLikesForMovieInPartyAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        var result = await _service.DetectMatchAsync(PartyId, MovieId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DetectMatchAsync_AllMembersLiked_ReturnsMatch()
    {
        // Arrange — 2 active members, 2 likes, no existing match
        _partyMemberRepositoryMock
            .GetByPartyIdAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(ActiveMembers(2));
        _swipeRepositoryMock
            .GetLikesForMovieInPartyAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns(2);
        _matchRepositoryMock
            .ExistsForMovieInPartyAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _service.DetectMatchAsync(PartyId, MovieId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.WatchPartyId, Is.EqualTo(PartyId));
        Assert.That(result.MovieId, Is.EqualTo(MovieId));
        Assert.That(result.IsWatched, Is.False);
    }

    [Test]
    public async Task DetectMatchAsync_MatchAlreadyExists_ReturnsNull()
    {
        // Arrange — all liked but match already recorded
        _partyMemberRepositoryMock
            .GetByPartyIdAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(ActiveMembers(2));
        _swipeRepositoryMock
            .GetLikesForMovieInPartyAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns(2);
        _matchRepositoryMock
            .ExistsForMovieInPartyAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _service.DetectMatchAsync(PartyId, MovieId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DetectMatchAsync_IgnoresInactiveMembers_ReturnsMatchIfRemainingAllLiked()
    {
        // Arrange — 1 active + 1 inactive member, 1 like → should match (inactive ignored)
        var members = new List<PartyMember>
        {
            new() { Id = Guid.NewGuid(), IsActive = true },
            new() { Id = Guid.NewGuid(), IsActive = false }
        };
        _partyMemberRepositoryMock
            .GetByPartyIdAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(members);
        _swipeRepositoryMock
            .GetLikesForMovieInPartyAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns(1);
        _matchRepositoryMock
            .ExistsForMovieInPartyAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _service.DetectMatchAsync(PartyId, MovieId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    private static IReadOnlyList<PartyMember> ActiveMembers(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new PartyMember { Id = Guid.NewGuid(), IsActive = true })
            .ToList();
}
