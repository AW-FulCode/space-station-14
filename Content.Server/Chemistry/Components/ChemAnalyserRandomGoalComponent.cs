using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components
{

    /// <summary>
    /// What the machine will spawn when the reward condition is met
    /// </summary>
    [ViewVariables]
    public string ResearchDiskReward = string.Empty;

    /// <summary>
    /// Random goal set - if true will pick a goal from a pre-determined dict (of allowed reagent names and disk rewards)
    /// </summary>
    [ViewVariables]
    [DataField("randomGoalSet")]
    public bool RandomGoalSet = false;

    /// <summary>
    /// Reagents that can be selected if random goal is set - and the reward disk given (overrides ResearchDiskReward and all reward filters)
    /// Goals cannot be selected if given reagent is in the analyser - if none can be given that an empty goal should be given
    /// </summary>
    [ViewVariables]
    [DataField("randomGoalPoints")]
    public Dictionary<string, int> RandomGoalPoints = new Dictionary<string, int>();

    /// <summary>
    /// If no new goals can be given because all possible reagents listed are already present, then this shall be set to true
    /// A message should be given via interface and a new goal given only after the machine is emptied
    /// </summary>
    [ViewVariables]
    public bool NeedsEmptying bool = false;

    /// <summary>
    /// if the goal(s) should be the ONLY reagent(s) present within the analyser for the reward
    /// should be made clear to player (Goal: xyz with no reagents)
    /// </summary>
    [ViewVariables]
    [DataField("exclusiveGoal")]
    public bool ExclusiveGoal = false;

}
