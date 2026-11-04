using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyPark.Platform.Tests.Integration;

/// <summary>
///     Starts the real API in memory. MySQL is replaced by an in-memory SQLite database created from the same
///     EF Core model, so the tests exercise controllers, services, repositories, transactions and constraints.
/// </summary>
public class EasyParkApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public EasyParkApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection",
            "Server=not-used;Database=not-used;User=not-used;Password=not-used;");
        Environment.SetEnvironmentVariable("TokenSettings__Secret", "integration-tests-secret-0123456789-abcdefghij");
        Environment.SetEnvironmentVariable("Database__Migrate", "false");
        Environment.SetEnvironmentVariable("SeedDemoData", "false");
        Environment.SetEnvironmentVariable("Reservations__ExpirationWorkerEnabled", "false");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins", "http://localhost:5173");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            var descriptors = services.Where(descriptor =>
                descriptor.ServiceType == typeof(AppDbContext) ||
                (descriptor.ServiceType.IsGenericType &&
                 descriptor.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext)))).ToList();
            foreach (var descriptor in descriptors) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

/// <summary>Helpers to call the API as a given user.</summary>
public static class ApiClientExtensions
{
    public static string UniqueEmail(string prefix)
    {
        return $"{prefix}.{Guid.NewGuid():N}@test.easypark.pe";
    }

    public static async Task<(int userId, string token)> SignUpAndSignInAsync(this HttpClient client, string email,
        string role, string password = "Password123!")
    {
        var signUp = await client.PostAsJsonAsync("/api/v1/authentication/sign-up", new { email, password, role });
        signUp.EnsureSuccessStatusCode();
        var signIn = await client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { email, password });
        signIn.EnsureSuccessStatusCode();
        var body = await signIn.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("id").GetInt32(), body.GetProperty("token").GetString()!);
    }

    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<int> CreateProfileAsync(this HttpClient client, string firstName, string? companyName = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/profiles",
            new { firstName, lastName = "Test", phone = "+51 900 000 000", companyName, jobTitle = companyName is null ? null : "Admin" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
