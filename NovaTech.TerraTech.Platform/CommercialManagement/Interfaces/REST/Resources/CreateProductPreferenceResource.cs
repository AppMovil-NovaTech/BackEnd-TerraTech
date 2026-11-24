using System.ComponentModel.DataAnnotations;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;

public record CreateProductPreferenceResource([Required, StringLength(50, MinimumLength = 1)] string Category);
