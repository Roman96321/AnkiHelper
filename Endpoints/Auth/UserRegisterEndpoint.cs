using Application.Exceptions;
using Application.UseCases.Auth.Registration;
using Microsoft.AspNetCore.Mvc;

namespace AnkiHelper.Endpoints.Auth
{
    public static class UserRegisterEndpoint
    {
        public static void MapUserRegister(this IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/register", async (
                [FromServices] UserRegistrationUseCase source,
                [FromBody] RegisterUserRequest request,
                CancellationToken token) =>
            {
                try
                {
                    await source.Register(request, token);

                    return Results.Ok();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
                catch (ConflictException ex)
                {
                    return Results.Problem(
                        title: ex.Message,
                        statusCode: StatusCodes.Status409Conflict);
                }
                catch (ArgumentException ex)
                {
                    return Results.Problem(
                        title: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest);
                }
            })
            .WithTags("Auth")
            .WithName("RegisterUser");
        }
    }
}
