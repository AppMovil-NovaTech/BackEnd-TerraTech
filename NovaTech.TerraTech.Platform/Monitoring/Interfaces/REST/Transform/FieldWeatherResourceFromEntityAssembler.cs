using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Transform;

public static class FieldWeatherResourceFromEntityAssembler
{
    public static FieldWeatherResource ToResourceFromEntity(FieldWeatherResult entity)
    {
        return new FieldWeatherResource(
            FieldId: entity.FieldId,
            FieldName: entity.FieldName,
            ForecastTimestamp: entity.ForecastTimestamp,
            CurrentTemperatureC: entity.CurrentTemperatureC,
            RelativeHumidityPercent: entity.RelativeHumidityPercent,
            WindSpeedKmh: entity.WindSpeedKmh,
            PrecipitationProbability24h: entity.PrecipitationProbability24h,
            ExpectedPrecipitationMm: entity.ExpectedPrecipitationMm,
            CombinedRecommendation: new CombinedRecommendationResource(
                SuspensionRecommended: entity.CombinedRecommendation.SuspensionRecommended,
                EstimatedWaterSavingsLiters: entity.CombinedRecommendation.EstimatedWaterSavingsLiters,
                Message: entity.CombinedRecommendation.Message
            )
        );
    }
}
