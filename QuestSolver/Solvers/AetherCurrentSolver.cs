﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using QuestSolver.Helpers;
using System.ComponentModel;
using System.Numerics;

namespace QuestSolver.Solvers;

[Description("Aether Current")]
internal class AetherCurrentSolver : BaseSolver
{
    private AetherCurrent[] Aethers = [];
    private readonly Dictionary<AetherCurrent, Vector3?> _points = [];

    public override uint Icon => 64;

    private unsafe static bool Unlocked(uint aetherId)
    {
        return PlayerState.Instance()->IsAetherCurrentUnlocked(aetherId);
    }

    private void FrameworkUpdate(IFramework framework)
    {
        if (!Available) return;

        var aether = Aethers.FirstOrDefault(a => !Unlocked(a.RowId));

        if (aether == null)
        {
            IsEnable = false;
            return;
        }

        if (!_points.TryGetValue(aether, out var dest))
        {
            var eObj = Svc.Data.GetExcelSheet<EObj>()?.FirstOrDefault(e => e.Data == aether.RowId);
            var level = Svc.Data.GetExcelSheet<Level>()?.FirstOrDefault(e => e.Object == eObj?.RowId);
            _points[aether] = dest = level?.ToLocation();
        }

        if (dest == null)
        {
            IsEnable = false;
            return;
        }

        if (MoveHelper.MoveTo(dest.Value, Svc.ClientState.TerritoryType)) return;
        if (MountHelper.InCombat) return;

        var obj = Svc.Objects.Where(o => o is not IPlayerCharacter
            && o.IsTargetable && !string.IsNullOrEmpty(o.Name.TextValue))
            .MinBy(i => Vector3.DistanceSquared(Player.Object.Position, i.Position));

        if (obj == null) return;
        Svc.Log.Info("Aether!");
        TargetHelper.Interact(obj);
    }

    public override void Enable()
    {
        var set = Svc.Data.GetExcelSheet<AetherCurrentCompFlgSet>()?.FirstOrDefault(s => s.Territory.Row == Svc.ClientState.TerritoryType);

        if (set == null) return;

        Aethers = set.AetherCurrent
            .Where(i => i.Row != 0)
            .Select(i => i.Value)
            .OfType<AetherCurrent>()
            .Where(i => i.Quest.Row == 0)
            .ToArray();

        Svc.Framework.Update += FrameworkUpdate;
    }

    public override void Disable()
    {
        Plugin.Vnavmesh.Stop();
        Svc.Framework.Update -= FrameworkUpdate;
    }
}
