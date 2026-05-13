using AutoMapper;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Domain.Models;

namespace CineMatch.Application.Features.WatchParties.Common.Mappings;

// DTOs are positional records (init-only constructor parameters), so ForCtorParam is required —
// ForMember would only run after construction and AutoMapper would fail to find a parameterless constructor.
public class WatchPartyMappingProfile : Profile
{
    public WatchPartyMappingProfile()
    {
        CreateMap<PartyMember, PartyMemberDto>()
            .ForCtorParam(nameof(PartyMemberDto.Username), opt => opt.MapFrom(src => src.User.Username));

        CreateMap<WatchParty, WatchPartyDto>()
            .ForCtorParam(nameof(WatchPartyDto.HostUsername), opt => opt.MapFrom(src => src.Host.Username))
            .ForCtorParam(nameof(WatchPartyDto.MemberCount), opt => opt.MapFrom(src => src.PartyMembers.Count));

        CreateMap<WatchParty, WatchPartyDetailsDto>()
            .ForCtorParam(nameof(WatchPartyDetailsDto.HostUsername), opt => opt.MapFrom(src => src.Host.Username))
            .ForCtorParam(nameof(WatchPartyDetailsDto.MemberCount), opt => opt.MapFrom(src => src.PartyMembers.Count))
            .ForCtorParam(nameof(WatchPartyDetailsDto.Members), opt => opt.MapFrom(src => src.PartyMembers));
    }
}
