using Content.Server.GameTicking;
using Content.Server.Speech.Components;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles removing accents when using the accentless trait.
/// </summary>
public sealed class AccentlessSystem : EntitySystem
{
    private readonly List<Type> _accentList = new() { typeof(ReplacementAccentComponent), typeof(LizardAccentComponent) };

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccentlessComponent, ComponentStartup>(RemoveAccents);
    }

    private void RemoveAccents(EntityUid uid, AccentlessComponent component, ComponentStartup args)
    {
        foreach (var accent in _accentList)
        {
            RemComp(uid, accent);
        }
    }
}
