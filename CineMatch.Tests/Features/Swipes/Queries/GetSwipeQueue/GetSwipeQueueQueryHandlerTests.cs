using AutoMapper;
using CineMatch.Application.Features.Swipes.Common.Dtos;
using CineMatch.Application.Features.Swipes.Common.Errors;
using CineMatch.Application.Features.Swipes.Queries.GetSwipeQueue;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.Swipes.Queries.GetSwipeQueue;

[TestFixture]
public class GetSwipeQueueQueryHandlerTests
{
    private ICurrentUserService _currentUserServiceMock;
    private IPartyMemberRepository _partyMemberRepositoryMock;
    private ISwipeRepository _swipeRepositoryMock;
    private IWatchPartyMovieRepository _watchPartyMovieRepositoryMock;
    private IMapper _mapperMock;
    private GetSwipeQueueQueryHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PartyId = Guid.NewGuid();

    private static readonly PartyMember ActiveMember = new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        WatchPartyId = PartyId,
        IsActive = true
    };

    private static Movie MakeMovie(int index) => new()
    {
        Id = Guid.NewGuid(),
        TmdbId = index,
        Title = $"Movie {index}"
    };

    [SetUp]
    public void SetUp()
    {
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();
        _partyMemberRepositoryMock = Substitute.For<IPartyMemberRepository>();
        _swipeRepositoryMock = Substitute.For<ISwipeRepository>();
        _watchPartyMovieRepositoryMock = Substitute.For<IWatchPartyMovieRepository>();
        _mapperMock = Substitute.For<IMapper>();

        _currentUserServiceMock.UserId.Returns(UserId);
        _partyMemberRepositoryMock
            .GetMembershipAsync(UserId, PartyId, Arg.Any<CancellationToken>())
            .Returns(ActiveMember);
        _swipeRepositoryMock
            .GetSwipedMovieIdsForMemberAsync(ActiveMember.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        _mapperMock
            .Map<MovieDto>(Arg.Any<Movie>())
            .Returns(call => new MovieDto(
                call.Arg<Movie>().Id,
                call.Arg<Movie>().TmdbId,
                call.Arg<Movie>().Title,
                string.Empty, string.Empty, 0));

        _handler = new GetSwipeQueueQueryHandler(
            _currentUserServiceMock,
            _partyMemberRepositoryMock,
            _swipeRepositoryMock,
            _watchPartyMovieRepositoryMock,
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
        var result = await _handler.Handle(new GetSwipeQueueQuery(PartyId), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError, Is.EqualTo(SwipeErrors.NotMemberOfParty));
    }

    [Test]
    public async Task Handle_NoMoviesSwiped_ReturnsFirstNInOrder()
    {
        // Arrange — 5 movies in party, none swiped, request Count = 3
        var movies = Enumerable.Range(1, 5).Select(MakeMovie).ToList();
        _watchPartyMovieRepositoryMock
            .GetMoviesForPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(movies);

        // Act
        var result = await _handler.Handle(new GetSwipeQueueQuery(PartyId, Count: 3), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value.Count, Is.EqualTo(3));
        Assert.That(result.Value[0].TmdbId, Is.EqualTo(movies[0].TmdbId));
        Assert.That(result.Value[1].TmdbId, Is.EqualTo(movies[1].TmdbId));
        Assert.That(result.Value[2].TmdbId, Is.EqualTo(movies[2].TmdbId));
    }

    [Test]
    public async Task Handle_SomeMoviesSwiped_ReturnsRemainingInOrder()
    {
        // Arrange — 4 movies, first 2 already swiped
        var movies = Enumerable.Range(1, 4).Select(MakeMovie).ToList();
        var swipedIds = movies.Take(2).Select(m => m.Id).ToList();

        _watchPartyMovieRepositoryMock
            .GetMoviesForPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(movies);
        _swipeRepositoryMock
            .GetSwipedMovieIdsForMemberAsync(ActiveMember.Id, Arg.Any<CancellationToken>())
            .Returns(swipedIds);

        // Act
        var result = await _handler.Handle(new GetSwipeQueueQuery(PartyId, Count: 10), CancellationToken.None);

        // Assert — only unswiped movies returned, in original order
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value.Count, Is.EqualTo(2));
        Assert.That(result.Value[0].TmdbId, Is.EqualTo(movies[2].TmdbId));
        Assert.That(result.Value[1].TmdbId, Is.EqualTo(movies[3].TmdbId));
    }

    [Test]
    public async Task Handle_AllMoviesSwiped_ReturnsEmptyList()
    {
        // Arrange — all 3 movies already swiped
        var movies = Enumerable.Range(1, 3).Select(MakeMovie).ToList();
        var swipedIds = movies.Select(m => m.Id).ToList();

        _watchPartyMovieRepositoryMock
            .GetMoviesForPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(movies);
        _swipeRepositoryMock
            .GetSwipedMovieIdsForMemberAsync(ActiveMember.Id, Arg.Any<CancellationToken>())
            .Returns(swipedIds);

        // Act
        var result = await _handler.Handle(new GetSwipeQueueQuery(PartyId), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value, Is.Empty);
    }
}
