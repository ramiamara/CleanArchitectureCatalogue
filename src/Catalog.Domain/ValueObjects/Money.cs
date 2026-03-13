namespace Catalog.Domain.ValueObjects;

/// <summary>
/// Value Object représentant un montant monétaire.
/// </summary>
public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "EUR")
    {
        if (amount < 0)
            throw new ArgumentException("Le montant ne peut pas être négatif.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("La devise doit être un code ISO 4217 valide.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "EUR") => new(0, currency);

    public override string ToString() => $"{Amount:F2} {Currency}";
}