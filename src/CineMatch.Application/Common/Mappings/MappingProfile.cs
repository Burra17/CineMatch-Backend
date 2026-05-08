using AutoMapper;
using CineMatch.Application.Features.Users.Common.Dtos;
using CineMatch.Domain.Models;

namespace CineMatch.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        //Map from user to userDTO
        public MappingProfile() 
        {
            CreateMap<User, UserDto>();
        }
    }
}
