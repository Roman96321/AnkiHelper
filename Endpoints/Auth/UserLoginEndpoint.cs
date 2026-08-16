using Application.UseCases.Auth.Login;
using Microsoft.AspNetCore.Mvc;

namespace AnkiHelper.Endpoints.Auth
{
    public static class UserLoginEndpoint
    {
        public static void MapUserLogin(this IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/login", async (
                [FromServices] UserLoginUseCase source,
                [FromBody] LoginUserRequest request,
                HttpContext httpContext,
                CancellationToken token) =>
            {
                try
                {
                    var response = await source.Login(request, token);

                    RefreshTokenCookie.Append(httpContext.Response, response.RefreshToken);

                    return Results.Ok(new { response.AccessToken });
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Auth")
            .WithName("LoginUser");
        }
    }
}
