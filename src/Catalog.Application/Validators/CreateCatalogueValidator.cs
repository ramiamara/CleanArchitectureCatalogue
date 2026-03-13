namespace Catalog.Application.Validators;

using FluentValidation;
using Catalog.Application.DTOs;

public class CreateCatalogueValidator : AbstractValidator<CreateCatalogueRequest>
{
    public CreateCatalogueValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom du catalogue est requis.")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La description ne peut pas dépasser 500 caractères.");
    }
}