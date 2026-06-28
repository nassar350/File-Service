using System.Text.Json.Serialization;
using DotNetEnv;
using FileService.API.Middleware;
using FileService.Application;
using FileService.Infrastructure;
using FileService.API.Swagger;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.HttpOverrides;

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.PreSerializeFilters.Add((swagger, httpReq) =>
        {
            var prefix = httpReq.Headers["X-Forwarded-Prefix"].ToString();
            var scheme = httpReq.Headers["X-Forwarded-Proto"].ToString();
            var host = httpReq.Headers["X-Forwarded-Host"].ToString();

            if (string.IsNullOrWhiteSpace(scheme))
            {
                scheme = httpReq.Scheme;
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                host = httpReq.Host.Value;
            }

            if (!string.IsNullOrWhiteSpace(prefix) && !prefix.StartsWith('/'))
            {
                prefix = "/" + prefix;
            }

            swagger.Servers = new List<OpenApiServer>
            {
                new()
                {
                    Url = $"{scheme}://{host}{prefix}"
                }
            };
        });
    });

    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("OpenCors");
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ServiceAuthMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
