
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Hosting;
using NovaTech.TerraTech.Platform.Monitoring.Infrastructure.External.Weather;
using NovaTech.TerraTech.Platform.Monitoring.Application.Internal.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NovaTech.TerraTech.Platform.NotificationManagement.Application.Services;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.NotificationManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Monitoring.Application.Internal.QueryServices;
using NovaTech.TerraTech.Platform.Monitoring.Application.Internal.CommandServices;
using NovaTech.TerraTech.Platform.Shared.Resources;
using NovaTech.TerraTech.Platform.Shared.Resources.Errors;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Interfaces.ASP.Configuration;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Mediator.Cortex.Configuration;
using Cortex.Mediator.Commands;
using Cortex.Mediator.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Application.Internal.CommandServices;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Application.Internal.QueryServices;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Application.Services;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using NovaTech.TerraTech.Platform.Monitoring.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using NovaTech.TerraTech.Platform.StockManagement.Application.Services;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.StockManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.CommercialManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using NovaTech.TerraTech.Platform.Shared.Interfaces.Rest.ProblemDetails;
using NovaTech.TerraTech.Platform.Iam.Application.Acl;
using NovaTech.TerraTech.Platform.Iam.Application.CommandServices;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.CommandServices;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.QueryServices;
using NovaTech.TerraTech.Platform.Iam.Application.QueryServices;
using NovaTech.TerraTech.Platform.Iam.Domain.Repository;
using NovaTech.TerraTech.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;
using NovaTech.TerraTech.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using NovaTech.TerraTech.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using NovaTech.TerraTech.Platform.Iam.Interface.Acl;
using NovaTech.TerraTech.Platform.ProfileManagement.Application.CommandServices;
using NovaTech.TerraTech.Platform.ProfileManagement.Application.Internal.CommandServices;
using NovaTech.TerraTech.Platform.ProfileManagement.Application.Internal.QueryServices;
using NovaTech.TerraTech.Platform.ProfileManagement.Application.QueryServices;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.ProfileManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using NovaTech.TerraTech.Platform.CommunityManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.CommunityManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureCloudRun();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessCheck>("database", tags: ["ready"]);

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new KebabCaseRouteNamingConvention());
    options.Filters.Add<OwnershipFilter>();
    options.Filters.Add<ProblemDetailsMediaTypeFilter>(int.MaxValue);
}).AddDataAnnotationsLocalization();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddScoped<OwnershipFilter>();
builder.Services.AddScoped<SensorRegistrationService>();
builder.Services.AddScoped<ISensorDataRepository, SensorDataRepository>();
builder.Services.AddScoped<SensorReadingQueryService>();
builder.Services.AddScoped<SensorReadingCommandService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<SelfProfileService>();

var jwtSecret = builder.Configuration["TokenSettings:Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException(
        "Set TokenSettings__Secret to a secure key of at least 32 UTF-8 bytes."
    );

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)
            ),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>()
                    .LogWarning(
                        "JWT rejected: {FailureType}",
                        context.Exception.GetType().Name
                    );

                return Task.CompletedTask;
            },

            OnTokenValidated = async context =>
            {
                var id = context.Principal!.UserId();

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();

                if (id <= 0 ||
                    !await db.Set<User>().AnyAsync(
                        u => u.Id == id,
                        context.HttpContext.RequestAborted
                    ))
                {
                    context.Fail("Unknown user.");
                }
            },

            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode = 401;
                context.Response.Headers.WWWAuthenticate = "Bearer";

                await Results.Problem(
                    statusCode: 401,
                    title: "Authentication required.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "UNAUTHENTICATED"
                    }
                ).ExecuteAsync(context.HttpContext);
            }
        };
    });

builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()
);

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAllPolicy",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
    );
});

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionStringTemplate =
        builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionStringTemplate))
        throw new InvalidOperationException(
            "Database connection string is not set in the configuration."
        );

    var connectionString =
        Environment.ExpandEnvironmentVariables(connectionStringTemplate);

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            "Database connection string is not set in the configuration."
        );

    options.UseMySQL(connectionString)
        .UseLoggerFactory(
            serviceProvider.GetRequiredService<ILoggerFactory>()
        )
        .EnableDetailedErrors();
});

builder.Services.AddLocalization(options =>
    options.ResourcesPath = "Resources"
);

