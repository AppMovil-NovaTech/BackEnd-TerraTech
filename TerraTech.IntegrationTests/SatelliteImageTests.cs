using System.Net;
using System.Net.Http.Json;
using Xunit;

public partial class JourneyTests
{
    [Fact]
    public async Task GetLatestSatelliteImageReturns200WithMetadataAndBounds()
    {
        var a = await Account();
        var response = await a.client.GetAsync($"/api/v1/fields/{a.fieldId}/satellite-images/latest");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await Json(response);
        Assert.Equal(a.fieldId, json.GetProperty("fieldId").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("imageUrl").GetString()));
        Assert.InRange(json.GetProperty("ndviAverage").GetDouble(), 0.1, 1.0);
        Assert.InRange(json.GetProperty("cloudCoveragePercent").GetDouble(), 0.0, 100.0);
        Assert.True(json.TryGetProperty("bounds", out var bounds));
        Assert.True(bounds.TryGetProperty("north", out _));
        Assert.True(bounds.TryGetProperty("south", out _));
        Assert.True(bounds.TryGetProperty("east", out _));
        Assert.True(bounds.TryGetProperty("west", out _));
    }

    [Fact]
    public async Task GetSatelliteImageByTargetDateReturnsClosestImage()
    {
        var a = await Account();
        var targetDate = DateTime.UtcNow.AddDays(-14).ToString("yyyy-MM-dd");
        var response = await a.client.GetAsync($"/api/v1/fields/{a.fieldId}/satellite-images?targetDate={targetDate}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await Json(response);
        Assert.Equal(a.fieldId, json.GetProperty("fieldId").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("imageUrl").GetString()));
    }

    [Fact]
    public async Task GetSatelliteImageWhenDateOutOfRangeReturns404()
    {
        var a = await Account();
        var response = await a.client.GetAsync($"/api/v1/fields/{a.fieldId}/satellite-images?targetDate=2020-01-01");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await Json(response);
        Assert.Equal("SATELLITE_IMAGE_NOT_FOUND", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetSatelliteImageRejectsForeignOrNonexistentFieldWith404()
    {
        var a = await Account();
        var response = await a.client.GetAsync("/api/v1/fields/999999/satellite-images/latest");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
