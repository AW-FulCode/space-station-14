using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Server.Station.Systems;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research;

public sealed partial class ResearchSystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    private void InitializeServer()
    {
        SubscribeLocalEvent<ResearchServerComponent, ComponentStartup>(OnServerStartup);
        SubscribeLocalEvent<ResearchServerComponent, ComponentShutdown>(OnServerShutdown);
    }

    private void OnServerShutdown(EntityUid uid, ResearchServerComponent component, ComponentShutdown args)
    {
        UnregisterServer(component);
    }

    private void OnServerStartup(EntityUid uid, ResearchServerComponent component, ComponentStartup args)
    {
        RegisterServer(component);
    }

    private bool CanRun(ResearchServerComponent component)
    {
        return this.IsPowered(component.Owner, EntityManager);
    }

    private void UpdateServer(ResearchServerComponent component, int time)
    {
        if (!CanRun(component)) return;
        if (component.SpecialisationPoints.ContainsKey("research-points-basic"))
            component.SpecialisationPoints["research-points-basic"] += PointsPerSecond(component) * time;
        else
            component.SpecialisationPoints.Add("research-points-basic", PointsPerSecond(component) * time);
    }

    public bool RegisterServerClient(ResearchServerComponent component, ResearchClientComponent clientComponent)
    {
        // Has to be on the same station
        if (_stationSystem.GetOwningStation(component.Owner) != _stationSystem.GetOwningStation(clientComponent.Owner))
            return false;

        // TODO: This is shit but I'm just trying to fix RND for now until it gets bulldozed
        if (TryComp<ResearchPointSourceComponent>(clientComponent.Owner, out var source))
        {
            if (component.PointSources.Contains(source)) return false;
            component.PointSources.Add(source);
            source.Server = component;
        }

        if (component.Clients.Contains(clientComponent)) return false;
        component.Clients.Add(clientComponent);
        clientComponent.Server = component;
        return true;
    }

    public void UnregisterServerClient(ResearchServerComponent component, ResearchClientComponent clientComponent)
    {
        if (TryComp<ResearchPointSourceComponent>(clientComponent.Owner, out var source))
        {
            component.PointSources.Remove(source);
        }

        component.Clients.Remove(clientComponent);
        clientComponent.Server = null;
    }

    public bool IsTechnologyUnlocked(ResearchServerComponent component, TechnologyPrototype prototype,
        TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!Resolve(component.Owner, ref databaseComponent, false)) return false;
        return databaseComponent.IsTechnologyUnlocked(prototype.ID);
    }

    public bool CanUnlockTechnology(ResearchServerComponent component, TechnologyPrototype technology, TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!Resolve(component.Owner, ref databaseComponent, false))
            return false;

        //check by required points dict, loop through tech req types, first check if they are present, then if there are enough points
        if (!databaseComponent.CanUnlockTechnology(technology) ||
            IsTechnologyUnlocked(component, technology, databaseComponent))
            return false;

        foreach (KeyValuePair<string, int> requirement in technology.RequiredPoints)
        {
            if (!component.SpecialisationPoints.ContainsKey(requirement.Key))
                return false;

            if (component.SpecialisationPoints[requirement.Key] < requirement.Value)
                return false;
        }

        return true;
    }

    public bool UnlockTechnology(ResearchServerComponent component, TechnologyPrototype prototype,
        TechnologyDatabaseComponent? databaseComponent = null)
    {

        if (!Resolve(component.Owner, ref databaseComponent, false)) return false;

        if (!CanUnlockTechnology(component, prototype, databaseComponent)) return false;
        var result = UnlockTechnology(databaseComponent, prototype);
        //negate the specific points type
        if (result)
        {
            foreach (KeyValuePair<string, int> requirement in prototype.RequiredPoints)
            {
                if (!component.SpecialisationPoints.ContainsKey(requirement.Key))
                    return false;
                component.SpecialisationPoints[requirement.Key] -= requirement.Value;
            }
        }
        return result;
    }

    public int PointsPerSecond(ResearchServerComponent component)
    {
        var points = 0;

        // Is our machine powered, and are we below our limit of passive point gain? only works for basic points
        if (CanRun(component) && component.SpecialisationPoints["research-points-basic"] < (component.PassiveLimitPerSource * component.PointSources.Count))
        {
            foreach (var source in component.PointSources)
            {
                if (CanProduce(source)) points += source.PointsPerSecond;
            }
        }

        return points;
    }
}
