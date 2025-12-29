# OpenAPI with Scalar and bearer security

OpenAPI is enabled with Scalar UI and a document transformer that injects bearer auth.

## API setup
- `ConfigureOpenApi` adds OpenAPI services and registers a bearer security transformer:
  ```csharp
  builder.Services.AddOpenApi(options =>
  {
      options.AddDocumentTransformer<BearerSecurityDocumentTransformer>();
  });
  ```
  @/src/api/Peritus.Api/Extensions/Setup/OpenApiExtensions.cs#8-16
- In development, the app maps both the OpenAPI document and Scalar UI:
  ```csharp
  app.MapOpenApi();
  app.MapScalarApiReference();
  ```
  @/src/api/Peritus.Api/Extensions/Setup/OpenApiExtensions.cs#18-24

## Bearer security transformer
- Adds an OpenAPI security scheme when the `Bearer` auth scheme is registered:
  ```csharp
  public sealed class BearerSecurityDocumentTransformer : IOpenApiDocumentTransformer
  {
      public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
      {
          var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
          if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
          {
              document.Components ??= new OpenApiComponents();
              document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
              {
                  ["Bearer"] = new OpenApiSecurityScheme
                  {
                      Type = SecuritySchemeType.Http,
                      Scheme = "bearer",
                      In = ParameterLocation.Header,
                      BearerFormat = "Json Web Token"
                  }
              };
          }
      }
  }
  ```
  @/src/common/Peritus.OpenApi/Transformers/BearerSecurityDocumentTransformer.cs#7-31

## Usage notes
- Scalar UI is available in Development environments to explore and try endpoints.
- Bearer security is reflected in the spec so clients know to send `Authorization: Bearer <token>`.
