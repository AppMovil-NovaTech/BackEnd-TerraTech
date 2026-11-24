using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
namespace NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;

public partial class Product
{
    protected Product()
    {
    }

    public Product(CreateProductCommand command)
    {
        Name = command.Name;
        Description = command.Description;
        Price = command.Price;
        Type = command.Type;
        ImageUrl = command.ImageUrl;
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0) throw new ArgumentException("Price must be greater than zero.");
        Price = newPrice;
    }
    public string Type { get; private set; }
    public string? ImageUrl { get; private set; }

    public int InterestThreshold { get; private set; } = 100;
    public int MonthlyConsultations { get; private set; }
    public int ConsultationMonth { get; private set; }
    public int ConsultationYear { get; private set; }
    public DateTimeOffset? LastConsultationAt { get; private set; }
    public bool HighInterestAlertSent { get; private set; }
    public bool InactivityAlertSent { get; private set; }

    public void ConfigureInterestThreshold(int threshold)
    {
        if (threshold <= 0)
            throw new ArgumentException(
                "Interest threshold must be greater than zero");

        InterestThreshold = threshold;
    }

    public void RegisterConsultation(DateTimeOffset consultedAt)
    {
        if (ConsultationMonth != consultedAt.Month ||
            ConsultationYear != consultedAt.Year)
        {
            MonthlyConsultations = 0;
            ConsultationMonth = consultedAt.Month;
            ConsultationYear = consultedAt.Year;
            HighInterestAlertSent = false;
        }

        MonthlyConsultations++;
        LastConsultationAt = consultedAt;
        InactivityAlertSent = false;
    }

    public bool ShouldNotifyHighInterest()
    {
        return MonthlyConsultations > InterestThreshold &&
               !HighInterestAlertSent;
    }

    public void MarkHighInterestAlertSent()
    {
        HighInterestAlertSent = true;
    }

    public bool ShouldNotifyInactivity(DateTimeOffset evaluatedAt)
    {
        var lastActivityAt = LastConsultationAt ?? CreatedAt;

        if (!lastActivityAt.HasValue)
            return false;

        return !InactivityAlertSent &&
               evaluatedAt >= lastActivityAt.Value.AddDays(30);
    }

    public void MarkInactivityAlertSent()
    {
        InactivityAlertSent = true;
    }
}