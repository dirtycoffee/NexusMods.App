using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Sdk.Settings;
using NexusMods.Sdk;
using NexusMods.Sdk.Games;

namespace NexusMods.App.UI.Settings;

/// <summary>
/// Settings that give access to experimental features in the UI.
/// </summary>
public record ExperimentalSettings : ISettings
{
    /// <summary>
    /// Enables games that are not enabled by default.
    /// </summary>
    public bool EnableAllGames { get; [UsedImplicitly] set; } = ApplicationConstants.IsDebug;

    /// <summary>
    /// Uploading collections to Nexus Mods. Always off: collections are not supported by this fork.
    /// </summary>
    [JsonIgnore]
    public bool EnableCollectionSharing => false;

    /// <summary>
    /// Games offered in "My Games" unless <see cref="EnableAllGames"/> is set.
    /// This fork focuses on Cyberpunk 2077; the other games stay registered so existing
    /// loadouts keep working, they just aren't offered for new setups.
    /// </summary>
    [JsonIgnore]
    public readonly GameId[] SupportedGames =
    [
        GameId.From("RedEngine.Cyberpunk2077"),
    ];

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureBackend(StorageBackendOptions.Use(StorageBackends.Json))
            .ConfigureProperty(
                x => x.EnableAllGames,
                new PropertyOptions<ExperimentalSettings, bool>
                {
                    Section = Sections.Experimental,
                    DisplayName = "Enable unsupported games",
                    DescriptionFactory = _ => "Allows you to manage unsupported games.",
                    RequiresRestart = true,
                },
                new BooleanContainerOptions()
            );
    }
}
