using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// Injects Nexus API keys into HTTP messages.
/// </summary>
public class ApiKeyMessageFactory(IConnection conn) : BaseHttpMessageFactory, IAuthenticatingMessageFactory
{
    /// <summary>
    /// The name of the environment variable that contains the API key.
    /// </summary>
    public const string NexusApiKeyEnvironmentVariable = "NEXUS_API_KEY";
    
    private string? EnvironmentApiKey => Environment.GetEnvironmentVariable(NexusApiKeyEnvironmentVariable);

    /// <summary>
    /// The API key to use: a stored key takes precedence over the environment variable.
    /// Returns <c>null</c> if neither is set.
    /// </summary>
    public string? TryGetApiKey()
    {
        var value = NexusMods.Networking.NexusWebApi.Auth.ApiKey.Get(conn.Db);
        if (!string.IsNullOrWhiteSpace(value)) return value;

        var environmentValue = EnvironmentApiKey;
        return string.IsNullOrWhiteSpace(environmentValue) ? null : environmentValue;
    }

    /// <summary>
    /// Whether an API key is available.
    /// </summary>
    public bool HasApiKey => TryGetApiKey() is not null;

    private string ApiKey => TryGetApiKey() ?? throw new Exception("No API key set");

    /// <inheritdoc />
    public override async ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri)
    {
        var requestMessage = await base.Create(method, uri);
        requestMessage.Headers.Add("apikey", ApiKey);
        return requestMessage;
    }

    /// <inheritdoc />
    public override ValueTask<bool> IsAuthenticated()
    {
        return ValueTask.FromResult(HasApiKey);
    }

    /// <summary>
    /// Sets the API key used for future requests.
    /// </summary>
    /// <param name="apiKey">The new API key set.</param>
    public async ValueTask SetApiKey(string apiKey)
    {
        await Auth.ApiKey.Set(conn, apiKey.Trim());
    }

    /// <summary>
    /// Removes the stored API key. A key provided via the environment variable is unaffected.
    /// </summary>
    public async ValueTask ClearApiKey()
    {
        var ids = Auth.ApiKey.All(conn.Db).Select(e => e.Id).ToArray();
        if (ids.Length == 0) return;

        using var tx = conn.BeginTransaction();
        foreach (var id in ids) tx.Delete(id, false);
        await tx.Commit();
        await conn.Excise(ids);
    }

    /// <inheritdoc/>
    public async ValueTask<UserInfo?> Verify(INexusApiClient nexusApiNexusApiClient, CancellationToken token)
    {
        var result = await nexusApiNexusApiClient.Validate(token);
        return new UserInfo
        {
            UserId = result.Data.UserId,
            Name = result.Data.Name,
            UserRole = result.Data.IsPremium ? UserRole.Premium : result.Data.IsSupporter ? UserRole.Supporter : UserRole.Free,
            AvatarUrl = result.Data.ProfileUrl,
        };
    }
}
