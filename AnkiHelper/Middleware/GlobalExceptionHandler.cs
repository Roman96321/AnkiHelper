using Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace AnkiHelper.Middleware;

public static class GlobalExceptionHandler
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features
                    .Get<IExceptionHandlerFeature>()
                    ?.Error;

                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(GlobalExceptionHandler));

                if (exception is not null)
                {
                    logger.LogError(exception, "Unhandled exception.");
                }

                var (statusCode, title) = exception switch
                {
                    ConflictException conflict => (StatusCodes.Status409Conflict, conflict.Message),
                    ArgumentException argument => (StatusCodes.Status400BadRequest, argument.Message),
                    UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized."),
                    _ => (StatusCodes.Status500InternalServerError, "Unexpected server error.")
                };

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                await Results.Problem(
                    title: title,
                    statusCode: statusCode)
                    .ExecuteAsync(context);
            });
        });

        return app;
    }
}
