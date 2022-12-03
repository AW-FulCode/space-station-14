using Content.Server.Mining.Components;
using Content.Shared.Destructible;
using Content.Shared.Mining;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Map.Components;

namespace Content.Server.Mining;

/// <summary>
/// This handles creating ores when the entity is destroyed.
/// </summary>
public sealed class MiningSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OreVeinComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OreVeinComponent, DestructionEventArgs>(OnDestruction);
    }

    private void CaveInCheck(EntityUid uid, CaveInComponent component)
    {
        //get the support range of the mined rock
        //check for all entities in range
        //if there are no walls or asteroid rocks in range, spawn rocks within the surrounding area
        var pos = Transform(uid).MapPosition;
        var xform = _entities.GetComponent<TransformComponent>(uid);

        var range = component.SupportRange;
        var impact = component.CollapseRange;
        var supported = false;

        /*foreach (var entity in _lookup.GetEntitiesInRange(pos,range)) {
            if (entity != uid)
            {
                if (EntityManager.TryGetComponent<CaveSupportComponent?>(entity, out var support))
                    supported = true;
            }
        }*/

        var box = Box2.CenteredAround(pos.Position, (range, range));
        var mapGrids = _mapManager.FindGridsIntersecting(pos.MapId, box).ToList();
        var grids = mapGrids.Select(x => x.GridEntityId).ToList();

        //TODO factor support range again - will require some kind of recursive function to track outer supports

        foreach (var grid in mapGrids)
        {
            //cave-in prevention requires TWO supports on opposing sides 
            bool CheckSupportDirs(Vector2i origin, Direction dir1, Direction dir2, bool supported, int range, int count)
            {
                count++;

                if (!supported)
                {
                    //Console.WriteLine("Was not supported before range " + count);
                    // Currently no support for spreading off or across grids.
                    var index1 = origin + dir1.ToIntVec();
                    var index2 = origin + dir2.ToIntVec();     

                    if (EntityManager.TryGetComponent<MetaDataComponent>(uid, out var caveIn))
                    {
                        var support1 = false;
                        foreach (var entity in _lookup.GetEntitiesIntersecting(grid.GridTileToLocal(index1)))
                        {
                            if (entity != uid)
                            {
                                if (EntityManager.TryGetComponent<CaveSupportComponent?>(entity, out var support))
                                    support1 = true;
                            }
                        }

                        //if there is nothing for support but the support range has not been fully expended, check if the support's support exists
                        if (!support1 && range > count)
                        {
                            //TODO maybe find a better way to do this... (compile directions in to a list, iterate through list)
                            support1 = CheckSupportDirs(index1, Direction.North, Direction.South, support1, range, count);
                            support1 = CheckSupportDirs(index1, Direction.North, Direction.SouthEast, support1, range, count);
                            support1 = CheckSupportDirs(index1, Direction.North, Direction.SouthWest, support1, range, count);

                            support1 = CheckSupportDirs(index1, Direction.West, Direction.East, support1, range, count);
                            support1 = CheckSupportDirs(index1, Direction.West, Direction.NorthEast, support1, range, count);
                            support1 = CheckSupportDirs(index1, Direction.West, Direction.SouthEast, support1, range, count);

                            support1 = CheckSupportDirs(index1, Direction.NorthEast, Direction.SouthWest, support1, range, count);
                            support1 = CheckSupportDirs(index1, Direction.NorthEast, Direction.South, support1, range, count);
                            support1 = CheckSupportDirs(index1, Direction.NorthEast, Direction.West, support1, range, count);

                            support1 = CheckSupportDirs(index1, Direction.NorthWest, Direction.SouthEast, support1, range, count);
                            support1 = CheckSupportDirs(index1, Direction.NorthWest, Direction.South, support1, range, count);
                            support1 = CheckSupportDirs(index1, Direction.NorthWest, Direction.East, support1, range, count);
                        }

                        var support2 = false;
                        foreach (var entity in _lookup.GetEntitiesIntersecting(grid.GridTileToLocal(index2)))
                        {
                            if (entity != uid)
                            {
                                if (EntityManager.TryGetComponent<CaveSupportComponent?>(entity, out var support))
                                    support2 = true;
                            }
                        }
                        //if there is nothing for support but the support range has not been fully expended, check if the support's support exists
                        if (!support2 && range > count)
                        {
                            //TODO maybe find a better way to do this... (see above)
                            support2 = CheckSupportDirs(index2, Direction.North, Direction.South, support2, range, count);
                            support2 = CheckSupportDirs(index2, Direction.North, Direction.SouthEast, support2, range, count);
                            support2 = CheckSupportDirs(index2, Direction.North, Direction.SouthWest, support2, range, count);

                            support2 = CheckSupportDirs(index2, Direction.West, Direction.East, support2, range, count);
                            support2 = CheckSupportDirs(index2, Direction.West, Direction.NorthEast, support2, range, count);
                            support2 = CheckSupportDirs(index2, Direction.West, Direction.SouthEast, support2, range, count);

                            support2 = CheckSupportDirs(index2, Direction.NorthEast, Direction.SouthWest, support2, range, count);
                            support2 = CheckSupportDirs(index2, Direction.NorthEast, Direction.South, support2, range, count);
                            support2 = CheckSupportDirs(index2, Direction.NorthEast, Direction.West, support2, range, count);

                            support2 = CheckSupportDirs(index2, Direction.NorthWest, Direction.SouthEast, support2, range, count);
                            support2 = CheckSupportDirs(index2, Direction.NorthWest, Direction.South, support2, range, count);
                            support2 = CheckSupportDirs(index2, Direction.NorthWest, Direction.East, support2, range, count);
                        }
                        if (support1 && support2)
                            supported = true;
                    }
                }

                //Console.WriteLine("supported at range "+ count);
                return supported;
            }

            //TODO maybe find a better way to do this... (see above)
            var origin = grid.TileIndicesFor(xform.Coordinates);
            supported = CheckSupportDirs(origin, Direction.North, Direction.South, supported, range, 0);
            supported = CheckSupportDirs(origin, Direction.North, Direction.SouthEast, supported, range, 0);
            supported = CheckSupportDirs(origin, Direction.North, Direction.SouthWest, supported, range, 0);

            supported = CheckSupportDirs(origin, Direction.West,Direction.East, supported, range, 0);
            supported = CheckSupportDirs(origin, Direction.West, Direction.NorthEast, supported, range, 0);
            supported = CheckSupportDirs(origin, Direction.West, Direction.SouthEast, supported, range, 0);

            supported = CheckSupportDirs(origin, Direction.NorthEast,Direction.SouthWest, supported, range, 0);
            supported = CheckSupportDirs(origin, Direction.NorthEast, Direction.South, supported, range, 0);
            supported = CheckSupportDirs(origin, Direction.NorthEast, Direction.West, supported, range, 0);

            supported = CheckSupportDirs(origin, Direction.NorthWest, Direction.SouthEast, supported, range, 0);
            supported = CheckSupportDirs(origin, Direction.NorthWest, Direction.South, supported, range, 0);
            supported = CheckSupportDirs(origin, Direction.NorthWest, Direction.East, supported, range, 0);
        }

        //TODO factor impact range - will also require some kind of recursive function to track outer impact areas

        if (!supported)
        {

            SoundSystem.Play("/Audio/Effects/explosion1.ogg", Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.2f));

            foreach (var grid in mapGrids)
            {
                void SpreadToDir(Vector2i origin, Direction dir, int range, int count)
                {

                    count++;

                    // Currently no support for spreading off or across grids.
                    var index = origin + dir.ToIntVec();

                    var occupied = false;
                    foreach (var entity in _lookup.GetEntitiesIntersecting(grid.GridTileToLocal(index)))
                    {
                        if (entity != uid)
                        {
                            if (EntityManager.TryGetComponent<CaveSupportComponent?>(entity, out var support))
                                occupied = true;
                        }
                    }

                    if (!occupied && EntityManager.TryGetComponent<MetaDataComponent>(uid, out var caveIn)) { 
                        if (caveIn.EntityPrototype != null)
                        {
                            var newEffect = EntityManager.SpawnEntity(
                                caveIn.EntityPrototype.ID,
                                grid.GridTileToLocal(index));
                        }
                    }

                    if (count < range) {
                        SpreadToDir(index, Direction.North, impact, count);
                        SpreadToDir(index, Direction.NorthEast, impact, count);
                        SpreadToDir(index, Direction.NorthWest, impact, count);
                        SpreadToDir(index, Direction.East, impact, count);
                        SpreadToDir(index, Direction.South, impact, count);
                        SpreadToDir(index, Direction.SouthEast, impact, count);
                        SpreadToDir(index, Direction.SouthWest, impact, count);
                        SpreadToDir(index, Direction.West, impact, count);
                    }
                }

                var origin = grid.TileIndicesFor(xform.Coordinates);
                SpreadToDir(origin,Direction.North,impact,0);
                SpreadToDir(origin, Direction.NorthEast, impact, 0);
                SpreadToDir(origin, Direction.NorthWest, impact, 0);
                SpreadToDir(origin, Direction.East, impact, 0);
                SpreadToDir(origin, Direction.South, impact, 0);
                SpreadToDir(origin, Direction.SouthEast, impact, 0);
                SpreadToDir(origin, Direction.SouthWest, impact, 0);
                SpreadToDir(origin, Direction.West, impact, 0);
            }
        }

    }

    private void OnDestruction(EntityUid uid, OreVeinComponent component, DestructionEventArgs args)
    {
        //run a cave in check
        if (EntityManager.TryGetComponent<CaveInComponent?>(uid, out var caveIn))
            CaveInCheck(uid,caveIn);

        if (component.CurrentOre == null)
            return;

        var proto = _proto.Index<OrePrototype>(component.CurrentOre);

        if (proto.OreEntity == null)
            return;

        var coords = Transform(uid).Coordinates;
        var toSpawn = _random.Next(proto.MinOreYield, proto.MaxOreYield);
        for (var i = 0; i < toSpawn; i++)
        {
            Spawn(proto.OreEntity, coords.Offset(_random.NextVector2(0.3f)));
        }
        
    }

    private void OnMapInit(EntityUid uid, OreVeinComponent component, MapInitEvent args)
    {
        if (component.CurrentOre != null || component.OreRarityPrototypeId == null || !_random.Prob(component.OreChance))
            return;

        component.CurrentOre = _proto.Index<WeightedRandomPrototype>(component.OreRarityPrototypeId).Pick(_random);
    }
}
