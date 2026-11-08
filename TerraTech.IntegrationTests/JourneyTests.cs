using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Shared.Tb1;
using Xunit;

public partial class JourneyTests(TestServer server) : IClassFixture<TestServer>
{
    private async Task<(HttpClient client, int userId, int profileId, int fieldId)> Account()
    {
        var client = server.CreateClient();
        var email = Guid.NewGuid() + "@test.example";
        var signUp = await client.PostAsJsonAsync("/api/v1/authentication/sign-up", new { emailAddress = email, password = "test-password", confirmPassword = "test-password", fullName = "Demo Farmer" });
        Assert.Equal(HttpStatusCode.Created, signUp.StatusCode);
        var user = await Json(signUp);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications/confirm", new { token = server.Emails.Verifications[email] })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { emailAddress = email, password = "test-password" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await Json(login);
        Assert.Equal("Demo Farmer", auth.GetProperty("fullName").GetString());
        var jwt = new JsonWebToken(auth.GetProperty("token").GetString());
        Assert.InRange((jwt.ValidTo - DateTime.UtcNow).TotalHours, 7.99, 8.01);
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/profiles/me")).StatusCode);
        var profile = await client.PutAsJsonAsync("/api/v1/profiles/me", Profile());
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var pr = await Json(profile);
        var field = await client.PostAsJsonAsync("/api/v1/fields", Field(pr.GetProperty("id").GetInt32()));
        Assert.Equal(HttpStatusCode.Created, field.StatusCode);
        var fr = await Json(field);
        Assert.Equal("Potato", fr.GetProperty("cropName").GetString());
        return (client, user.GetProperty("id").GetInt32(), pr.GetProperty("id").GetInt32(), fr.GetProperty("id").GetInt32());
    }

    static object Profile() => new
    {
        fullName = "Demo Farmer",
        fundoName = "Test Farm",
        contactPhone = "999888777",
        location = "Lima",
        sizeM2 = 10000
    };
    static object Field(int profileId) => new
    {
        profileId,
        name = "North",
        sizeM2 = 5000,
        soilType = "SANDY",
        latitude = -12.0,
        longitude = -77.0,
        cropName = "Potato"
    };
    static async Task<JsonElement> Json(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    async Task<string> Catalog()
    {
        var code = "TT-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        await server.WithDb(async db =>
        {
            db.Add(new SensorCatalogItem { SensorCode = code, MacAddress = "02:" + string.Join(":", Guid.NewGuid().ToByteArray().Take(5).Select(x => x.ToString("X2"))), IsDemo = true });
            await db.SaveChangesAsync();
        });
        return code;
    }

