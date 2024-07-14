﻿using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;

namespace QuestSolver.Helpers;
internal static class MoveHelper
{
    private static Vector3 lastPos = default;
    private static DateTime stopTime = DateTime.Now;

    public static bool MoveTo(Level level)
    {
        var destination = level.ToLocation();

        if (level.Radius > 10)
        {
            destination = Plugin.Vnavmesh.PointOnFloor(destination, false, 1) ?? destination;
        }
        return MoveTo(destination, level.Territory.Row);
    }

    public static bool MoveTo(Vector3 destination, uint territoryId)
    {
        if (territoryId != 0 && territoryId != Svc.ClientState.TerritoryType)
        {
            return TeleportHelper.Teleport(destination, territoryId);
        }

#if DEBUG
        unsafe
        {
            var terr = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRow(territoryId);

            AgentMap.Instance()->SetFlagMapMarker(territoryId, terr?.Map.Row ?? 0, destination);
        }
#endif 
        return MoveToInMap(destination);
    }

    //Todo: Fly..
    public static bool MoveToInMap(Vector3 destination)
    {
        if (Plugin.Vnavmesh.IsRunning() && !Plugin.Vnavmesh.PathfindInProgress())
        {
            var dis = Vector3.DistanceSquared(Player.Object.Position, lastPos);
            if (dis < 0.001 && DateTime.Now - stopTime > TimeSpan.FromSeconds(3))
            {
                //TODO: reduce jump!
                MountHelper.TryJump();

                //Re calculate.
                Plugin.Vnavmesh.Stop();
                stopTime = DateTime.Now;
            }
            lastPos = Player.Object.Position;
            return true;
        }

        if (Vector3.DistanceSquared(Player.Object.Position, destination) > 1)
        {
            if (!MountHelper.TryMount())
            {
                if (!Plugin.Vnavmesh.PathfindInProgress())
                {
                    var random = new Random();
                    destination += new Vector3((float)random.NextDouble(), 0, (float)random.NextDouble());
                    Plugin.Vnavmesh.PathfindAndMoveTo(destination, false);
                }
            }
            return true;
        }
        else if (MountHelper.IsMount && MountHelper.InCombat)
        {
            MountHelper.TryDisMount();
        }

        return false;
    }

    public static Vector3 ToLocation(this Level level)
    {
        return new Vector3(level.X, level.Y, level.Z);
    }

    public static bool IsInSide(this Level level)
    {
        if (Svc.ClientState.TerritoryType != level.Territory.Row) return false;
        return level.IsInSide(Player.Object);
    }

    public static bool IsInSide(this Level level, IGameObject obj)
    {
        return level.IsInSide(obj.Position);
    }
    public static bool IsInSide(this Level level, Vector3 position)
    {
        return Vector2.DistanceSquared(level.ToLocation().ToVector2(), position.ToVector2()) <= level.Radius * level.Radius;
    }
}
