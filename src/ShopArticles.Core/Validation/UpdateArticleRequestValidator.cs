using FluentValidation;
using ShopArticles.Core.Contracts;

namespace ShopArticles.Core.Validation;

public class UpdateArticleRequestValidator : AbstractValidator<UpdateArticleRequest>
{
    public UpdateArticleRequestValidator()
    {
        Include(new CreateArticleRequestValidator());

        RuleFor(x => x.Version)
            .NotNull().WithMessage("version is required; send the version you received with the article.")
            .GreaterThan(0);
    }
}
