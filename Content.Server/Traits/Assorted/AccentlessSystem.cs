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

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var player = ev.Mob;
        foreach (var t in AccentList)
        {
            if (HasComp(player, t))
                EntityManager.RemoveComponent(player, t);
        }
    }
}
