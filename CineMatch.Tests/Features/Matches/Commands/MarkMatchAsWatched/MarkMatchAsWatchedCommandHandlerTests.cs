using AutoMapper;
using CineMatch.Application.Features.Matches.Commands.MarkMatchAsWatched;
using CineMatch.Application.Features.Matches.Common.Dtos;
using CineMatch.Application.Features.Matches.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using NSubstitute;

namespace CineMatch.Tests.Features.Matches.Commands.MarkMatchAsWatched;

[TestFixture]
public class MarkMatchAsWatchedCommandHandlerTests
{
    private ICurrentUserService _currentUserServiceMock;
    private IMatchRepository _matchRepositoryMock;
    private IPartyMemberRepository _partyMemberRepositoryMock;
    private IUnitOfWork _unitOfWorkMock;
    private IMapper _mapperMock;
    private MarkMatchAsWatchedCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PartyId = Guid.NewGuid();
    private static readonly Guid MatchId = Guid.NewGuid();

    private static readonly Match UnwatchedMatch = new()
    {
        Id = MatchId,
        WatchPartyId = PartyId,
        MovieId = Guid.NewGuid(),
        IsWatched = false
    };

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
        _matchRepositoryMock = Substitute.For<IMatchRepository>();
        _partyMemberRepositoryMock = Substitute.For<IPartyMemberRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _mapperMock = Substitute.For<IMapper>();

        _currentUserServiceMock.UserId.Returns(UserId);
        _matchRepositoryMock.GetByIdAsync(MatchId).Returns(UnwatchedMatch);
        _partyMemberRepositoryMock
            .GetMembershipAsync(UserId, PartyId, Arg.Any<CancellationToken>())
            .Returns(ActiveMember);
        _mapperMock
            .Map<MatchDto>(Arg.Any<Match>())
            .Returns(call =>
            {
                var m = call.Arg<Match>();
                return new MatchDto(m.Id, m.WatchPartyId, m.MovieId, m.MatchedAt, m.IsWatched, m.WatchedByUserId, m.WatchedAt);
            });

        _handler = new MarkMatchAsWatchedCommandHandler(
            _currentUserServiceMock,
            _matchRepositoryMock,
            _partyMemberRepositoryMock,
            _unitOfWorkMock,
            _mapperMock);
    }

    [Test]
    public async Task Handle_UnauthenticatedUser_ReturnsUnauthorizedError()
    {
        // Arrange
        _currentUserServiceMock.UserId.Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(new MarkMatchAsWatchedCommand(MatchId), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError, Is.EqualTo(MatchErrors.Unauthorized));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task Handle_NotMemberOfParty_ReturnsForbiddenError()
    {
        // Arrange — match exists but user is not an active member of that party
        _partyMemberRepositoryMock
            .GetMembershipAsync(UserId, PartyId, Arg.Any<CancellationToken>())
            .Returns((PartyMember?)null);

        // Act
        var result = await _handler.Handle(new MarkMatchAsWatchedCommand(MatchId), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError, Is.EqualTo(MatchErrors.NotMemberOfParty));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task Handle_MatchNotFound_ReturnsNotFound()
    {
        // Arrange
        _matchRepositoryMock.GetByIdAsync(MatchId).Returns((Match?)null);

        // Act
        var result = await _handler.Handle(new MarkMatchAsWatchedCommand(MatchId), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError, Is.EqualTo(MatchErrors.NotFound));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task Handle_AlreadyWatched_ReturnsConflict()
    {
        // Arrange
        var watchedMatch = new Match { Id = MatchId, WatchPartyId = PartyId, IsWatched = true };
        _matchRepositoryMock.GetByIdAsync(MatchId).Returns(watchedMatch);

        // Act
        var result = await _handler.Handle(new MarkMatchAsWatchedCommand(MatchId), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError, Is.EqualTo(MatchErrors.AlreadyWatched));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task Handle_Valid_UpdatesWatchedFields()
    {
        // Act
        var result = await _handler.Handle(new MarkMatchAsWatchedCommand(MatchId), CancellationToken.None);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(UnwatchedMatch.IsWatched, Is.True);
        Assert.That(UnwatchedMatch.WatchedByUserId, Is.EqualTo(UserId));
        Assert.That(UnwatchedMatch.WatchedAt, Is.Not.Null);
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }
}
