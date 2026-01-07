using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

var controllerNames = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && t.Name.EndsWith("Controller"))
    .Select(t => t.Name.Replace("Controller", "").ToLower())
    .Where(name => !string.IsNullOrEmpty(name))
    .ToList();

builder.Services.AddOpenApi("v1");

foreach (var controller in controllerNames)
{
    builder.Services.AddOpenApi(controller, options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            var displayName = char.ToUpper(controller[0]) + controller.Substring(1);

            document.Info.Title = $"{displayName} API";
            document.Info.Description = $"Endpoints for {controller} management.";
            document.Info.Version = "v1";
            var pathsToRemove = document.Paths
                .Where(pathItem =>
                    pathItem.Value?.Operations == null ||
                    !pathItem.Value.Operations.Values.Any(operation =>
                        operation.Tags?.Any(tag =>
                            tag.Name?.Equals(controller, StringComparison.OrdinalIgnoreCase) ?? false
                        ) ?? false
                    )
                )
                .ToList();

            foreach (var path in pathsToRemove)
            {
                document.Paths.Remove(path.Key);
            }

            return Task.CompletedTask;
        });
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient<ICoinService, CoinGeckoService>();
builder.Services.AddHttpClient<IMarketService, MarketService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddHttpClient<IToolsService, ToolsService>();
builder.Services.AddHttpClient<IChartService, ChartService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Full API v1");

        foreach (var controller in controllerNames)
        {
            var displayName = char.ToUpper(controller[0]) + controller.Substring(1);
            options.SwaggerEndpoint($"/openapi/{controller}.json", $"{displayName} API");
        }

        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Crypto Tracker API Documentation";
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
    });
}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.UseAuthorization();
app.MapControllers();

app.Run();