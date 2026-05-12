using AutoMapper;
using CineMatch.Application.Features.Users.Common.Dtos;
using CineMatch.Domain.Models;

namespace CineMatch.Application.Features.Users.Common.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Map from user to UserDto
        CreateMap<User, UserDto>();
    }
}
