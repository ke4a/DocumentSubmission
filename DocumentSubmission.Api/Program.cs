using System.Text.Json.Serialization;
using DocumentSubmission.Api.Auth;
using DocumentSubmission.Api.Errors;
using DocumentSubmission.Api.Services;
using DocumentSubmission.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Database. The connection string is read when the DbContext is created, so tests can override it.
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IConfiguration>().GetConnectionString("Database")));

// Authentication: JWT bearer tokens issued by the development-only /auth/login.
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        // Keep the claim names exactly as issued ("sub", "role", "country") instead of mapping them
        // to the long WS-Federation URIs, and tell ASP.NET Core which ones hold the name and the role.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = TokenService.CreateSigningKey(jwt),
            NameClaimType = AuthClaims.UserId,
            RoleClaimType = AuthClaims.Role,
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Editor, policy => policy.RequireRole(Policies.EditorRole))
    .AddPolicy(Policies.Reviewer, policy => policy.RequireRole(Policies.ReviewerRole))
    .AddPolicy(Policies.Participant, policy => policy.RequireRole(Policies.EditorRole, Policies.ReviewerRole));

// Errors: every error response is ProblemDetails (N2).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    // Describe the bearer scheme so the interactive UI can send the token.
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    };
    document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
    return Task.CompletedTask;
}));

builder.Services.AddScoped<SubmissionService>();
builder.Services.AddScoped<ReferenceDataService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Creates the schema and the seed data (N3).
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
// Gives empty error responses (e.g. 401/403 from authorization, 404 for unknown routes) a ProblemDetails body.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // Web UI: the Blazor WebAssembly app in DocumentSubmission.Web, served under /ui from the same origin
    // as the API (so no CORS). Development only, like the login it relies on.
    app.MapStaticAssets();                                              // /ui/index.html, /ui/_framework/...
    app.MapFallbackToFile("ui/{*path:nonfile}", "ui/index.html");       // deep links such as /ui/submissions/{id}
}

app.Run();
