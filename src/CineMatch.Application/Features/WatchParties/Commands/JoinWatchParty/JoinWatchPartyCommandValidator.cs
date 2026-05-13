using FluentValidation;

namespace CineMatch.Application.Features.WatchParties.Commands.JoinWatchParty;

public class JoinWatchPartyCommandValidator : AbstractValidator<JoinWatchPartyCommand>
{
    public JoinWatchPartyCommandValidator()
    {
        RuleFor(x => x.JoinCode)
            .NotEmpty().WithMessage("Join code is required.")
            .Length(6).WithMessage("Join code must be exactly 6 characters.");
    }
}
