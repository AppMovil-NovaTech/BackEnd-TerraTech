using System.ComponentModel.DataAnnotations;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

public class UpdateFertilityZoneThresholdsResource
{
    [Required]
    [Range(0.0, 100.0)]
    public double? MoistureMin { get; set; }

    [Required]
    [Range(0.0, 100.0)]
    public double? MoistureMax { get; set; }

    [Required]
    [Range(0.0, double.MaxValue)]
    public double? NitrogenMin { get; set; }

    [Required]
    [Range(0.0, double.MaxValue)]
    public double? NitrogenMax { get; set; }

    [Required]
    [Range(0.0, double.MaxValue)]
    public double? PhosphorusMin { get; set; }

    [Required]
    [Range(0.0, double.MaxValue)]
    public double? PhosphorusMax { get; set; }

    [Required]
    [Range(0.0, double.MaxValue)]
    public double? PotassiumMin { get; set; }

    [Required]
    [Range(0.0, double.MaxValue)]
    public double? PotassiumMax { get; set; }
}
