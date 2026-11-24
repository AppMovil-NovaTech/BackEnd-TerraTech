using System.ComponentModel.DataAnnotations;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;
public record ConfigureProductInterestThresholdResource([Range(1, int.MaxValue)] int Threshold);
