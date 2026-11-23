using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Transform;
public static class ConfigureProductInterestThresholdCommandFromResourceAssembler
{
    public static ConfigureProductInterestThresholdCommand ToCommandFromResource(
        int productId, int userId, ConfigureProductInterestThresholdResource resource)
    {
        return new ConfigureProductInterestThresholdCommand(productId, resource.Threshold, userId);
    }
}
