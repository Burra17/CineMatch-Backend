using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Users.Commands.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<ErrorOr<Success>>;
