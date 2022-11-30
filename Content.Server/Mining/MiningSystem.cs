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

        var range = component.RequiredSupportRange;
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
            void CheckSupportDirs(Direction dir1, Direction dir2)
            {
                // Currently no support for spreading off or across grids.
                var origin = grid.TileIndicesFor(xform.Coordinates);
                var index1 = origin + dir1.ToIntVec();
                var index2 = origin + dir2.ToIntVec();
                if (!grid.TryGetTileRef(index1, out var tile1) || tile1.Tile.IsEmpty || !grid.TryGetTileRef(index1, out var tile2) || tile2.Tile.IsEmpty)
                    return;

                if (EntityManager.TryGetComponent<MetaDataComponent>(uid, out var caveIn))
                {
                    var support1 = false;
                    foreach (var entity in _lookup.GetEntitiesIntersecting(grid.GridTileToLocal(index1))) {
                        if (entity != uid)
                        {
                            if (EntityManager.TryGetComponent<CaveSupportComponent?>(entity, out var support))
                                support1 = true;
                        }
                    }
                    var support2 = false;
                    foreach (var entity in _lookup.GetEntitiesIntersecting(grid.GridTileToLocal(index2))) {
                        if (entity != uid)
                        {
                            if (EntityManager.TryGetComponent<CaveSupportComponent?>(entity, out var support))
                                support2 = true;
                        }
                    }
                    if (support1 && support2)
                        supported = true;
                }
            }

            CheckSupportDirs(Direction.North, Direction.South);
            CheckSupportDirs(Direction.North, Direction.SouthEast);
            CheckSupportDirs(Direction.North, Direction.SouthWest);

            CheckSupportDirs(Direction.West,Direction.East);
            CheckSupportDirs(Direction.West, Direction.NorthEast);
            CheckSupportDirs(Direction.West, Direction.SouthEast);

            CheckSupportDirs(Direction.NorthEast,Direction.SouthWest);
            CheckSupportDirs(Direction.NorthEast, Direction.South);
            CheckSupportDirs(Direction.NorthEast, Direction.West);

            CheckSupportDirs(Direction.NorthWest, Direction.SouthEast);
            CheckSupportDirs(Direction.NorthWest, Direction.South);
            CheckSupportDirs(Direction.NorthWest, Direction.East);
        }

        //TODO factor impact range - will also require some kind of recursive function to track outer impact areas

        if (!supported)
        {

            SoundSystem.Play("/Audio/Effects/explosion1.ogg", Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.2f));

            foreach (var grid in mapGrids)
            {
                void SpreadToDir(Direction dir)
                {
                    // Currently no support for spreading off or across grids.
                    var origin = grid.TileIndicesFor(xform.Coordinates);
                    var index = origin + dir.ToIntVec();
                    if (!grid.TryGetTileRef(index, out var tile) || tile.Tile.IsEmpty)
                        return;

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
                }

                SpreadToDir(Direction.North);
                SpreadToDir(Direction.NorthEast);
                SpreadToDir(Direction.NorthWest);
                SpreadToDir(Direction.East);
                SpreadToDir(Direction.South);
                SpreadToDir(Direction.SouthEast);
                SpreadToDir(Direction.SouthWest);
                SpreadToDir(Direction.West);
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
