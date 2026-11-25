using System.Reactive.Linq;
using DynamicData.Kernel;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Networking.ModUpdates;
using NexusMods.Networking.ModUpdates.Mixins;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// An <see cref="IModUpdateService"/> that never contacts Nexus Mods and never reports updates.
/// </summary>
/// <remarks>
/// This fork doesn't poll Nexus Mods for mod updates. With no updates reported, the update
/// columns and the "Update" / "Update all" buttons in the Library stay hidden.
/// The real implementation is <see cref="ModUpdateService"/>.
/// </remarks>
public sealed class DisabledModUpdateService : IModUpdateService
{
    /// <inheritdoc/>
    public Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckAndUpdateModPages(CancellationToken token, bool notify = true, bool throttle = true)
        => Task.FromResult(new PerFeedCacheUpdaterResult<PageMetadataMixin>());

    /// <inheritdoc/>
    public void NotifyForUpdates() { }

    /// <inheritdoc/>
    public IObservable<Optional<ModUpdateOnPage>> GetNewestFileVersionObservable(NexusModsFileMetadata.ReadOnly current, Func<ModUpdateOnPage, ModUpdateOnPage?>? select = null)
        => Observable.Return(Optional<ModUpdateOnPage>.None);

    /// <inheritdoc/>
    public IObservable<Optional<ModUpdatesOnModPage>> GetNewestModPageVersionObservable(NexusModsModPageMetadata.ReadOnly current, Func<ModUpdatesOnModPage, ModUpdatesOnModPage?>? select = null)
        => Observable.Return(Optional<ModUpdatesOnModPage>.None);

    /// <inheritdoc/>
    public Optional<ModUpdatesOnModPage> HasModPageUpdatesAvailable(NexusModsModPageMetadata.ReadOnly current, Func<ModUpdatesOnModPage, ModUpdatesOnModPage?>? select = null)
        => Optional<ModUpdatesOnModPage>.None;

    /// <inheritdoc/>
    public IObservable<bool> HasAnyUpdatesObservable(Func<ModUpdatesOnModPage, ModUpdatesOnModPage?>? select = null)
        => Observable.Return(false);

    /// <inheritdoc/>
    public IEnumerable<(NexusModsModPageMetadataId modPageId, ModUpdatesOnModPage updates)> GetAllModPagesWithUpdates(Func<ModUpdatesOnModPage, ModUpdatesOnModPage?>? select = null)
        => [];
}
