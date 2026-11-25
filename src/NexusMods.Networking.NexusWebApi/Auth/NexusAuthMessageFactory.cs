using System.Net.Http.Headers;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// Authenticates Nexus Mods API requests with a personal API key when one is set
/// (stored via the <c>nexus-api-key</c> command or the <c>NEXUS_API_KEY</c> environment
/// variable), and falls back to the OAuth login otherwise.
/// </summary>
/// <remarks>
/// The OAuth login uses the client id of the discontinued official app, which Nexus Mods
/// may revoke at any point. Personal API keys keep working independently of that.
/// The choice is made per request, so setting or removing a key takes effect immediately.
/// </remarks>
public sealed class NexusAuthMessageFactory : IAuthenticatingMessageFactory
{
    private readonly ApiKeyMessageFactory _apiKey;
    private readonly OAuth2MessageFactory _oauth;

    /// <summary>
    /// Constructor.
    /// </summary>
    public NexusAuthMessageFactory(ApiKeyMessageFactory apiKey, OAuth2MessageFactory oauth)
    {
        _apiKey = apiKey;
        _oauth = oauth;
    }

    private IAuthenticatingMessageFactory Current => _apiKey.HasApiKey ? _apiKey : _oauth;

    /// <inheritdoc/>
    public AuthenticationHeaderValue? GetAuthenticationHeaderValue() => Current.GetAuthenticationHeaderValue();

    /// <inheritdoc/>
    public ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri) => Current.Create(method, uri);

    /// <inheritdoc/>
    public ValueTask<bool> IsAuthenticated() => Current.IsAuthenticated();

    /// <inheritdoc/>
    public ValueTask<UserInfo?> Verify(INexusApiClient nexusApiNexusApiClient, CancellationToken token) => Current.Verify(nexusApiNexusApiClient, token);
}
