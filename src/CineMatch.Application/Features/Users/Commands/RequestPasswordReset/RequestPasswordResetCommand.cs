using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Users.Commands.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email) : IRequest<ErrorOr<string>>;
