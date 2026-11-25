using System.Collections.Frozen;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Networking.NexusWebApi.Auth;
using NexusMods.Networking.NexusWebApi.UpdateFilters;
using NexusMods.Networking.NexusWebApi.V1Interop;
using NexusMods.Sdk;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Helps with registration of services for Microsoft DI container.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the Nexus Web API to your DI Container's service collection.
    /// </summary>
    public static IServiceCollection AddNexusWebApi(this IServiceCollection collection, bool? apiKeyAuth = null)
    {
        collection.AddLoginVerbs();

        // Both login methods are always available. NexusAuthMessageFactory uses a personal API key
        // when one is set and falls back to OAuth otherwise. apiKeyAuth: true forces API keys only
        // (used by tests).
        collection
            .AddSingleton<ApiKeyMessageFactory>()
            .AddSingleton<OAuth2MessageFactory>();

        if (apiKeyAuth is true)
        {
            collection
                .AddSingleton<IHttpMessageFactory>(s => s.GetRequiredService<ApiKeyMessageFactory>())
                .AddSingleton<IAuthenticatingMessageFactory>(s => s.GetRequiredService<ApiKeyMessageFactory>());
        }
        else
        {
            collection
                .AddSingleton<NexusAuthMessageFactory>()
                .AddSingleton<IHttpMessageFactory>(s => s.GetRequiredService<NexusAuthMessageFactory>())
                .AddSingleton<IAuthenticatingMessageFactory>(s => s.GetRequiredService<NexusAuthMessageFactory>());
        }
        collection.AddSingleton<OAuth>();
        collection.AddSingleton<IIDGenerator, IDGenerator>();

        collection.AddJWTTokenModel();
        collection.AddApiKeyModel();
        collection.AddSingleton(TimeProvider.System);
        collection.AddIgnoreFileUpdateModel();

        collection.AddGameDomainToGameIdMappingModel();
        collection.AddSingleton<GameDomainToGameIdMappingCache>();
        collection.AddSingleton<IGameDomainToGameIdMappingCache>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<LocalMappingCache>>();

            var didParse = LocalMappingCache.TryParseJsonFile(logger, out var gameIdToDomain, out var gameDomainToId);
            Debug.Assert(didParse);

            if (!didParse) logger.LogWarning("Failed to parse local mapping cache data, all mapping requests will be handled by the fallback cache");
            else
            {
                Debug.Assert(gameIdToDomain is not null && gameDomainToId is not null);
                logger.LogInformation("Loaded local mapping cache data with {Count} entries", gameDomainToId?.Count ?? 0);
            }

            var fallbackCache = serviceProvider.GetRequiredService<GameDomainToGameIdMappingCache>();
            return new LocalMappingCache(logger, gameIdToDomain ?? FrozenDictionary<NexusModsGameId, GameDomain>.Empty, gameDomainToId ?? FrozenDictionary<GameDomain, NexusModsGameId>.Empty, fallbackCache);
        });

        collection
            .AddNexusModsLibraryModels()
            .AddSingleton<IGraphQlClient, GraphQlClient>()
            .AddSingleton<NexusModsLibrary>()
            .AddAllSingleton<ILoginManager, LoginManager>()
            .AddAllSingleton<INexusApiClient, NexusApiClient>()
            .AddSingleton<IModUpdateFilterService, ModUpdateFilterService>()
            .AddAllSingleton<IModUpdateService, ModUpdateService>()
            .AddHostedService<HandlerRegistration>()
            .AddNexusApiVerbs();

        collection
            .AddNexusGraphQLClient()
            .ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                httpClient.BaseAddress = ClientConfig.GraphQlEndpoint;
                httpClient.DefaultRequestHeaders.UserAgent.Add(ApplicationConstants.UserAgent);

                httpClient.DefaultRequestHeaders.Add(BaseHttpMessageFactory.HeaderApplicationName, ApplicationConstants.UserAgent.ApplicationName);
                httpClient.DefaultRequestHeaders.Add(BaseHttpMessageFactory.HeaderApplicationVersion, ApplicationConstants.UserAgent.ApplicationVersion);

                var apiKey = serviceProvider.GetRequiredService<ApiKeyMessageFactory>().TryGetApiKey();
                if (apiKey is not null)
                {
                    httpClient.DefaultRequestHeaders.Add("apikey", apiKey);
                    return;
                }

                var authenticationHeaderValue = serviceProvider.GetRequiredService<IHttpMessageFactory>().GetAuthenticationHeaderValue();
                if (authenticationHeaderValue is null) return;

                httpClient.DefaultRequestHeaders.Authorization = authenticationHeaderValue;
            });

        return collection;
    }
}
