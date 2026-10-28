using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Monitoring.Infrastructure.External.Weather;
using Xunit;

public partial class JourneyTests
{
    private class FakeWeatherClient(WeatherForecastData? dataToReturn, bool shouldThrow = false) : IWeatherForecastClient
    {
        public Task<WeatherForecastData?> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            if (shouldThrow)
                throw new HttpRequestException("Simulated weather provider failure.");
            return Task.FromResult(dataToReturn);
        }
    }

    [Fact]
    public async Task WeatherForecastReturns200WithCombinedRecommendation()
    {
        var fakeData = new WeatherForecastData(
            ForecastTimestampUtc: DateTime.UtcNow,
            CurrentTemperatureC: 22.5,
            RelativeHumidityPercent: 65,
            WindSpeedKmh: 14.0,
            PrecipitationProbability24h: 85,
            ExpectedPrecipitationMm: 12.0
        );
        var customServer = server.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IWeatherForecastClient>(_ => new FakeWeatherClient(fakeData));
            });
        });

        var client = customServer.CreateClient();
        var a = await Account();
        client.DefaultRequestHeaders.Authorization = a.client.DefaultRequestHeaders.Authorization;

        var response = await client.GetAsync($"/api/v1/fields/{a.fieldId}/weather");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await Json(response);
        Assert.Equal(a.fieldId, json.GetProperty("fieldId").GetInt32());
        Assert.Equal(22.5, json.GetProperty("currentTemperatureC").GetDouble());
        Assert.Equal(85, json.GetProperty("precipitationProbability24h").GetDouble());
        Assert.True(json.GetProperty("combinedRecommendation").GetProperty("suspensionRecommended").GetBoolean());
        Assert.Equal(500, json.GetProperty("combinedRecommendation").GetProperty("estimatedWaterSavingsLiters").GetInt32());
    }

    [Fact]
    public async Task WeatherForecastReturns502WhenProviderFails()
    {
        var customServer = server.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IWeatherForecastClient>(_ => new FakeWeatherClient(null, shouldThrow: true));
            });
        });

        var client = customServer.CreateClient();
        var a = await Account();
        client.DefaultRequestHeaders.Authorization = a.client.DefaultRequestHeaders.Authorization;

        var response = await client.GetAsync($"/api/v1/fields/{a.fieldId}/weather");
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var json = await Json(response);
        Assert.Equal("WEATHER_SERVICE_UNAVAILABLE", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WeatherForecastRejectsForeignOrNonexistentFieldWith404()
    {
        var a = await Account();
        var response = await a.client.GetAsync("/api/v1/fields/999999/weather");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
