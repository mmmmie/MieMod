using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;

namespace MieMod.QuickRestart;

[HarmonyPatch]
static class AutoReadyPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NMultiplayerLoadGameScreen),
            nameof(NMultiplayerLoadGameScreen.OnSubmenuOpened));
        yield return AccessTools.Method(typeof(NDailyRunLoadScreen), nameof(NDailyRunLoadScreen.OnSubmenuOpened));
        yield return AccessTools.Method(typeof(NCustomRunLoadScreen), nameof(NCustomRunLoadScreen.OnSubmenuOpened));
    }

    static void Postfix(object __instance)
    {
        if (!QuickRestartState.TryConsumePendingAutoReady())
        {
            Log.Info("[MIEMOD]: AutoReady postfix called, but no pending restart or autoReady is false.");
            return;
        }
        QuickRestartState.Clear();

        if (__instance is not Godot.Node node)
        {
            Log.Warn("[MIEMOD]: AutoReady postfix called, but instance is not a Godot.Node.");
            return;
        }

        var confirm = node.GetNodeOrNull<NButton>((Godot.NodePath)"ConfirmButton");
        if (confirm == null)
        {
            Log.Warn("[MIEMOD]: AutoReady postfix called, but ConfirmButton not found.");
            return;
        }


        confirm.EmitSignal(NClickableControl.SignalName.Released, confirm);
    }
}