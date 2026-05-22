using CineMatch.Application.Interfaces.Repositories;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Admin.Queries.GetUserCount;

public class GetUserCountQueryHandler : IRequestHandler<GetUserCountQuery, ErrorOr<int>>
{
    private readonly IUserRepository _userRepository;

    public GetUserCountQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ErrorOr<int>> Handle(GetUserCountQuery request, CancellationToken cancellationToken)
    {
        return await _userRepository.CountAsync(cancellationToken);
    }
}