builder.Services.AddSingleton<
    IStringLocalizer<ErrorMessages>,
    StringLocalizer<ErrorMessages>
>();

builder.Services.AddSingleton<
    IStringLocalizer<CommonMessages>,
    StringLocalizer<CommonMessages>
>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "NovaTech.TerraTech.Platform",
            Version = "v1",
            Description = "TerraTech Web Service Platform API",
            Contact = new OpenApiContact
            {
                Name = "NovaTech",
            },
            License = new OpenApiLicense
            {
                Name = "Apache 2.0",
                Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
            }
        }
    );

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        }
    );

    options.AddSecurityRequirement(
        document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        }
    );

    options.EnableAnnotations();
    options.OperationFilter<SwaggerSecurityFilter>();
    options.SchemaFilter<RecordRequiredSchemaFilter>();
});

// Repositories and application services

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<
    NovaTech.TerraTech.Platform.Shared.Interfaces.Rest.ProblemDetails.ProblemDetailsFactory
>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddHttpClient<IWeatherForecastClient, OpenMeteoWeatherClient>();
builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();
builder.Services.AddScoped<ISatelliteImageService, SatelliteImageService>();

// Monitoring - Fields
builder.Services.AddScoped<IFieldRepository, FieldRepository>();
builder.Services.AddScoped<IFieldCommandService, FieldCommandService>();
builder.Services.AddScoped<IFieldQueryService, FieldQueryService>();

// Monitoring - Devices
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceCommandService, DeviceCommandService>();
builder.Services.AddScoped<IDeviceQueryService, DeviceQueryService>();

// US13 - Fertility zones
builder.Services.AddScoped<IFertilityZoneRepository, FertilityZoneRepository>();
builder.Services.AddScoped<IFertilityZoneQueryService, FertilityZoneQueryService>();
builder.Services.AddScoped<IFertilityZoneCommandService, FertilityZoneCommandService>();
builder.Services.AddScoped<IFertilityZoneThresholdQueryService, FertilityZoneThresholdQueryService>();
builder.Services.AddScoped<IFertilityZoneThresholdCommandService, FertilityZoneThresholdCommandService>();

builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IProfileCommandService, ProfileCommandService>();
builder.Services.AddScoped<IProfileQueryService, ProfileQueryService>();

builder.Services.AddScoped<ICommunityProfileRepository, CommunityProfileRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommunityProfileService, CommunityProfileService>();
builder.Services.AddScoped<ICommentService, CommentService>();

builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportCommandService, ReportCommandService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();

builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IStockService, StockService>();

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.Configure<TokenSettings>(
    builder.Configuration.GetSection("TokenSettings")
);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();

builder.Services.AddScoped(
    typeof(ICommandPipelineBehavior<>),
    typeof(LoggingCommandBehavior<>)
);

builder.Services.AddCortexMediator([typeof(Program)]);

var app = builder.Build();

if (!app.Environment.IsDevelopment() &&
    args.Any(x => x.StartsWith("--demo-")))
{
    throw new InvalidOperationException(
        "Demo commands are allowed only in Development."
    );
}

if (DatabaseStartup.ShouldMigrate(app.Configuration, app.Environment, args))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();

        await DatabaseInitializer.InitializeAsync(
            context,
            app.Lifetime.ApplicationStopping
        );

        if (args.Contains("--migrate-only"))
            return;

        if (await DemoCommands.Run(app, context, args))
            return;
    }
}

if (CloudRunConfiguration.IsCloudRunService(app.Configuration))
    app.UseForwardedHeaders();

app.UseExceptionHandler();

var supportedCultures = new[]
{
    "en",
    "es"
};

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAllPolicy");

app.UseRouting();

app.Use(async (context, next) =>
{
    if (context.GetEndpoint() == null)
    {
        await Results.Problem(
            statusCode: 404,
            title: "Route was not found."
        ).ExecuteAsync(context);

        return;
    }

    await next();
});

app.UseStatusCodePages(async context =>
{
    var status = context.HttpContext.Response.StatusCode;

    await Results.Problem(
        statusCode: status,
        title: status == 404
            ? "Route was not found."
            : "Request failed."
    ).ExecuteAsync(context.HttpContext);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    }
).AllowAnonymous();

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    }
).AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program
{
}
