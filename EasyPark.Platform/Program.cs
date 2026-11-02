using System.Reflection;
using EasyPark.Platform.ExtendedFeatures.Application;
using EasyPark.Platform.ExtendedFeatures.Infrastructure.Persistence.EFC.Seeding;
using System.Text;
using EasyPark.Platform.AccessControl.Application.ACL;
using EasyPark.Platform.AccessControl.Application.CommandServices;
using EasyPark.Platform.AccessControl.Application.Internal.CommandServices;
using EasyPark.Platform.AccessControl.Application.Internal.QueryServices;
using EasyPark.Platform.AccessControl.Application.QueryServices;
using EasyPark.Platform.AccessControl.Domain.Repositories;
using EasyPark.Platform.AccessControl.Infrastructure.Persistence.EFC.Repositories;
using EasyPark.Platform.AccessControl.Interfaces.ACL;
using EasyPark.Platform.Iam.Application.CommandServices;
using EasyPark.Platform.Iam.Application.Internal.CommandServices;
using EasyPark.Platform.Iam.Application.Internal.OutboundServices;
using EasyPark.Platform.Iam.Application.Internal.QueryServices;
using EasyPark.Platform.Iam.Application.QueryServices;
using EasyPark.Platform.Iam.Domain.Repositories;
using EasyPark.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;
using EasyPark.Platform.Iam.Infrastructure.Persistence.EFC.Repositories;
using EasyPark.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using EasyPark.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using EasyPark.Platform.ParkingManagement.Application.ACL;
using EasyPark.Platform.ParkingManagement.Application.CommandServices;
using EasyPark.Platform.ParkingManagement.Application.Internal.CommandServices;
using EasyPark.Platform.ParkingManagement.Application.Internal.QueryServices;
using EasyPark.Platform.ParkingManagement.Application.QueryServices;
using EasyPark.Platform.ParkingManagement.Domain.Repositories;
using EasyPark.Platform.ParkingManagement.Infrastructure.Persistence.EFC.Repositories;
using EasyPark.Platform.ParkingManagement.Interfaces.ACL;
using EasyPark.Platform.Profiles.Application.ACL;
using EasyPark.Platform.Profiles.Application.CommandServices;
using EasyPark.Platform.Profiles.Application.Internal.CommandServices;
using EasyPark.Platform.Profiles.Application.Internal.QueryServices;
using EasyPark.Platform.Profiles.Application.QueryServices;
using EasyPark.Platform.Profiles.Domain.Repositories;
using EasyPark.Platform.Profiles.Infrastructure.Persistence.EFC.Repositories;
using EasyPark.Platform.Profiles.Interfaces.ACL;
using EasyPark.Platform.Reservations.Application.ACL;
using EasyPark.Platform.Reservations.Application.CommandServices;
using EasyPark.Platform.Reservations.Application.Internal.CommandServices;
using EasyPark.Platform.Reservations.Application.Internal.QueryServices;
using EasyPark.Platform.Reservations.Application.QueryServices;
using EasyPark.Platform.Reservations.Domain.Repositories;
using EasyPark.Platform.Reservations.Infrastructure.BackgroundJobs;
using EasyPark.Platform.Reservations.Infrastructure.Persistence.EFC.Repositories;
using EasyPark.Platform.Reservations.Interfaces.ACL;
using EasyPark.Platform.Shared.Domain.Repositories;
using EasyPark.Platform.Shared.Infrastructure.Interfaces.ASP.Configuration;
using EasyPark.Platform.Shared.Infrastructure.Interfaces.ASP.Middleware;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Seeding;
using EasyPark.Platform.Shared.Interfaces.REST;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── Hosting: Render (and similar platforms) inject the port in the PORT variable ────────────────
var platformPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(platformPort) && int.TryParse(platformPort, out _))
    builder.WebHost.UseUrls($"http://0.0.0.0:{platformPort}");

// ── Database (MySQL through EF Core, same provider as FruitLogix) ────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "The connection string is missing. Set ConnectionStrings__DefaultConnection (see .env.example).");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString);
    // The initial migration is maintained by hand (see docs). EF Core 9+ would otherwise refuse to migrate
    // when no model snapshot is present.
    options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    if (builder.Environment.IsDevelopment()) options.EnableDetailedErrors();
});

// ── Shared ───────────────────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ExtendedAccess>();
builder.Services.AddScoped<SessionsService>();

// ── IAM ──────────────────────────────────────────────────────────────────────────────────────────
var tokenSettings = builder.Configuration.GetSection(TokenSettings.SectionName).Get<TokenSettings>() ?? new TokenSettings();
if (string.IsNullOrWhiteSpace(tokenSettings.Secret) || tokenSettings.Secret.Length < TokenSettings.MinimumSecretLength)
    throw new InvalidOperationException(
        $"TokenSettings__Secret must contain at least {TokenSettings.MinimumSecretLength} characters.");
