
using System.ComponentModel.DataAnnotations;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

public class SimulateSensorReadingResource
{
    [Required]
    [Range(0, 100)]
    public double? MoisturePercent { get; set; }

    [Required]
    public double? SoilTemperatureC { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public double? NitrogenPpm { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public double? PhosphorusPpm { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public double? PotassiumPpm { get; set; }
}

