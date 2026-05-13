using AutoMapper;
using CineMatch.Application.Features.Swipes.Commands.CreateSwipe;
using CineMatch.Application.Features.Swipes.Common.Dtos;
using CineMatch.Application.Features.Swipes.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.Swipes.Commands.CreateSwipe;

[TestFixture]
public class CreateSwipeCommandHandlerTests
{
    private ICurrentUserService _currentUserServiceMock;
    private IPartyMemberRepository _partyMemberRepositoryMock;
    private ISwipeRepository _swipeRepositoryMock;
    private IWatchPartyMovieRepository _watchPartyMovieRepositoryMock;
    private IMatchRepository _matchRepositoryMock;
    private IMatchDetectionService _matchDetectionServiceMock;
    private IUnitOfWork _unitOfWorkMock;
    private IMapper _mapperMock;
    private CreateSwipeCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PartyId = Guid.NewGuid();
    private static readonly Guid MovieId = Guid.NewGuid();

    private static readonly PartyMember ActiveMember = new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        WatchPartyId = PartyId,
        IsActive = true
    };

    private static readonly Movie TheMovie = new()
    {
        Id = MovieId,
        TmdbId = 42,
        Title = "Test Movie"
    };

    [SetUp]
    public void SetUp()
    {
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();
        _partyMemberRepositoryMock = Substitute.For<IPartyMemberRepository>();
        _swipeRepositoryMock = Substitute.For<ISwipeRepository>();
        _watchPartyMovieRepositoryMock = Substitute.For<IWatchPartyMovieRepository>();
        _matchRepositoryMock = Substitute.For<IMatchRepository>();
        _matchDetectionServiceMock = Substitute.For<IMatchDetectionService>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _mapperMock = Substitute.For<IMapper>();

        // Happy-path defaults
        _currentUserServiceMock.UserId.Returns(UserId);
        _partyMemberRepositoryMock
            .GetMembershipAsync(UserId, PartyId, Arg.Any<CancellationToken>())
            .Returns(ActiveMember);
        _swipeRepositoryMock
            .GetByPartyMemberAndMovieAsync(ActiveMember.Id, MovieId, Arg.Any<CancellationToken>())
            .Returns((Swipe?)null);
        _watchPartyMovieRepositoryMock
            .GetMoviesForPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(new List<Movie> { TheMovie });
        _matchDetectionServiceMock
            .DetectMatchAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns((Match?)null);

        _handler = new CreateSwipeCommandHandler(
            _currentUserServiceMock,
            _partyMemberRepositoryMock,
            _swipeRepositoryMock,
            _watchPartyMovieRepositoryMock,
            _matchRepositoryMock,
            _matchDetectionServiceMock,
            _unitOfWorkMock,
            _mapperMock);
    }

    [Test]
    public async Task Handle_NotMemberOfParty_ReturnsForbiddenError()
    {
        // Arrange
        _partyMemberRepositoryMock
            .GetMembershipAsync(UserId, PartyId, Arg.Any<CancellationToken>())
            .Returns((PartyMember?)null);

        // Act
        var result = await _handler.Handle(new CreateSwipeCommand(PartyId, MovieId, true), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError, Is.EqualTo(SwipeErrors.NotMemberOfParty));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task Handle_AlreadySwiped_ReturnsConflictError()
    {
        // Arrange
        _swipeRepositoryMock
            .GetByPartyMemberAndMovieAsync(ActiveMember.Id, MovieId, Arg.Any<CancellationToken>())
            .Returns(new Swipe());

        // Act
        var result = await _handler.Handle(new CreateSwipeCommand(PartyId, MovieId, true), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError, Is.EqualTo(SwipeErrors.AlreadySwiped));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task Handle_MovieNotInParty_ReturnsValidationError()
    {
        // Arrange — party has no movies matching the requested MovieId
        _watchPartyMovieRepositoryMock
            .GetMoviesForPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(new List<Movie>());

        // Act
        var result = await _handler.Handle(new CreateSwipeCommand(PartyId, MovieId, true), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError, Is.EqualTo(SwipeErrors.MovieNotInParty));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task Handle_ValidSwipeNoMatch_ReturnsSwipeResultWithIsMatchFalse()
    {
        // Arrange — match detection returns null (no match yet)
        _matchDetectionServiceMock
            .DetectMatchAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns((Match?)null);

        // Act
        var result = await _handler.Handle(new CreateSwipeCommand(PartyId, MovieId, true), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value.IsMatch, Is.False);
        Assert.That(result.Value.MatchedMovie, Is.Null);
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task Handle_ValidSwipeCausesMatch_ReturnsSwipeResultWithMatchedMovie()
    {
        // Arrange
        var match = new Match { Id = Guid.NewGuid(), WatchPartyId = PartyId, MovieId = MovieId };
        var expectedMovieDto = new MovieDto(MovieId, 42, "Test Movie", "", "", 2020);

        _matchDetectionServiceMock
            .DetectMatchAsync(PartyId, MovieId, Arg.Any<CancellationToken>())
            .Returns(match);
        _mapperMock.Map<MovieDto>(TheMovie).Returns(expectedMovieDto);

        // Act
        var result = await _handler.Handle(new CreateSwipeCommand(PartyId, MovieId, true), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value.IsMatch, Is.True);
        Assert.That(result.Value.MatchedMovie, Is.EqualTo(expectedMovieDto));
        await _matchRepositoryMock.Received(1).AddAsync(match);
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task Handle_DislikeSwipe_DoesNotRunMatchDetection()
    {
        // Arrange — IsLiked = false
        // Act
        await _handler.Handle(new CreateSwipeCommand(PartyId, MovieId, false), CancellationToken.None);

        // Assert — match detection must not run for dislikes
        await _matchDetectionServiceMock.DidNotReceive()
            .DetectMatchAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }
}
