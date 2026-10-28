using System.ComponentModel.DataAnnotations;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

public record CreateFertilityZoneResource(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Name,

    [Range(1, int.MaxValue)]
    int DeviceId,

    [Required]
    [MinLength(3)]
    IReadOnlyList<FieldVertexResource> Boundary
);
