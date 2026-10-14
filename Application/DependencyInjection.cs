using Application.Services.BuildDecks;
using Application.Services.ConfiguredDecks;
using Application.UseCases.Anki.CheckConnection;
using Application.UseCases.Auth.Login;
using Application.UseCases.Auth.Registration;
using Application.UseCases.Auth.RefreshTokens;
using Application.UseCases.Decks.DeckImports.ImportConfigured;
using Application.UseCases.Decks.DeckImports.PrepareConfiguration;
using Application.UseCases.Decks.GetDeckNamesAndIds;
using Application.UseCases.Decks.Synchronization;
using Application.UseCases.Training.SentenceComposition;
using Application.UseCases.Training.TranslationSentence;
using Application.UseCases.Training.WordOrder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<PrepareDeckImportConfigurationUseCase>();
        services.AddScoped<CheckAnkiConnectionUseCase>();
        services.AddScoped<GetDeckNamesAndIdsUseCase>();
        services.AddScoped<ImportConfiguredDecksUseCase>();
        services.AddScoped<DeckSynchronizationUseCase>();
        services.AddScoped<WordOrderUseCase>();
        services.AddScoped<SentenceCompositionUseCase>();
        services.AddScoped<TranslationSentenceUseCase>();
        services.AddScoped<UserRegistrationUseCase>();
        services.AddScoped<UserLoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();

        services.AddScoped<IPreparedImportDecksBuilder, BuildPreparedImportDecks>();
        services.AddScoped<IConfiguredDeckImportBuilder, ConfiguredDeckImportBuilder>();

        return services;
    }
}
