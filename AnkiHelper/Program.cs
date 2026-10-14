using AnkiHelper;
using AnkiHelper.Endpoints;
using AnkiHelper.Endpoints.Auth;
using AnkiHelper.Endpoints.Trainings;
using AnkiHelper.Middleware;
using Application;
using Application.Abstractions.Auth;
using Infrastructure;
using System;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services
            .AddApplicationServices()
            .AddInfrastructureServices(builder.Configuration)
            .AddMemoryCache();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.AddAuthorization();

        var clientOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? ["http://localhost:56573"];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("ClientApp", policy =>
            {
                policy
                    .WithOrigins(clientOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        var app = builder.Build();

        app.UseGlobalExceptionHandler();

        app.UseCors("ClientApp");

        app.UseAuthentication();
        app.UseAuthorization();

        var api = app.MapGroup("/api");

        api.MapAnkiEndpoints();
        api.MapDeckEndpoints();
        api.MapTrainingEndpoints();
        api.MapAuthEndpoints();

        app.Run();
    }
}