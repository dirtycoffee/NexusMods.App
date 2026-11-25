using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A file of the original (vanilla) game, taken from the game folder the first time the game was managed.
/// </summary>
/// <remarks>
/// The app normally knows which files belong to a game from the game-hashes database, looked up by the
/// installed version (e.g. Steam depot manifest ids). That database is no longer updated by Nexus Mods, so
/// newer game versions aren't in it. Without this baseline, every file of such a game would be treated as an
/// "external change", and resetting the game to its original state would delete the whole game folder.
///
/// When the game-hashes database has no files for the installed version, the files found on the first scan are
/// recorded here and used as the game's original files instead.
/// </remarks>
public partial class GameBaselineFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.GameBaselineFile";

    /// <summary>
    /// The path of the file.
    /// </summary>
    public static readonly GamePathParentAttribute Path = new(Namespace, nameof(Path));

    /// <summary>
    /// The hash of the file.
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash));

    /// <summary>
    /// The size of the file (in bytes).
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));

    /// <summary>
    /// The game installation this file belongs to.
    /// </summary>
    public static readonly ReferenceAttribute<Sdk.Games.GameInstallMetadata> Game = new(Namespace, nameof(Game)) { IsIndexed = true };
}
