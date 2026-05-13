using FluentValidation;

namespace CineMatch.Application.Features.Swipes.Commands.CreateSwipe;

public class CreateSwipeCommandValidator : AbstractValidator<CreateSwipeCommand>
{
    public CreateSwipeCommandValidator()
    {
        RuleFor(x => x.WatchPartyId).NotEmpty();
        RuleFor(x => x.MovieId).NotEmpty();
    }
}
