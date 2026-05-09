using CustomerApi.API.Middleware;
using CustomerApi.API.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CustomerApi.API;

public static class DependencyInjection
{
    // — Swagger ————————————————————————————————————————
    private const string SwaggerDocVersion  = "v1";
    private const string SwaggerDisplayName = "Customer API v1";
    private const string SwaggerRoutePrefix = "swagger";
    private const string SwaggerDescription = """
        A RESTful API for managing customer records built with .NET 9 Clean Architecture.

        ## Authentication

        All endpoints require **HTTP Basic Authentication**.

        **How to authenticate in Swagger UI:**
        1. Click the **Authorize** button (padlock icon, top right of this page).
        2. Enter the username and password configured for this environment.
        3. Click **Authorize**, then **Close**.
        4. All "Try it out" requests will now include the correct header automatically.

        **How to authenticate via curl:**
        ```
        curl -u <username>:<password> http://localhost:5104/api/v1/customers
        ```
        """;

    // — Auth ———————————————————————————————————————————
    private const string SecuritySchemeId = "BasicAuth";  // OpenAPI definition name
    private const string HttpScheme       = "basic";       // RFC 7617 HTTP scheme

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SwaggerOptions>(configuration.GetSection(SwaggerOptions.SectionName));

        services.AddOptions<BasicAuthOptions>()
            .BindConfiguration(BasicAuthOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Username),
                $"{BasicAuthOptions.SectionName}:{nameof(BasicAuthOptions.Username)} is not configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Password),
                $"{BasicAuthOptions.SectionName}:{nameof(BasicAuthOptions.Password)} is not configured.")
            .ValidateOnStart();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => ConfigureSwagger(options, configuration));

        return services;
    }
    public static WebApplication UsePresentation(this WebApplication app)
    {
        app.UseExceptionHandler();
        if (!app.Environment.IsDevelopment())
            app.UseHttpsRedirection();
        app.UseMiddleware<BasicAuthMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/{SwaggerRoutePrefix}/{SwaggerDocVersion}/swagger.json", SwaggerDisplayName);
                options.RoutePrefix = SwaggerRoutePrefix;
                options.DisplayRequestDuration();
            });
        }

        return app;
    }
    private static void ConfigureSwagger(SwaggerGenOptions options, IConfiguration configuration)
    {
        var swagger = configuration.GetSection(SwaggerOptions.SectionName).Get<SwaggerOptions>() ?? new SwaggerOptions();

        AddSwaggerDocument(options, swagger);
        AddSwaggerSecurity(options);
        AddXmlComments(options);
    }
    private static void AddSwaggerDocument(SwaggerGenOptions options, SwaggerOptions swagger)
    {
        options.SwaggerDoc(SwaggerDocVersion, new OpenApiInfo
        {
            Title       = swagger.Title,
            Version     = SwaggerDocVersion,
            Description = SwaggerDescription,
            Contact     = new OpenApiContact { Name = swagger.ContactName },
        });
    }
    private static void AddSwaggerSecurity(SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(SecuritySchemeId, new OpenApiSecurityScheme
        {
            Name        = "Authorization",
            Type        = SecuritySchemeType.Http,
            Scheme      = HttpScheme,
            In          = ParameterLocation.Header,
            Description = "HTTP Basic Authentication. Use the credentials configured for this environment.",
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = SecuritySchemeId
                    }
                },
                []
            }
        });
    }
    private static void AddXmlComments(SwaggerGenOptions options)
    {
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
    }
}
