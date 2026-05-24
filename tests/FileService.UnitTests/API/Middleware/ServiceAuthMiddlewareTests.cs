using FileService.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FileService.UnitTests.API.Middleware;

public sealed class ServiceAuthMiddlewareTests
{
    private const string Token = "dev-file-service-token";

    [Fact]
    public async Task InvokeAsync_Returns401WhenTokenHeaderIsMissing()
    {
        var state = CreateState();
        var middleware = CreateMiddleware(state);
        var context = CreateContext();

        await middleware.InvokeAsync(context, CreateConfiguration(Token));

        Assert.False(state.NextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Returns403WhenTokenIsInvalid()
    {
        var state = CreateState();
        var middleware = CreateMiddleware(state);
        var context = CreateContext();
        context.Request.Headers["x-service-token"] = "wrong-token";

        await middleware.InvokeAsync(context, CreateConfiguration(Token));

        Assert.False(state.NextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Returns503WhenTokenIsNotConfigured()
    {
        var state = CreateState();
        var middleware = CreateMiddleware(state);
        var context = CreateContext();
        context.Request.Headers["x-service-token"] = Token;

        await middleware.InvokeAsync(context, CreateConfiguration(string.Empty));

        Assert.False(state.NextCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextWhenTokenMatches()
    {
        var state = CreateState();
        var middleware = CreateMiddleware(state);
        var context = CreateContext();
        context.Request.Headers["x-service-token"] = Token;

        await middleware.InvokeAsync(context, CreateConfiguration(Token));

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

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
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