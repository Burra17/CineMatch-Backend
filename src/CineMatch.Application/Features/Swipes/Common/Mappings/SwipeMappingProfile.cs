using AutoMapper;
using CineMatch.Application.Features.Swipes.Common.Dtos;
using CineMatch.Domain.Models;

namespace CineMatch.Application.Features.Swipes.Common.Mappings;

public class SwipeMappingProfile : Profile
{
    public SwipeMappingProfile()
    {
        CreateMap<Movie, MovieDto>()
            .ForCtorParam("id",          opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("tmdbId",      opt => opt.MapFrom(src => src.TmdbId))
            .ForCtorParam("title",       opt => opt.MapFrom(src => src.Title))
            .ForCtorParam("posterUrl",   opt => opt.MapFrom(src => src.PosterUrl))
            .ForCtorParam("overview",    opt => opt.MapFrom(src => src.Overview))
            .ForCtorParam("releaseYear", opt => opt.MapFrom(src => src.ReleaseYear));
    }
}
