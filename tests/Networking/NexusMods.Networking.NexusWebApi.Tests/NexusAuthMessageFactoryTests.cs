using FluentAssertions;
using NexusMods.Networking.NexusWebApi.Auth;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class NexusAuthMessageFactoryTests
{
    private readonly ApiKeyMessageFactory _apiKeyFactory;
    private readonly NexusAuthMessageFactory _factory;

    public NexusAuthMessageFactoryTests(ApiKeyMessageFactory apiKeyFactory, OAuth2MessageFactory oauthFactory)
    {
        _apiKeyFactory = apiKeyFactory;
        _factory = new NexusAuthMessageFactory(apiKeyFactory, oauthFactory);
    }

    [Fact]
    public async Task UsesStoredApiKeyAndFallsBackToOAuthWhenCleared()
    {
        // A key from the environment would take over and make this test meaningless.
        if (Environment.GetEnvironmentVariable(ApiKeyMessageFactory.NexusApiKeyEnvironmentVariable) is not null) return;

        await _apiKeyFactory.ClearApiKey();
        try
        {
            // No key and no OAuth token: the OAuth path is used, which refuses unauthenticated requests.
            (await _factory.IsAuthenticated()).Should().BeFalse();
            var createWithoutLogin = async () => await _factory.Create(HttpMethod.Get, new Uri("https://example.invalid/"));
            await createWithoutLogin.Should().ThrowAsync<Exception>().WithMessage("Unauthorized*");

            // With a stored key, requests use it.
            await _apiKeyFactory.SetApiKey("  test-key  ");
            _apiKeyFactory.TryGetApiKey().Should().Be("test-key");
            (await _factory.IsAuthenticated()).Should().BeTrue();
            using (var request = await _factory.Create(HttpMethod.Get, new Uri("https://example.invalid/")))
            {
                request.Headers.GetValues("apikey").Should().ContainSingle().Which.Should().Be("test-key");
            }

            // Clearing the key falls back to OAuth again.
            await _apiKeyFactory.ClearApiKey();
            _apiKeyFactory.HasApiKey.Should().BeFalse();
            (await _factory.IsAuthenticated()).Should().BeFalse();
        }
        finally
        {
            await _apiKeyFactory.ClearApiKey();
        }
    }
}
