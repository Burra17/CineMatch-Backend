using AutoMapper;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Domain.Models;

namespace CineMatch.Application.Features.WatchParties.Common.Mappings;

public class WatchPartyMappingProfile : Profile
{
    public WatchPartyMappingProfile()
    {
        // 1. Map PartyMember -> PartyMemberDto
        CreateMap<PartyMember, PartyMemberDto>()
            // Vi behöver plocka Username från den relaterade User-entiteten
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username));

        // 2. Map WatchParty -> WatchPartyDto
        CreateMap<WatchParty, WatchPartyDto>()
            // Plocka HostUsername från den relaterade User-entiteten (Host)
            .ForMember(dest => dest.HostUsername, opt => opt.MapFrom(src => src.Host.Username))
            // Räkna antalet medlemmar utifrån navigation propertyn 
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.PartyMembers.Count));

        // 3. Map WatchParty -> WatchPartyDetailsDto
        CreateMap<WatchParty, WatchPartyDetailsDto>()
            .ForMember(dest => dest.HostUsername, opt => opt.MapFrom(src => src.Host.Username))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.PartyMembers.Count))
            // Mappa hela listan av medlemmar till vår IReadOnlyList<PartyMemberDto>
            .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.PartyMembers));
    }
}
