using FluentValidation;

namespace ShareBook.Domain.Validators
{
    public class MeetupValidator : AbstractValidator<Meetup>
    {
        private const string RequiredPropertyMessage = "A propriedade: {PropertyName} é obrigatória!";

        public MeetupValidator()
        {
            RuleFor(m => m.Title)
                .NotEmpty()
                .WithMessage(RequiredPropertyMessage);

            RuleFor(m => m.StartDate)
                .NotEmpty()
                .WithMessage(RequiredPropertyMessage);

            RuleFor(m => m.SymplaEventUrl)
                .NotEmpty()
                .WithMessage(RequiredPropertyMessage);

            RuleFor(m => m.SymplaEventId)
                .NotEmpty()
                .WithMessage(RequiredPropertyMessage);
        }
    }
}