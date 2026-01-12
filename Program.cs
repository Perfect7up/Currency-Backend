using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

var controllerNames = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && t.Name.EndsWith("Controller"))
    .Select(t => t.Name.Replace("Controller", "").ToLower())
    .Where(name => !string.IsNullOrEmpty(name))
    .ToList();

void ConfigureJwtSecurity(OpenApiDocument document)
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

    var scheme = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    };
    document.Components.SecuritySchemes.TryAdd("Bearer", scheme);
    document.Security ??= new List<OpenApiSecurityRequirement>();

    var requirement = new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    };

    document.Security.Add(requirement);
}

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        ConfigureJwtSecurity(document);
        return Task.CompletedTask;
    });
});

foreach (var controller in controllerNames)
{
    builder.Services.AddOpenApi(controller, options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            if (document.Info != null)
            {
                var displayName = char.ToUpper(controller[0]) + controller.Substring(1);
                document.Info.Title = $"{displayName} API";
                document.Info.Version = "v1";
            }

            ConfigureJwtSecurity(document);

            if (document.Paths != null)
            {
                var pathsToRemove = document.Paths
                    .Where(pathItem =>
                        pathItem.Value?.Operations == null ||
                        !pathItem.Value.Operations.Values.Any(op =>
                            op.Tags != null && op.Tags.Any(t =>
                                t.Name != null && t.Name.Equals(controller, StringComparison.OrdinalIgnoreCase)
                            )
                        )
                    )
                    .ToList();

                foreach (var path in pathsToRemove)
                {
                    document.Paths.Remove(path.Key);
                }
            }

            return Task.CompletedTask;
        });
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHttpClient<ICoinService, CoinGeckoService>();
builder.Services.AddHttpClient<IMarketService, MarketService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddHttpClient<IToolsService, ToolsService>();
builder.Services.AddHttpClient<IChartService, ChartService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Full API v1");
        foreach (var name in controllerNames)
        {
            var displayName = char.ToUpper(name[0]) + name.Substring(1);
            options.SwaggerEndpoint($"/openapi/{name}.json", $"{displayName} API");
        }
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();