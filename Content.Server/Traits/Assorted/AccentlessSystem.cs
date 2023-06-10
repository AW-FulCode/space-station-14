using Content.Server.GameTicking;
using Content.Server.Speech.Components;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles...
/// </summary>
public sealed class AccentlessSystem : EntitySystem
{
    private List<Type> AccentList = new() { typeof(ReplacementAccentComponent), typeof(LizardAccentComponent) };

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccentlessComponent, ComponentStartup>(RemoveAccents);
    }

    private void RemoveAccents(EntityUid uid, AccentlessComponent component, ComponentStartup args)
    {
        var player = uid;
        foreach (var t in AccentList)
        {
            if (HasComp(player, t))
                EntityManager.RemoveComponent(player, t);
        }
    }
}
