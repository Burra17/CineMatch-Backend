using AutoMapper;
using CineMatch.Application.Features.Users.Common.Dtos;
using CineMatch.Application.Features.Users.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ErrorOr<UserDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return UserErrors.InvalidCredentials;
        }

        var user = await _userRepository.GetByIdAsync(userId.Value);

        // The JWT was valid but the user record is gone (account deleted after the token was issued).
        // Returning NotFound rather than Unauthorized is intentional — the token itself is still cryptographically valid.
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        return _mapper.Map<UserDto>(user);
    }
}
