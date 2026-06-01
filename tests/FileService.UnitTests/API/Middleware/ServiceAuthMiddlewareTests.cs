using FileService.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FileService.UnitTests.API.Middleware;

public sealed class ServiceAuthMiddlewareTests
{
    private const string Token = "dev-file-service-token";

    [Fact]
    public async Task InvokeAsync_Returns401WhenTokenHeaderIsMissing_OnProtectedEndpoint()
    {
        var state = CreateState();
        var middleware = CreateMiddleware(state);
        var context = CreateContext(HttpMethods.Post, "/v1/uploads/sessions/3fa85f64-5717-4562-b3fc-2c963f66afa6/commit");

        await middleware.InvokeAsync(context, CreateConfiguration(Token));

        Assert.False(state.NextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Returns403WhenTokenIsInvalid_OnProtectedEndpoint()
    {
        var state = CreateState();
        var middleware = CreateMiddleware(state);
        var context = CreateContext(HttpMethods.Delete, "/v1/files/3fa85f64-5717-4562-b3fc-2c963f66afa6");
        context.Request.Headers["x-service-token"] = "wrong-token";

        await middleware.InvokeAsync(context, CreateConfiguration(Token));

        Assert.False(state.NextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Returns503WhenTokenIsNotConfigured_OnProtectedEndpoint()
    {
        var state = CreateState();
        var middleware = CreateMiddleware(state);
        var context = CreateContext(HttpMethods.Post, "/v1/uploads/sessions/3fa85f64-5717-4562-b3fc-2c963f66afa6/commit");
        context.Request.Headers["x-service-token"] = Token;

        await middleware.InvokeAsync(context, CreateConfiguration(string.Empty));

        Assert.False(state.NextCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextWhenTokenMatches_OnProtectedEndpoint()
    {
        var state = CreateState();
        var middleware = CreateMiddleware(state);
        var context = CreateContext(HttpMethods.Delete, "/v1/files/3fa85f64-5717-4562-b3fc-2c963f66afa6");
        context.Request.Headers["x-service-token"] = Token;

        await middleware.InvokeAsync(context, CreateConfiguration(Token));

        Assert.True(state.NextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AllowsPublicEndpointWithoutToken()
    {
        var state = CreateState();
        var middleware = CreateMiddleware(state);
        var context = CreateContext(HttpMethods.Get, "/v1/files/3fa85f64-5717-4562-b3fc-2c963f66afa6");

        await middleware.InvokeAsync(context, CreateConfiguration(string.Empty));

        Assert.True(state.NextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static ServiceAuthMiddleware CreateMiddleware(NextCalledState state)
    {
        return new ServiceAuthMiddleware(_ =>
        {
            state.NextCalled = true;
            return Task.CompletedTask;
        });
    }

    private static NextCalledState CreateState() => new();

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = path;
        return context;
    }

    private static IConfiguration CreateConfiguration(string token)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceAuth:Token"] = token
            })
            .Build();
    }

    private sealed class NextCalledState
    {
        public bool NextCalled { get; set; }
    }
}