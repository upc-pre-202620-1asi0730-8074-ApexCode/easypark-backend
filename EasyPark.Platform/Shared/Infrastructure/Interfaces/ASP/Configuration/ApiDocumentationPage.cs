namespace EasyPark.Platform.Shared.Infrastructure.Interfaces.ASP.Configuration;

/// <summary>
///     Optional Scalar API reference page (loaded from the jsDelivr CDN) that reads the same OpenAPI document
///     used by Swagger UI. No extra NuGet package is required.
/// </summary>
public static class ApiDocumentationPage
{
    public const string ScalarHtml = """
        <!doctype html>
        <html lang="es">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>EasyPark API - Scalar</title>
        </head>
        <body>
          <div id="app"></div>
          <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
          <script>
            Scalar.createApiReference('#app', { url: '/openapi/v1.json' });
          </script>
        </body>
        </html>
        """;
}
