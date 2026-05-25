using System.Text.Json.Serialization;
using DotNetEnv;
using FileService.API.Middleware;
using FileService.Application;
using FileService.Infrastructure;
using FileService.API.Swagger;
using Microsoft.OpenApi;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
        options.AddSecurityDefinition("ServiceToken", new OpenApiSecurityScheme
        {
                Name = "x-service-token",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Shared file-service token"
        });

        options.OperationFilter<ServiceTokenOperationFilter>();
});
builder.Services.AddControllers()
        .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
        app.UseSwagger();
        app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ServiceAuthMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
