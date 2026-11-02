using System.Net;
using System.Net.Http.Json;
using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyPark.Platform.Tests.Integration;

public class ExtendedFeaturesApiTests
{
    [Fact]
    public async Task Database_contains_exactly_fifteen_business_entity_tables()
    {
        using var factory = new EasyParkApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var names = db.Model.GetEntityTypes().Select(x => x.GetTableName()).Distinct().ToHashSet();
        foreach (var name in new[] { "alert_rules", "alerts", "notification_templates", "notifications",
                     "occupancy_metrics", "reports", "refresh_tokens" }) Assert.Contains(name, names);
        Assert.Equal(15, names.Count);
    }

    [Fact]
    public async Task Sign_in_returns_rotating_refresh_token_and_replay_is_rejected()
    {
        using var factory = new EasyParkApiFactory();
        using var client = factory.CreateClient();
        var email = ApiClientExtensions.UniqueEmail("session");
        var signed = await client.SignUpAndSignInAsync(email, "DRIVER");
        using var refreshClient = factory.CreateClient();
        refreshClient.WithToken(signed.token);
        var session = await refreshClient.PostAsync("/api/v1/sessions", null);
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        var body = await session.ReadJsonAsync();
        var raw = body.GetProperty("refreshToken").GetString()!;
        var renewed = await client.PostAsJsonAsync("/api/v1/sessions/refresh", new { refreshToken = raw });
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        var replay = await client.PostAsJsonAsync("/api/v1/sessions/refresh", new { refreshToken = raw });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Anonymous_requests_to_alerts_are_rejected()
    {
        using var factory = new EasyParkApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/alerts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Driver_cannot_access_operator_alerts()
    {
        using var factory = new EasyParkApiFactory();
        using var client = factory.CreateClient();
        var signed = await client.SignUpAndSignInAsync(ApiClientExtensions.UniqueEmail("driver"), "DRIVER");
        client.WithToken(signed.token);
        var response = await client.GetAsync("/api/v1/alerts");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
