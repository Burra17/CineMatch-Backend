using AutoMapper;
using CineMatch.Application.Features.Users.Common.Dtos;
using CineMatch.Application.Features.Users.Common.Errors;
using CineMatch.Application.Interfaces;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Users.Commands.LoginUser
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, ErrorOr<LoginResponseDto>>
    {
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IUserRepository _userRepository;

        public LoginUserCommandHandler(
            IMapper mapper, 
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IUserRepository userRepository)
        {
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _userRepository = userRepository;
        }

        public async Task<ErrorOr<LoginResponseDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null)
            {
                return UserErrors.InvalidCredentials;
            }

            var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return UserErrors.InvalidCredentials;
            }

            var token = _jwtService.GenerateToken(user);

            return new LoginResponseDto(
                Token: token,
                User: _mapper.Map<UserDto>(user));
        }
    }
}
