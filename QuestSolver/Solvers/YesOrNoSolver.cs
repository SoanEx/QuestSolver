﻿using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using QuestSolver.Helpers;
using System.ComponentModel;

namespace QuestSolver.Solvers;

[Description("Yes Solver")]
internal class YesOrNoSolver : DelaySolver
{
    public override uint Icon => 45;

    protected override void Enable()
    {
        Svc.Framework.Update += FrameworkUpdate;
    }

    protected override void Disable()
    {
        Svc.Framework.Update -= FrameworkUpdate;
    }

    private unsafe void FrameworkUpdate(IFramework framework)
    {
        if (MountHelper.InCombat) return;

        var yesOrNo = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno");

        if (yesOrNo == null) return;

        if (_delay.Delay(yesOrNo->IsVisible))
        {
            if (CallbackHelper.Fire(yesOrNo, true, 0))
            {
                Svc.Log.Info("Click Yes");
            }
        }
    }
}
