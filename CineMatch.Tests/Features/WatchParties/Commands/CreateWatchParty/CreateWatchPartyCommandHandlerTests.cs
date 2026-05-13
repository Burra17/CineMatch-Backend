using AutoMapper;
using CineMatch.Application.Features.Movies.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Commands.CreateWatchParty;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CineMatch.Tests.Features.WatchParties.Commands.CreateWatchParty
{
    [TestFixture]
    public class CreateWatchPartyCommandHandlerTests
    {
        private ICurrentUserService _currentUserServiceMock;
        private IJoinCodeGenerator _joinCodeGeneratorMock;
        private IWatchPartyRepository _watchPartyRepositoryMock;
        private IMovieRepository _movieRepositoryMock;
        private IWatchPartyMovieRepository _watchPartyMovieRepositoryMock;
        private ITmdbService _tmdbServiceMock;
        private IMapper _mapperMock;
        private IUnitOfWork _unitOfWorkMock;
        private ILogger<CreateWatchPartyCommandHandler> _loggerMock;
        private CreateWatchPartyCommandHandler _handler;

        private static readonly List<TmdbMovieDto> ThreeDummyMovies =
        [
            new(100, "Movie A", "/a.jpg", "Overview A", 2020),
            new(200, "Movie B", "/b.jpg", "Overview B", 2021),
            new(300, "Movie C", "/c.jpg", "Overview C", 2022),
        ];

        [SetUp]
        public void SetUp()
        {
            _currentUserServiceMock = Substitute.For<ICurrentUserService>();
            _joinCodeGeneratorMock = Substitute.For<IJoinCodeGenerator>();
            _watchPartyRepositoryMock = Substitute.For<IWatchPartyRepository>();
            _movieRepositoryMock = Substitute.For<IMovieRepository>();
            _watchPartyMovieRepositoryMock = Substitute.For<IWatchPartyMovieRepository>();
            _tmdbServiceMock = Substitute.For<ITmdbService>();
            _mapperMock = Substitute.For<IMapper>();
            _unitOfWorkMock = Substitute.For<IUnitOfWork>();
            _loggerMock = Substitute.For<ILogger<CreateWatchPartyCommandHandler>>();

            // Default: TMDB returns 3 movies, none exist in DB
            _tmdbServiceMock
                .GetMoviesByGenreAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(ThreeDummyMovies);
            _movieRepositoryMock
                .GetByTmdbIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((Movie?)null);

            _handler = new CreateWatchPartyCommandHandler(
                _currentUserServiceMock,
                _joinCodeGeneratorMock,
                _watchPartyRepositoryMock,
                _movieRepositoryMock,
                _watchPartyMovieRepositoryMock,
                _tmdbServiceMock,
                _mapperMock,
                _unitOfWorkMock,
                _loggerMock);
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
            _currentUserServiceMock.UserId.Returns((Guid?)null);

            // Act
            var result = await _handler.Handle(new CreateWatchPartyCommand(), CancellationToken.None);

            // Assert
            Assert.That(result.IsError, Is.True, "Resultatet borde vara ett fel.");
            Assert.That(result.FirstError, Is.EqualTo(WatchPartyErrors.Unauthorized), "Felet ska vara Unauthorized.");

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
            await _watchPartyRepositoryMock.Received(1).AddAsync(Arg.Is<WatchParty>(wp =>
                wp.HostId == userId &&
                wp.PartyMembers.Count == 1 &&
                wp.PartyMembers.First().UserId == userId &&
                wp.PartyMembers.First().IsActive == true));

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
            await _joinCodeGeneratorMock.Received(1).GenerateUniqueCodeAsync(Arg.Any<CancellationToken>());
            await _watchPartyRepositoryMock.Received(1).AddAsync(Arg.Is<WatchParty>(wp =>
                wp.JoinCode == generatedCode));
        }

        [Test]
        public async Task Handle_ValidCommand_DeduplicatesExistingMovies()
        {
            // Arrange — TmdbIds 100 and 200 already exist in DB, 300 is new
            var userId = Guid.NewGuid();
            var existingMovieA = new Movie { Id = Guid.NewGuid(), TmdbId = 100 };
            var existingMovieB = new Movie { Id = Guid.NewGuid(), TmdbId = 200 };

            _currentUserServiceMock.UserId.Returns(userId);
            _joinCodeGeneratorMock.GenerateUniqueCodeAsync(Arg.Any<CancellationToken>()).Returns("ABC234");
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new WatchParty());

            _movieRepositoryMock.GetByTmdbIdAsync(100, Arg.Any<CancellationToken>()).Returns(existingMovieA);
            _movieRepositoryMock.GetByTmdbIdAsync(200, Arg.Any<CancellationToken>()).Returns(existingMovieB);
            _movieRepositoryMock.GetByTmdbIdAsync(300, Arg.Any<CancellationToken>()).Returns((Movie?)null);

            // Act
            await _handler.Handle(new CreateWatchPartyCommand(), CancellationToken.None);

            // Assert — only 1 new movie inserted, 3 WatchPartyMovies created
            await _movieRepositoryMock.Received(1).BulkInsertAsync(
                Arg.Is<IEnumerable<Movie>>(list => list.Count() == 1 && list.Single().TmdbId == 300),
                Arg.Any<CancellationToken>());

            await _watchPartyMovieRepositoryMock.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<WatchPartyMovie>>(list => list.Count() == 3),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Handle_ValidCommand_CreatesWatchPartyMoviesWithCorrectOrderIndex()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _currentUserServiceMock.UserId.Returns(userId);
            _joinCodeGeneratorMock.GenerateUniqueCodeAsync(Arg.Any<CancellationToken>()).Returns("ABC234");
            _watchPartyRepositoryMock
                .GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new WatchParty());

            IEnumerable<WatchPartyMovie> capturedMovies = [];
            await _watchPartyMovieRepositoryMock.AddRangeAsync(
                Arg.Do<IEnumerable<WatchPartyMovie>>(m => capturedMovies = m),
                Arg.Any<CancellationToken>());

            // Act
            await _handler.Handle(new CreateWatchPartyCommand(), CancellationToken.None);

            // Assert — OrderIndex must be 0, 1, 2 in the same order TMDB returned
            var ordered = capturedMovies.OrderBy(m => m.OrderIndex).ToList();
            Assert.That(ordered.Count, Is.EqualTo(3));
            Assert.That(ordered[0].OrderIndex, Is.EqualTo(0));
            Assert.That(ordered[1].OrderIndex, Is.EqualTo(1));
            Assert.That(ordered[2].OrderIndex, Is.EqualTo(2));
        }

        [Test]
        public async Task Handle_ValidCommand_SaveChangesCalledExactlyOnce()
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
            await _unitOfWorkMock.Received(1).SaveChangesAsync();
        }
    }
}
