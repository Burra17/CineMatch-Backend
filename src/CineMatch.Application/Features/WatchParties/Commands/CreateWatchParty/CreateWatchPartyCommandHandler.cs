using AutoMapper;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CineMatch.Application.Features.WatchParties.Commands.CreateWatchParty;

public class CreateWatchPartyCommandHandler : IRequestHandler<CreateWatchPartyCommand, ErrorOr<WatchPartyDto>>
{
    // Genre selection is not exposed to clients yet — all parties default to "popular" until we add real genre picking.
    private const string DefaultGenre = "popular";
    private const int MoviesPerParty = 50;

    private readonly ICurrentUserService _currentUserService;
    private readonly IJoinCodeGenerator _joinCodeGenerator;
    private readonly IWatchPartyRepository _watchPartyRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IWatchPartyMovieRepository _watchPartyMovieRepository;
    private readonly ITmdbService _tmdbService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateWatchPartyCommandHandler> _logger;

    public CreateWatchPartyCommandHandler(
        ICurrentUserService currentUserService,
        IJoinCodeGenerator joinCodeGenerator,
        IWatchPartyRepository watchPartyRepository,
        IMovieRepository movieRepository,
        IWatchPartyMovieRepository watchPartyMovieRepository,
        ITmdbService tmdbService,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<CreateWatchPartyCommandHandler> logger)
    {
        _currentUserService = currentUserService;
        _joinCodeGenerator = joinCodeGenerator;
        _watchPartyRepository = watchPartyRepository;
        _movieRepository = movieRepository;
        _watchPartyMovieRepository = watchPartyMovieRepository;
        _tmdbService = tmdbService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ErrorOr<WatchPartyDto>> Handle(CreateWatchPartyCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
            return WatchPartyErrors.Unauthorized;

        var joinCode = await _joinCodeGenerator.GenerateUniqueCodeAsync(cancellationToken);

        var watchParty = new WatchParty
        {
            Id = Guid.NewGuid(),
            JoinCode = joinCode,
            HostId = userId.Value,
            Genre = DefaultGenre,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PartyMembers = new List<PartyMember>()
        };

        var partyMember = new PartyMember
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            WatchPartyId = watchParty.Id,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        watchParty.PartyMembers.Add(partyMember);
        await _watchPartyRepository.AddAsync(watchParty, cancellationToken);

        await FetchAndStageMoviesAsync(watchParty.Id, cancellationToken);

        await _unitOfWork.SaveChangesAsync();

        var partyWithRelations = await _watchPartyRepository.GetByIdWithMembersAsync(watchParty.Id, cancellationToken);
        return _mapper.Map<WatchPartyDto>(partyWithRelations);
    }

    private async Task FetchAndStageMoviesAsync(Guid watchPartyId, CancellationToken cancellationToken)
    {
        var tmdbMovies = (await _tmdbService.GetMoviesByGenreAsync(DefaultGenre, MoviesPerParty, cancellationToken))
            .DistinctBy(movie => movie.TmdbId)
            .ToList();

        if (tmdbMovies.Count < MoviesPerParty)
            _logger.LogWarning("TMDB returned {Actual} movies, fewer than the requested {Count}.", tmdbMovies.Count, MoviesPerParty);

        var tmdbIds = tmdbMovies.Select(m => m.TmdbId).ToList();
        var existingMovies = await _movieRepository.GetExistingByTmdbIdsAsync(tmdbIds, cancellationToken);
        var existingById = existingMovies.ToDictionary(m => m.TmdbId);

        var newMovies = tmdbMovies
            .Where(dto => !existingById.ContainsKey(dto.TmdbId))
            .Select(dto => new Movie
            {
                Id = Guid.NewGuid(),
                TmdbId = dto.TmdbId,
                Title = dto.Title,
                PosterUrl = dto.PosterUrl,
                Overview = dto.Overview,
                ReleaseYear = dto.ReleaseYear,
                CachedAt = DateTime.UtcNow
            })
            .ToList();

        if (newMovies.Count > 0)
            await _movieRepository.BulkInsertAsync(newMovies, cancellationToken);

        var allMoviesById = existingById;
        foreach (var movie in newMovies)
            allMoviesById[movie.TmdbId] = movie;

        var watchPartyMovies = tmdbMovies
            .Select((dto, index) => new WatchPartyMovie
            {
                Id = Guid.NewGuid(),
                WatchPartyId = watchPartyId,
                MovieId = allMoviesById[dto.TmdbId].Id,
                OrderIndex = index,
                AddedAt = DateTime.UtcNow
            })
            .ToList();

        await _watchPartyMovieRepository.AddRangeAsync(watchPartyMovies, cancellationToken);
    }
}