if (!builder.Environment.IsDevelopment() && tokenSettings.Secret.Contains("DEVELOPMENT-ONLY", StringComparison.Ordinal))
    throw new InvalidOperationException("The development JWT secret cannot be used outside Development.");

builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection(TokenSettings.SectionName));
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<IUserAccountCommandService, UserAccountCommandService>();
builder.Services.AddScoped<IUserAccountQueryService, UserAccountQueryService>();
builder.Services.AddSingleton<IHashingService, HashingService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// ── Profiles and Vehicles ────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IProfileCommandService, ProfileCommandService>();
builder.Services.AddScoped<IProfileQueryService, ProfileQueryService>();
builder.Services.AddScoped<IVehicleCommandService, VehicleCommandService>();
builder.Services.AddScoped<IVehicleQueryService, VehicleQueryService>();
builder.Services.AddScoped<IProfilesContextFacade, ProfilesContextFacade>();

// ── Parking Management ───────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IParkingFacilityRepository, ParkingFacilityRepository>();
builder.Services.AddScoped<IParkingSpotRepository, ParkingSpotRepository>();
builder.Services.AddScoped<IParkingFacilityCommandService, ParkingFacilityCommandService>();
builder.Services.AddScoped<IParkingFacilityQueryService, ParkingFacilityQueryService>();
builder.Services.AddScoped<IParkingSpotCommandService, ParkingSpotCommandService>();
builder.Services.AddScoped<IParkingSpotQueryService, ParkingSpotQueryService>();
builder.Services.AddScoped<IParkingManagementContextFacade, ParkingManagementContextFacade>();

// ── Reservations ─────────────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IReservationCommandService, ReservationCommandService>();
builder.Services.AddScoped<IReservationQueryService, ReservationQueryService>();
builder.Services.AddScoped<IReservationsContextFacade, ReservationsContextFacade>();
builder.Services.AddHostedService<ReservationExpirationWorker>();

// ── Access Control ───────────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAccessMovementRepository, AccessMovementRepository>();
builder.Services.AddScoped<IParkingStayRepository, ParkingStayRepository>();
builder.Services.AddScoped<IAccessControlCommandService, AccessControlCommandService>();
builder.Services.AddScoped<IAccessControlQueryService, AccessControlQueryService>();
builder.Services.AddScoped<IAccessControlContextFacade, AccessControlContextFacade>();

// ── Authentication (JWT Bearer) and role-based authorization ─────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = tokenSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = tokenSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimsPrincipalExtensions.SubjectClaim,
            RoleClaimType = ClaimsPrincipalExtensions.RoleClaim
        };
    });
builder.Services.AddAuthorization();

// ── Controllers (kebab-case routes, camelCase JSON) ──────────────────────────────────────────────
builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()))
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ── CORS: only the configured origins (comma separated) ──────────────────────────────────────────
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(origin => origin.TrimEnd('/'))
    .ToArray();
builder.Services.AddCors(options => options.AddPolicy("EasyParkWebApplication", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ── OpenAPI (Swashbuckle, as in FruitLogix) with JWT support ─────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EasyPark Platform API",
        Version = "v1",
        Description = "RESTful web services of EasyPark: IAM, Profiles and Vehicles, Parking Management, " +
                      "Reservations and Access Control."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Paste the token returned by POST /api/v1/authentication/sign-in (without the word Bearer).",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
    });
    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile)) options.IncludeXmlComments(xmlFile);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
#pragma warning disable ASPDEPR005
    options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (allowedOrigins.Length == 0)
    app.Logger.LogWarning("Cors:AllowedOrigins is empty: browsers will not be able to call the API.");

// ── Database migration and optional demo data ────────────────────────────────────────────────────
if (app.Configuration.GetValue("Database:Migrate", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    if (app.Configuration.GetValue("SeedDemoData", false))
        await DemoDataSeeder.SeedAsync(context, scope.ServiceProvider.GetRequiredService<IHashingService>());
    await ExtendedFeaturesSeeder.SeedTemplatesAsync(context);
}

// ── HTTP pipeline ────────────────────────────────────────────────────────────────────────────────
app.UseForwardedHeaders();
app.UseCors("EasyParkWebApplication");
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Configuration.GetValue("OpenApi:Enabled", true))
{
    app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "EasyPark Platform API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "EasyPark Platform API";
    });
    app.MapGet("/scalar/v1", () => Results.Content(ApiDocumentationPage.ScalarHtml, "text/html"))
        .ExcludeFromDescription();
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new { status = "healthy", service = "easypark-platform" }))
    .AllowAnonymous().ExcludeFromDescription();
app.MapGet("/health/ready", async (AppDbContext context, CancellationToken cancellationToken) =>
{
    try
    {
        return await context.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ready", database = "connected" })
            : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unavailable");
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Readiness check failed");
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unavailable");
    }
}).AllowAnonymous().ExcludeFromDescription();

app.Run();

/// <summary>Entry point, exposed for the integration tests (WebApplicationFactory).</summary>
public partial class Program
{
}
