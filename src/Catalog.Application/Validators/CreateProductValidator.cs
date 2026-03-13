namespace Catalog.Application.Validators;

using FluentValidation;
using Catalog.Application.DTOs;

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom du produit est requis.")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Le prix doit être supérieur à 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("La devise est requise.")
            .Length(3).WithMessage("La devise doit être un code ISO 4217 valide.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("La quantité ne peut pas être négative.");

        RuleFor(x => x.CatalogueId)
            .NotEmpty().WithMessage("L'ID du catalogue est requis.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La description ne peut pas dépasser 500 caractères.");
    }
}