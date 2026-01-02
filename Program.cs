using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
var controllerTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.IsSubclassOf(typeof(ControllerBase)) &&
                t.Name.EndsWith("Controller"))
    .Select(t => t.Name.Replace("Controller", "").ToLower())
    .ToList();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Crypto Tracker API",
        Version = "v1",
        Description = "Complete API Documentation"
    });

    foreach (var controller in controllerTypes)
    {
        c.SwaggerDoc(controller, new OpenApiInfo
        {
            Title = $"{char.ToUpper(controller[0])}{controller.Substring(1)} API",
            Version = "v1",
            Description = $"{char.ToUpper(controller[0])}{controller.Substring(1)}-specific endpoints"
        });
    }

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (docName == "v1")
            return true;

        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"]?.ToLower();
        return docName == controllerName;
    });

});

builder.Services.AddOpenApi();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHttpClient<ICoinService, CoinGeckoService>();
builder.Services.AddHttpClient<IMarketService, MarketService>();
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

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Full API v1");

        foreach (var controller in controllerTypes)
        {
            options.SwaggerEndpoint($"/swagger/{controller}/swagger.json",
                $"{char.ToUpper(controller[0])}{controller.Substring(1)} API");
        }

        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Crypto Tracker API Documentation";
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
        options.DefaultModelsExpandDepth(-1);
        options.DisplayOperationId();
        options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.UseAuthorization();
app.MapControllers();

app.Run();