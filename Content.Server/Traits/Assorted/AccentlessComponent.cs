using Content.Server.Speech.Components;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This is used for the accentless trait
/// </summary>
[RegisterComponent]
public sealed class AccentlessComponent : Component
{
    /// <summary>
    ///     The accents removed by the accentless trait.
    /// </summary>
    [DataField("accentsRemoved")]
    public readonly List<Type> RemovedAccents = new()
    {
        typeof(ReplacementAccentComponent),
        typeof(LizardAccentComponent)
    };
}
