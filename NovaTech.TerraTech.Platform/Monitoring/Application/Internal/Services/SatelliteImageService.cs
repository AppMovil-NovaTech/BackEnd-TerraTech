using Microsoft.Extensions.Logging;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.Services;

public class SatelliteImageService(
    IFieldRepository fieldRepository,
    TimeProvider timeProvider) : ISatelliteImageService

{
    public async Task<SatelliteImageRecord?> GetLatestImageAsync(int fieldId, CancellationToken cancellationToken = default)
    {
        var field = await fieldRepository.FindByIdAsync(fieldId, cancellationToken);
        if (field is null)
            return null;

        var catalog = GenerateCatalogForField(field);
        return catalog.OrderByDescending(x => x.CapturedAtUtc).FirstOrDefault();
    }

    public async Task<SatelliteImageRecord?> GetImageByTargetDateAsync(int fieldId, DateTime targetDate, CancellationToken cancellationToken = default)
    {
        var field = await fieldRepository.FindByIdAsync(fieldId, cancellationToken);
        if (field is null)
            return null;

        var catalog = GenerateCatalogForField(field);
        var targetUtc = targetDate.Kind == DateTimeKind.Utc ? targetDate : targetDate.ToUniversalTime();

        var closest = catalog
            .Select(img => new { Image = img, Diff = Math.Abs((img.CapturedAtUtc.Date - targetUtc.Date).TotalDays) })
            .Where(x => x.Diff <= 60)
            .OrderBy(x => x.Diff)
            .FirstOrDefault();

        return closest?.Image;
    }

    private List<SatelliteImageRecord> GenerateCatalogForField(Field field)
    {
        var lat = field.LocationLatLong.Latitude;
        var lon = field.LocationLatLong.Longitude;

        var north = field.Boundary?.Max(p => p.Latitude) ?? lat + 0.002;
        var south = field.Boundary?.Min(p => p.Latitude) ?? lat - 0.002;
        var east = field.Boundary?.Max(p => p.Longitude) ?? lon + 0.002;
        var west = field.Boundary?.Min(p => p.Longitude) ?? lon - 0.002;
        var bounds = new SatelliteBounds(north, south, east, west);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return
        [
            new SatelliteImageRecord(
                Id: 1,
                FieldId: field.Id,
                CapturedAtUtc: now.AddDays(-2),
                ImageUrl: $"https://storage.googleapis.com/terratech-satellite-assets/fields/{field.Id}/scene-latest.png",
                NdviAverage: 0.74,
                CloudCoveragePercent: 3.2,
                Bounds: bounds
            ),
            new SatelliteImageRecord(
                Id: 2,
                FieldId: field.Id,
                CapturedAtUtc: now.AddDays(-15),
                ImageUrl: $"https://storage.googleapis.com/terratech-satellite-assets/fields/{field.Id}/scene-mid.png",
                NdviAverage: 0.69,
                CloudCoveragePercent: 5.8,
                Bounds: bounds
            ),
            new SatelliteImageRecord(
                Id: 3,
                FieldId: field.Id,
                CapturedAtUtc: now.AddDays(-30),
                ImageUrl: $"https://storage.googleapis.com/terratech-satellite-assets/fields/{field.Id}/scene-past.png",
                NdviAverage: 0.62,
                CloudCoveragePercent: 2.1,
                Bounds: bounds
            )
        ];
    }
}
