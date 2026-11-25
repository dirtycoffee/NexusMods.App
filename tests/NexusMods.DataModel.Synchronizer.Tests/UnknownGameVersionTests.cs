using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Synchronizer.Tests;

/// <summary>
/// The game-hashes database is no longer updated, so newer game versions aren't in it. Managing such a game must
/// not treat the whole game folder as external changes, and resetting it must never delete the game.
/// </summary>
public class UnknownGameVersionTests : ACyberpunkIsolatedGameTest<UnknownGameVersionTests>
{
    private static readonly GamePath[] GameFiles =
    [
        new(LocationId.Game, "bin/x64/Cyberpunk2077.exe"),
        new(LocationId.Game, "archive/pc/content/basegame_1_engine.archive"),
        new(LocationId.Game, "engine/config/base/general.ini"),
    ];

    private readonly UniversalStubbedGameLocator<Cyberpunk2077Game> _locator;

    public UnknownGameVersionTests(ITestOutputHelper helper) : base(helper)
    {
        _locator = ServiceProvider.GetServices<IGameLocator>().OfType<UniversalStubbedGameLocator<Cyberpunk2077Game>>().Single();
    }

    private async Task<Loadout.ReadOnly> ManageGameWithUnknownVersion()
    {
        // A version the game-hashes database doesn't know, like a game patch released after it was last updated
        _locator.LocatorIds = [LocatorId.From("unknown-version-manifest")];

        foreach (var file in GameFiles)
        {
            var path = GameInstallation.Locations.ToAbsolutePath(file);
            path.Parent.CreateDirectory();
            await path.WriteAllTextAsync($"original contents of {file.Path}");
        }

        return await CreateLoadout();
    }

    private LoadoutItemWithTargetPath.ReadOnly[] ExternalChanges(Loadout.ReadOnly loadout)
    {
        loadout = loadout.Rebase();
        return LoadoutOverridesGroup.FindByOverridesFor(loadout.Db, loadout.Id)
            .SelectMany(overrides => overrides.AsLoadoutItemGroup().Children.OfTypeLoadoutItemWithTargetPath())
            .ToArray();
    }

    [Fact]
    public async Task GameFilesAreRecordedAsBaselineAndNotAsExternalChanges()
    {
        var loadout = await ManageGameWithUnknownVersion();

        var baseline = GameBaselineFile.FindByGame(Connection.Db, loadout.InstallationId)
            .Select(file => (GamePath)file.Path)
            .ToArray();
        baseline.Should().Contain(GameFiles, "the files found when first managing the game are its original files");

        // This is what the "isn't a clean install" prompt is based on
        ExternalChanges(loadout).Select(item => (GamePath)item.TargetPath)
            .Should().NotContain(GameFiles, "the original game files are not external changes");
    }

    [Fact]
    public async Task FilesAddedLaterAreStillExternalChanges()
    {
        var loadout = await ManageGameWithUnknownVersion();

        var addedFile = new GamePath(LocationId.Game, "bin/x64/plugins/added_later.dll");
        var addedPath = GameInstallation.Locations.ToAbsolutePath(addedFile);
        addedPath.Parent.CreateDirectory();
        await addedPath.WriteAllTextAsync("added after the game was managed");

        loadout = await Synchronizer.Synchronize(loadout.Rebase());

        ExternalChanges(loadout).Select(item => (GamePath)item.TargetPath)
            .Should().Contain(addedFile).And.NotContain(GameFiles);
    }

    [Fact]
    public async Task UnmanagingKeepsTheOriginalGameFiles()
    {
        var loadout = await ManageGameWithUnknownVersion();

        var addedFile = new GamePath(LocationId.Game, "bin/x64/plugins/added_later.dll");
        var addedPath = GameInstallation.Locations.ToAbsolutePath(addedFile);
        addedPath.Parent.CreateDirectory();
        await addedPath.WriteAllTextAsync("added after the game was managed");
        await Synchronizer.Synchronize(loadout.Rebase());

        await ServiceProvider.GetRequiredService<ISynchronizerService>().UnManage(GameInstallation);

        foreach (var file in GameFiles)
        {
            GameInstallation.Locations.ToAbsolutePath(file).FileExists.Should().BeTrue($"{file} is an original game file");
        }

        addedPath.FileExists.Should().BeFalse("files added after managing the game are removed when resetting it");
        GameBaselineFile.FindByGame(Connection.Db, loadout.InstallationId).Should().BeEmpty("unmanaging forgets the baseline");
    }

    [Fact]
    public async Task ResettingWithNoKnownOriginalFilesIsRefused()
    {
        var loadout = await ManageGameWithUnknownVersion();

        // Simulate an installation managed before baselines existed: unknown version and no baseline
        using (var tx = Connection.BeginTransaction())
        {
            foreach (var file in GameBaselineFile.FindByGame(Connection.Db, loadout.InstallationId))
                tx.Delete(file, recursive: false);
            await tx.Commit();
        }

        var reset = async () => await Synchronizer.ResetToOriginalGameState(GameInstallation, [LocatorId.From("unknown-version-manifest")]);
        await reset.Should().ThrowAsync<InvalidOperationException>().WithMessage("Refusing to reset*");

        foreach (var file in GameFiles)
        {
            GameInstallation.Locations.ToAbsolutePath(file).FileExists.Should().BeTrue($"{file} must not be deleted");
        }
    }
}
