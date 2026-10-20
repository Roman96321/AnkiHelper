using Application.Abstractions;
using Application.Abstractions.Anki;
using Application.Abstractions.Auth;
using Application.Abstractions.OpenAI;
using Infrastructure.AnkiConnect;
using Infrastructure.AnkiPackage;
using Infrastructure.Auth;
using Infrastructure.OpenAI;
using Infrastructure.OpenAI.SentenceComposition;
using Infrastructure.OpenAI.TranslationSentence;
using Infrastructure.OpenAI.WordOrder;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var ankiDataSource = configuration["AnkiDataSource"] ?? "AnkiConnect";

        var JwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JwtKey is missing in configuration.");

        var connectionString = configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration
                    .GetSection(nameof(JwtOptions))
                    .Get<JwtOptions>()
                    ?? throw new InvalidOperationException("JwtOptions section was not found.");

                if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
                    throw new InvalidOperationException("JwtOptions:Issuer is missing.");

                if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
                    throw new InvalidOperationException("JwtOptions:Audience is missing.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(JwtKey))
                };
            });

        services.AddHttpClient<AnkiConnectTransport>();
        services.AddScoped<AnkiPackageTransport>();
        services.AddScoped<OpenAITransport>();

        services.AddScoped<IRefreshTokenProvider, RefreshTokenProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();

        if (ankiDataSource == "Apkg")
        {
            services.AddScoped<IAnkiCardIds, ApkgGetCardsId>();
            services.AddScoped<IAnkiCardsInfo, ApkgGetCardsInfo>();
            services.AddScoped<IAnkiNotesInfo, ApkgGetNotesInfo>();
            services.AddScoped<IAnkiDeckNamesAndIds, ApkgGetDeckNamesAndIds>();
        }
        else
        {
            services.AddScoped<IAnkiCardIds, AnkiGetCardsId>();
            services.AddScoped<IAnkiCardsInfo, AnkiGetCardsInfo>();
            services.AddScoped<IAnkiNotesInfo, AnkiGetNotesInfo>();
            services.AddScoped<IAnkiDeckNamesAndIds, AnkiGetDeckNamesAndIds>();        
        }

        services.AddScoped<IAnkiCheckConnection, AnkiCheckConnection>();

        services.AddScoped<IDetermineLanguage, DetermineLanguage>();
        services.AddScoped<IWordOrderQuestionBuilder, WordOrderQuestionBuilder>();
        services.AddScoped<ISentenceCompositionCheckAnswer, SentenceCompositionCheckAnswer>();
        services.AddScoped<ITranslationSentenceExerciseService, TranslationSentenceExerciseService>();

        services.AddScoped<IDeckRepository, DeckRepository>();
        services.AddScoped<ITrainingRepository, TrainingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
