using Content.Shared.Mining;

/// <summary>
/// Defines an entity that triggers a cave in if supports are not provided.
/// </summary>
[RegisterComponent]
public sealed class CaveInComponent : Component
{
    [DataField("requiredSupportRange")]
    public float RequiredSupportRange = 1.0f;
}
