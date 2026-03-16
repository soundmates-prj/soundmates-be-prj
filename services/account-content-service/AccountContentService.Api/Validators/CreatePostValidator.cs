using AccountContentService.Api.Contracts.Requests;
using FluentValidation;

namespace AccountContentService.Api.Validators
{
    public class CreatePostValidator : AbstractValidator<CreatePostRequest>
    {
        public CreatePostValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.ContentText)
                .NotEmpty();

            RuleFor(x => x.MoodTag)
                .MaximumLength(50);
        }
    }
}