    async Task<int> Sensor(HttpClient client, int fieldId, string? code = null)
    {
        code ??= await Catalog();
        var response = await client.PostAsJsonAsync("/api/v1/devices/register", new { sensorCode = code, fieldId, name = "Demo Soil" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await Json(response);
        Assert.Equal(code, data.GetProperty("sensorCode").GetString());
        return data.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task RegistrationDuplicateAndWrongPassword()
    {
        var client = server.CreateClient();
        var r = new
        {
            emailAddress = Guid.NewGuid() + "@test.example",
            password = "test-password",
            confirmPassword = "test-password",
            fullName = "Test Farmer"
        };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/authentication/sign-up", r)).StatusCode);
        var duplicate = await client.PostAsJsonAsync("/api/v1/authentication/sign-up", r);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("EMAIL_EXISTS", (await Json(duplicate)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { r.emailAddress, password = "wrong" })).StatusCode);
    }

    [Theory]
    [InlineData("a", "bad", "123")]
    [InlineData(" ", "valid@test.example", "longpassword")]
    public async Task InvalidRegistration(string name, string email, string password) => Assert.Equal(HttpStatusCode.BadRequest, (await server.CreateClient().PostAsJsonAsync("/api/v1/authentication/sign-up", new { fullName = name, emailAddress = email, password, confirmPassword = password })).StatusCode);
    [Theory]
    [InlineData("missing")]
    [InlineData("tampered")]
    [InlineData("expired")]
    [InlineData("unknown")]
    public async Task InvalidJwt(string kind)
    {
        var account = await Account();
        var client = server.CreateClient();
        if (kind != "missing")
        {
            var key = kind == "tampered" ? new string('x', 48) : TestServer.Secret;
            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor { Subject = new ClaimsIdentity([new Claim("sub", kind == "unknown" ? "2147483647" : account.userId.ToString())]), Expires = kind == "expired" ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(1), IssuedAt = DateTime.UtcNow.AddHours(-2), NotBefore = DateTime.UtcNow.AddHours(-2), SigningCredentials = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256) });
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        }

        var response = await client.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MissingRoutesAndPublicSwagger()
    {
        var client = server.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/does-not-exist")).StatusCode);
        var swagger = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        var json = await Json(swagger);
        Assert.True(json.GetProperty("paths").TryGetProperty("/api/v1/devices/{id}/readings", out _));
        Assert.True(json.GetProperty("paths").TryGetProperty("/api/v1/devices/{deviceId}/readings/{readingId}", out _));
        var required = json.GetProperty("components").GetProperty("schemas").GetProperty("SignUpResource").GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains("confirmPassword", required);
        Assert.Contains("fullName", required);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/products")).StatusCode);
    }

    [Fact]
    public async Task CompleteJourneyReadingsUnitsRangesAndPersistence()
    {
        var a = await Account();
        var id = await Sensor(a.client, a.fieldId);
        var none = await a.client.GetAsync($"/api/v1/devices/{id}/readings/latest");
        Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
        Assert.Equal("NO_READINGS", (await Json(none)).GetProperty("code").GetString());
        var empty = await Json(await a.client.GetAsync($"/api/v1/devices/{id}/readings"));
        Assert.Empty(empty.GetProperty("readings").EnumerateArray());
        var emptyMonth = await Json(await a.client.GetAsync($"/api/v1/devices/{id}/readings?days=30"));
        Assert.Empty(emptyMonth.GetProperty("readings").EnumerateArray());
        await server.WithDb(async db =>
        {
            foreach (var days in new[]
            {
                1,
                6,
                10,
                29,
                31
            }

            )
                db.Add(new SensorReading { DeviceId = id, RecordedAt = DateTime.UtcNow.AddDays(-days), MoisturePercent = 42, SoilTemperatureC = 23, NitrogenPpm = 35, PhosphorusPpm = 18, PotassiumPpm = 60 });
            await db.SaveChangesAsync();
        });
        var latest = await Json(await a.client.GetAsync($"/api/v1/devices/{id}/readings/latest"));
        Assert.True(latest.GetProperty("isStale").GetBoolean());
        var week = await Json(await a.client.GetAsync($"/api/v1/devices/{id}/readings?days=7"));
        Assert.Equal(2, week.GetProperty("readings").GetArrayLength());
        Assert.Equal(30, week.GetProperty("minimumMoisturePercent").GetDouble());
        var month = await Json(await a.client.GetAsync($"/api/v1/devices/{id}/readings?days=30"));
        var rows = month.GetProperty("readings").EnumerateArray().ToList();
        Assert.Equal(4, rows.Count);
        Assert.Equal(rows.Select(r => r.GetProperty("recordedAt").GetDateTime()).Order(), rows.Select(r => r.GetProperty("recordedAt").GetDateTime()));
        foreach (var row in rows)
        {
            Assert.Equal("SIMULATED", row.GetProperty("source").GetString());
            Assert.Equal(42, row.GetProperty("moisturePercent").GetDouble());
            Assert.Equal(23, row.GetProperty("soilTemperatureC").GetDouble());
            Assert.Equal(35, row.GetProperty("nitrogenPpm").GetDouble());
            Assert.EndsWith("Z", row.GetProperty("recordedAt").GetString());
        }

        await server.WithDb(async db =>
        {
            db.Add(new SensorReading { DeviceId = id, RecordedAt = DateTime.UtcNow, MoisturePercent = 50 });
            await db.SaveChangesAsync();
        });
        latest = await Json(await a.client.GetAsync($"/api/v1/devices/{id}/readings/latest"));
        Assert.False(latest.GetProperty("isStale").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await a.client.GetAsync($"/api/v1/devices/{id}/readings?days=14")).StatusCode);
        var user = await Json(await a.client.GetAsync("/api/v1/users/me"));
        Assert.Equal(a.userId, user.GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await a.client.DeleteAsync($"/api/v1/devices/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await a.client.DeleteAsync($"/api/v1/fields/{a.fieldId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await a.client.DeleteAsync($"/api/v1/profiles/{a.profileId}")).StatusCode);
    }

    [Fact]
    public async Task TwoAccountsCannotReadWriteDeleteOrListOthersResources()
    {
        var a = await Account();
        var b = await Account();
        var id = await Sensor(a.client, a.fieldId);
        foreach (var route in new[]
        {
            $"users/{a.userId}",
            $"profiles/{a.profileId}",
            $"fields/{a.fieldId}",
            $"devices/{id}",
            $"devices/{id}/readings/latest",
            $"devices/{id}/readings",
            $"fields/{a.fieldId}/devices"}

        )
            Assert.Equal(HttpStatusCode.NotFound, (await b.client.GetAsync("/api/v1/" + route)).StatusCode);
        foreach (var route in new[]
        {
            "profiles",
            "fields",
            "devices"
        }

        )
        {
            var list = await Json(await b.client.GetAsync("/api/v1/" + route));
            Assert.DoesNotContain(list.EnumerateArray(), r => r.GetProperty("id").GetInt32() == (route == "profiles" ? a.profileId : route == "fields" ? a.fieldId : id));
        }

        foreach (var route in new[]
        {
            $"profiles/{a.profileId}",
            $"fields/{a.fieldId}",
            $"devices/{id}"}

        )
            Assert.Equal(HttpStatusCode.NotFound, (await b.client.DeleteAsync("/api/v1/" + route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PutAsJsonAsync($"/api/v1/fields/{a.fieldId}", Field(b.profileId))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PostAsJsonAsync("/api/v1/fields", Field(a.profileId))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PostAsJsonAsync("/api/v1/devices/register", new { sensorCode = await Catalog(), fieldId = a.fieldId, name = "Stolen" })).StatusCode);
    }

    [Fact]
    public async Task UnknownOccupiedAndConcurrentSensorRegistration()
    {
        var a = await Account();
        var code = await Catalog();
        Assert.Equal(HttpStatusCode.NotFound, (await a.client.PostAsJsonAsync("/api/v1/devices/register", new { sensorCode = code, fieldId = int.MaxValue, name = "Demo" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.client.PostAsJsonAsync("/api/v1/devices/register", new { sensorCode = "TT-UNKNWN", fieldId = a.fieldId, name = "Demo" })).StatusCode);
        var r = new
        {
            sensorCode = code,
            fieldId = a.fieldId,
            name = "Demo"
        };
        var attempts = await Task.WhenAll(a.client.PostAsJsonAsync("/api/v1/devices/register", r), a.client.PostAsJsonAsync("/api/v1/devices/register", r));
        Assert.Single(attempts, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(attempts, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.Conflict, (await a.client.PostAsJsonAsync("/api/v1/devices/register", r)).StatusCode);
        var b = await Account();
        Assert.Equal(HttpStatusCode.Conflict, (await b.client.PostAsJsonAsync("/api/v1/devices/register", new { sensorCode = code, fieldId = b.fieldId, name = "Occupied" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.client.PostAsJsonAsync("/api/v1/devices", new { fieldId = a.fieldId, macAddress = "AA:BB:CC:DD:EE:FF", status = "ONLINE", lastSync = DateTime.UtcNow })).StatusCode);
    }

    [Fact]
    public async Task ForeignKeysAndUniqueReadingTimestampAreEnforced()
    {
        await server.WithDb(async db =>
        {
            db.Add(new SensorReading { DeviceId = int.MaxValue, RecordedAt = DateTime.UtcNow });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
        var a = await Account();
        var id = await Sensor(a.client, a.fieldId);
        var time = DateTime.UtcNow;
        await server.WithDb(async db =>
        {
            db.Add(new SensorReading { DeviceId = id, RecordedAt = time });
            await db.SaveChangesAsync();
        });
        await server.WithDb(async db =>
        {
            db.Add(new SensorReading { DeviceId = id, RecordedAt = time });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
        await server.WithDb(async db =>
        {
            var error = await Assert.ThrowsAsync<MySqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM devices WHERE id = {id}"));
            Assert.Equal(1451, error.Number);
        });
    }

    [Fact]
    public async Task ProfileThresholdsAndLegacySnakeCaseRemainCompatible()
    {
        var a = await Account();
        var update = await a.client.PutAsJsonAsync($"/api/v1/profiles/{a.profileId}", new { fundo_name = "Legacy", contact_phone = "999888777", moisture_threshold = 45, temp_threshold = 28 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        await a.client.PutAsJsonAsync("/api/v1/profiles/me", Profile());
        var profile = await Json(await a.client.GetAsync("/api/v1/profiles/me"));
        Assert.Equal(45, profile.GetProperty("moistureThreshold").GetDouble());
        Assert.Equal(10000, profile.GetProperty("sizeM2").GetDouble());
    }

    [Fact]
    public async Task AncillaryPrivateResourcesAlsoEnforceOwnership()
    {
        var a = await Account();
        var b = await Account();
        var deviceId = await Sensor(a.client, a.fieldId);
        async Task<int> Create(string route, object resource)
        {
            var response = await a.client.PostAsJsonAsync("/api/v1/" + route, resource);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await Json(response)).GetProperty("id").GetInt32();
        }

        var product = await Create("products", new { name = "Demo Product", description = "Test", price = 10, type = "SOIL", imageUrl = "https://example.test/product" });
        var report = await Create("reports", new { deviceId, generatedAt = DateTime.UtcNow, meanValue = 42, variance = 1, standardDeviation = 1, technicalInterpretation = "Manual statistics" });
        var inventory = await Create("inventories", new { name = "Demo Harvest", fieldId = a.fieldId, profileId = a.profileId, quantity = 20, unitType = "SACKS", averageWeightPerUnitKg = 50.0m, price = 10.0m, imageUrl = "https://example.test/product" });
        var order = await Create("orders", new { profileId = a.profileId, productId = product, quantity = 1, paymentMethod = 0, isSubscription = false });
        var notification = await Create("notifications", new { profileId = a.profileId, title = "Demo", message = "Test", isAlert = false });
        var community = await Create("community-profiles", new { profileId = a.profileId, nickname = "Farmer", reputationScore = 0, publicBio = "Demo", visibilityStatus = 1 });
        var comment = await Create("comments", new { userId = a.userId, productId = product, targetProfileId = b.profileId, content = "Private", rating = 4 });
        foreach (var (route, id) in new[]
        {
            ("reports", report),
            ("inventories", inventory),
            ("orders", order),
            ("notifications", notification)
        }

        )
        {
            Assert.Equal(HttpStatusCode.NotFound, (await b.client.GetAsync($"/api/v1/{route}/{id}")).StatusCode);
            var list = await Json(await b.client.GetAsync($"/api/v1/{route}"));
            Assert.DoesNotContain(list.EnumerateArray(), r => r.GetProperty("id").GetInt32() == id);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await b.client.GetAsync($"/api/v1/community-profiles/{a.profileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PutAsJsonAsync($"/api/v1/inventories/{inventory}", new { stockQuantity = 5 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PutAsJsonAsync($"/api/v1/orders/{order}/validates", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PutAsJsonAsync($"/api/v1/notifications/{notification}/reads", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PutAsJsonAsync($"/api/v1/reports/{report}", new { meanValue = 5, variance = 1, standardDeviation = 1, technicalInterpretation = "Intrusion" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PutAsJsonAsync($"/api/v1/comments/{comment}", new { content = "Intrusion", rating = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.DeleteAsync($"/api/v1/comments/{comment}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.DeleteAsync($"/api/v1/community-profiles/{community}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PostAsJsonAsync("/api/v1/reports", new { deviceId, generatedAt = DateTime.UtcNow, meanValue = 42, variance = 1, standardDeviation = 1, technicalInterpretation = "Intrusion" })).StatusCode);
        var publicClient = server.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await publicClient.GetAsync($"/api/v1/comments/{comment}")).StatusCode);
        await a.client.PutAsJsonAsync($"/api/v1/community-profiles/{community}", new { nickname = "Farmer", reputationScore = 0, publicBio = "Public Demo", visibilityStatus = 0 });
        Assert.Equal(HttpStatusCode.OK, (await publicClient.GetAsync($"/api/v1/community-profiles/{a.profileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await publicClient.PostAsJsonAsync("/api/v1/products", new { name = "x", description = "x", price = 1, type = "x", imageUrl = "x" })).StatusCode);
    }

    private async Task<AppDbContext> LegacyDatabase()
    {
        var config = new MySqlConnectionStringBuilder(server.Connection);
        config.Database = "terratech_tb1_legacy_" + Guid.NewGuid().ToString("N");
        var name = config.Database;
        var connection = config.ConnectionString;
        config.Database = "";
        await using var mysql = new MySqlConnection(config.ConnectionString);
        await mysql.OpenAsync();
        await new MySqlCommand($"CREATE DATABASE `{name}`", mysql).ExecuteNonQueryAsync();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection).Options);
        await db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync("20260702085937_Initial");
        return db;
    }

    [Fact]
    public async Task IncrementalMigrationPreservesLegacyDevicesAndAssociations()
    {
        await using var db = await LegacyDatabase();
        await db.Database.ExecuteSqlRawAsync("INSERT INTO users (id,email_address,password_hash) VALUES (1,'legacy@test.example','unused')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO profiles (id,user_id,fundo_name,contact_phone,moisture_threshold,temp_threshold) VALUES (1,1,'Legacy','999',30,35)");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO fields (id,profile_id,name,size_m2,soil_type,latitude,longitude) VALUES (1,1,'Legacy',100,'SANDY',0,0)");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO devices (id,field_id,mac_address,status,last_sync) VALUES (123,1,'aa-bb-cc-dd-ee-ff','OFFLINE','2026-01-01')");
        await MigrationPreflight.Check(db);
        await db.Database.MigrateAsync();
        var device = await db.Set<Device>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal(123, device.Id);
        Assert.Equal(1, device.FieldId.Value);
        Assert.Equal("aa-bb-cc-dd-ee-ff", device.MacAddress.Value);
        Assert.Equal("TT-00003F", device.SensorCode);
        var sensor = await db.Set<SensorCatalogItem>().SingleAsync();
        Assert.Equal(device.MacAddress.Value, sensor.MacAddress);
        Assert.False(sensor.IsDemo);
        await db.Database.MigrateAsync();
        Assert.Single(await db.Set<Device>().IgnoreQueryFilters().ToListAsync());
        var legacyField = await db.Set<Field>().IgnoreQueryFilters().SingleAsync();
        Assert.Null(legacyField.Boundary);
        Assert.Equal(100, legacyField.SizeM2.Value);
    }

    [Fact]
    public async Task PreflightRejectsOrphansAndDuplicatesWithoutDeletingData()
    {
        await using var db = await LegacyDatabase();
        await db.Database.ExecuteSqlRawAsync("INSERT INTO devices (field_id,mac_address,status,last_sync) VALUES (999,'AA:BB:CC:DD:EE:FF','OFFLINE','2026-01-01'),(999,'aa-bb-cc-dd-ee-ff','OFFLINE','2026-01-01')");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => MigrationPreflight.Check(db));
        Assert.Contains("devices without fields: 2", error.Message);
        Assert.Contains("duplicate normalized device MACs: 1", error.Message);
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM devices";
        Assert.Equal(2, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task LegacyMacUsesSameRegistryAndOwnedCrudWorks()
    {
        var a = await Account();
        var b = await Account();
        var code = await Catalog();
        string mac = "";
        await server.WithDb(async db => mac = (await db.Set<SensorCatalogItem>().SingleAsync(x => x.SensorCode == code)).MacAddress);
        var r = new
        {
            fieldId = a.fieldId,
            macAddress = mac,
            status = "OFFLINE",
            lastSync = DateTime.UtcNow
        };
        var created = await a.client.PostAsJsonAsync("/api/v1/devices", r);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await Json(created)).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await a.client.PostAsJsonAsync("/api/v1/devices", r)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PostAsJsonAsync("/api/v1/devices", r)).StatusCode);
        var update = new
        {
            macAddress = mac,
            status = "ONLINE",
            lastSync = DateTime.UtcNow
        };
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PutAsJsonAsync($"/api/v1/devices/{id}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.client.PutAsJsonAsync($"/api/v1/profiles/{a.profileId}", new { fundo_name = "Intrusion", contact_phone = "999", moisture_threshold = 10, temp_threshold = 10 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.client.PutAsJsonAsync($"/api/v1/devices/{id}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.client.PutAsJsonAsync($"/api/v1/devices/{id}", new { macAddress = "11:22:33:44:55:66", status = "ONLINE", lastSync = DateTime.UtcNow })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await a.client.DeleteAsync($"/api/v1/devices/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await a.client.PostAsJsonAsync("/api/v1/devices", r)).StatusCode);
    }

    async Task<(int exit, string output)> Command(string environment, params string[] arguments)
    {
        var executable = Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? throw new InvalidOperationException("Set DOTNET_ROOT for command tests."), "dotnet");
        var start = new System.Diagnostics.ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory
        };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        foreach (var arg in arguments)
            start.ArgumentList.Add(arg);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["ConnectionStrings__DefaultConnection"] = server.Connection;
        start.Environment["TokenSettings__Secret"] = TestServer.Secret;
        using var process = System.Diagnostics.Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await stdout + await stderr);
    }

    [Fact]
    public async Task ExplicitDemoCommandsAreRepeatableAndBlockedInProduction()
    {
        var first = await Command("Development", "--demo-catalog");
        Assert.True(first.exit == 0, first.output);
        Assert.Equal(0, (await Command("Development", "--demo-catalog")).exit);
        var a = await Account();
        var id = await Sensor(a.client, a.fieldId, "TT-ZZZ001");
        var readings = await Command("Development", "--demo-readings", "--device-id", id.ToString());
        Assert.True(readings.exit == 0, readings.output);
        int count = 0;
        await server.WithDb(async db => count = await db.Set<SensorReading>().IgnoreQueryFilters().CountAsync(x => x.DeviceId == id));
        Assert.Equal(720, count);
        Assert.Equal(0, (await Command("Development", "--demo-readings", "--device-id", id.ToString())).exit);
        await server.WithDb(async db => Assert.Equal(count, await db.Set<SensorReading>().IgnoreQueryFilters().CountAsync(x => x.DeviceId == id)));
        var blocked = await Command("Production", "--demo-readings", "--device-id", id.ToString());
        Assert.NotEqual(0, blocked.exit);
        Assert.Contains("allowed only in Development", blocked.output);
    }
}
