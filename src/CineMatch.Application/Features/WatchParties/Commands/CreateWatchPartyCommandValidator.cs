using FluentValidation;

namespace CineMatch.Application.Features.WatchParties.Commands
{
    public class CreateWatchPartyCommandValidator : AbstractValidator<CreateWatchPartyCommand>
    {
        public CreateWatchPartyCommandValidator() 
        {
            RuleFor(x => x.Genre)
                .NotEmpty().WithMessage("Genre is Required")
                .MaximumLength(50).WithMessage("Genre must be at most 50 characters long");
        }
    }
}
