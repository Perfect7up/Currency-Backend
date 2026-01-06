using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

var controllerTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.IsSubclassOf(typeof(ControllerBase)) &&
                t.Name.EndsWith("Controller"))
    .Select(t => t.Name.Replace("Controller", "").ToLower())
    .ToList();

builder.Services.AddOpenApi("v1");
foreach (var controller in controllerTypes)
{
    builder.Services.AddOpenApi(controller, options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = $"{char.ToUpper(controller[0])}{controller.Substring(1)} API";
            document.Info.Description = $"Endpoints for {controller} management.";
            document.Info.Version = "v1";

            var pathsToRemove = document.Paths
                .Where(p => !p.Key.Contains($"/api/{controller}", StringComparison.OrdinalIgnoreCase))
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

        foreach (var controller in controllerTypes)
        {
            options.SwaggerEndpoint($"/openapi/{controller}.json",
                $"{char.ToUpper(controller[0])}{controller.Substring(1)} API");
        }

        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Crypto Tracker API Documentation";
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.UseAuthorization();
app.MapControllers();

app.Run();