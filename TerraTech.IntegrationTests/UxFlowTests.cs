using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

public partial class JourneyTests
{
    [Fact]
    public async Task SignupRequiresVerificationBeforeIssuingAnEightHourSession()
    {
        var client = server.CreateClient();
        var email = Guid.NewGuid() + "@test.example";
        var response = await client.PostAsJsonAsync("/api/v1/authentication/sign-up", Registration(email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await Json(response);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, user.GetProperty("token").ValueKind);
        Assert.True(user.GetProperty("emailVerificationRequired").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { emailAddress = email, password = "test-password" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications/confirm", new { token = server.Emails.Verifications[email] })).StatusCode);
        user = await Json(await client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { emailAddress = email, password = "test-password" }));
        var token = user.GetProperty("token").GetString();
        var jwt = new JsonWebToken(token);
        Assert.InRange((jwt.ValidTo - DateTime.UtcNow).TotalHours, 7.99, 8.01);
        Assert.Equal(jwt.ValidTo, user.GetProperty("expiresAt").GetDateTime().ToUniversalTime());
        Assert.False(user.TryGetProperty("passwordHash", out _));
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var me = await client.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(user.GetProperty("id").GetInt32(), (await Json(me)).GetProperty("id").GetInt32());
    }

    private static object[] Rectangle() => [new
    {
        latitude = -14d,
        longitude = -75d
    }, new
    {
        latitude = -14d,
        longitude = -74.999d
    }, new
    {
        latitude = -14.001d,
        longitude = -74.999d
    }, new
    {
        latitude = -14.001d,
        longitude = -75d
    }

    ];
    private static object MappedField(int profile, object[] boundary) => new
    {
        profileId = profile,
        name = "Mapped parcel",
        sizeM2 = 1,
        soilType = "Sin especificar",
        latitude = 0,
        longitude = 0,
        cropName = "Papa",
        boundary
    };
    [Fact]
    public async Task PolygonIsPersistedComputesAreaAndLegacyUpdateKeepsIt()
    {
        var a = await Account();
        var polygon = Rectangle();
        var created = await a.client.PostAsJsonAsync("/api/v1/fields", MappedField(a.profileId, [.. polygon, polygon[0]]));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var field = await Json(created);
        var id = field.GetProperty("id").GetInt32();
        Assert.Equal(4, field.GetProperty("boundary").GetArrayLength());
        Assert.InRange(field.GetProperty("sizeM2").GetDouble(), 11990, 12010);
        Assert.Equal(-14.0005, field.GetProperty("latitude").GetDouble(), 6);
        var update = await a.client.PutAsJsonAsync($"/api/v1/fields/{id}", new { name = "Updated parcel", sizeM2 = 2, soilType = "Franco", latitude = 0, longitude = 0, cropName = "Papa" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        await server.WithDb(async db =>
        {
            var saved = await db.Set<Field>().IgnoreQueryFilters().SingleAsync(f => f.Id == id);
            Assert.Equal(4, saved.Boundary!.Count);
            Assert.InRange(saved.SizeM2.Value, 11990, 12010);
            Assert.Equal("Updated parcel", saved.Name.Value);
        });
        var other = await Account();
        Assert.Equal(HttpStatusCode.NotFound, (await other.client.GetAsync($"/api/v1/fields/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.client.PutAsJsonAsync($"/api/v1/fields/{id}", MappedField(other.profileId, polygon))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.client.DeleteAsync($"/api/v1/fields/{id}")).StatusCode);
        var clear = await a.client.PutAsJsonAsync($"/api/v1/fields/{id}", MappedField(a.profileId, []));
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await Json(clear)).GetProperty("boundary").ValueKind);
    }

    [Theory]
    [InlineData("cross")]
    [InlineData("too-few")]
    [InlineData("duplicate")]
    [InlineData("collinear")]
    [InlineData("range")]
    public async Task InvalidPolygonIsRejectedWithoutCreatingField(string kind)
    {
        var a = await Account();
        var p = Rectangle();
        object[] bad = kind switch
        {
            "cross" => [p[0], p[2], p[1], p[3]],
            "too-few" => [p[0], p[1]],
            "duplicate" => [p[0], p[1], p[0], p[2]],
            "collinear" => [new
            {
                latitude = 1,
                longitude = 1
            }, new
            {
                latitude = 2,
                longitude = 2
            }, new
            {
                latitude = 3,
                longitude = 3
            }

            ],
            _ => [new
            {
                latitude = 91,
                longitude = 1
            }, p[1], p[2]]
        };
        var response = await a.client.PostAsJsonAsync("/api/v1/fields", MappedField(a.profileId, bad));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Single((await Json(await a.client.GetAsync("/api/v1/fields"))).EnumerateArray());
    }

    [Fact]
    public async Task LegacyControllersAlsoAdvertiseProblemDetailsForErrors()
    {
        var a = await Account();
        foreach (var path in new[]
        {
            "/api/v1/fields/2147483647",
            "/api/v1/devices/2147483647"
        }

        )
        {
            var response = await a.client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        var unknown = await a.client.PostAsJsonAsync("/api/v1/devices/register", new { sensorCode = "TT-UNKNWN", fieldId = a.fieldId, name = "Unknown" });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal("application/problem+json", unknown.Content.Headers.ContentType?.MediaType);
    }
}
