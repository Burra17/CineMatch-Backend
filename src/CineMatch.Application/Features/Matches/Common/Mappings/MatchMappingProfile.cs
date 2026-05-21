using AutoMapper;
using CineMatch.Application.Features.Matches.Common.Dtos;
using CineMatch.Domain.Models;

namespace CineMatch.Application.Features.Matches.Common.Mappings;

public class MatchMappingProfile : Profile
{
    public MatchMappingProfile()
    {
        CreateMap<Match, MatchDto>()
            .ForCtorParam("id",               opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("watchPartyId",     opt => opt.MapFrom(src => src.WatchPartyId))
            .ForCtorParam("movieId",          opt => opt.MapFrom(src => src.MovieId))
            .ForCtorParam("movieTitle",       opt => opt.MapFrom(src => src.Movie != null ? src.Movie.Title : null))
            .ForCtorParam("moviePosterUrl",   opt => opt.MapFrom(src => src.Movie != null ? src.Movie.PosterUrl : null))
            .ForCtorParam("movieOverview",    opt => opt.MapFrom(src => src.Movie != null ? src.Movie.Overview : null))
            .ForCtorParam("movieReleaseYear", opt => opt.MapFrom(src => src.Movie != null ? src.Movie.ReleaseYear : (int?)null))
            .ForCtorParam("matchedAt",        opt => opt.MapFrom(src => src.MatchedAt))
            .ForCtorParam("isWatched",        opt => opt.MapFrom(src => src.IsWatched))
            .ForCtorParam("watchedByUserId",  opt => opt.MapFrom(src => src.WatchedByUserId))
            .ForCtorParam("watchedAt",        opt => opt.MapFrom(src => src.WatchedAt));
    }
}
