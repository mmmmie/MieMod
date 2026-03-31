using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Runs;

namespace QuickReload;

[HarmonyPatch]
static class RunStartupNetGuardBeginRunPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        var screenTypes = new[]
        {
            typeof(NMultiplayerLoadGameScreen),
            typeof(NDailyRunLoadScreen),
            typeof(NCustomRunLoadScreen)
        };

        foreach (var screenType in screenTypes)
        {
            var beginRun = AccessTools.Method(screenType, nameof(NMultiplayerLoadGameScreen.BeginRun));
            if (beginRun != null)
            {
                yield return beginRun;
            }
        }
    }

    [HarmonyPrefix]
    static void Prefix()
    {
        if (QuickReloadState.TryArmRunStartupNetGuard())
        {
            Log.Info("[QUICKRELOAD]: Armed load-screen net update guard for quick reload startup.");
        }
    }
}

[HarmonyPatch]
static class RunStartupNetGuardProcessPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        var screenTypes = new[]
        {
            typeof(NMultiplayerLoadGameScreen),
            typeof(NDailyRunLoadScreen),
            typeof(NCustomRunLoadScreen)
        };

        foreach (var screenType in screenTypes)
        {
            var process = AccessTools.Method(screenType, nameof(NMultiplayerLoadGameScreen._Process));
            if (process != null)
            {
                yield return process;
            }
        }
    }

    [HarmonyPrefix]
    static bool Prefix()
    {
        return !QuickReloadState.IsRunStartupNetGuardActive();
    }
}

[HarmonyPatch]
static class RunStartupNetGuardSubmenuClosedPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        var screenTypes = new[]
        {
            typeof(NMultiplayerLoadGameScreen),
            typeof(NDailyRunLoadScreen),
            typeof(NCustomRunLoadScreen)
        };

        foreach (var screenType in screenTypes)
        {
            var closed = AccessTools.Method(screenType, nameof(NMultiplayerLoadGameScreen.OnSubmenuClosed));
            if (closed != null)
            {
                yield return closed;
            }
        }
    }

    [HarmonyPrefix]
    static void Prefix()
    {
        QuickReloadState.ClearRunStartupNetGuard();
    }
}

[HarmonyPatch]
static class RunStartupNetGuardRunManagerPatch
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedMultiPlayer))]
    [HarmonyPostfix]
    static void OnSetUpSavedMultiPlayerPostfix()
    {
        TryClearGuard("SetUpSavedMultiPlayer");
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer))]
    [HarmonyPostfix]
    static void OnSetUpNewMultiPlayerPostfix()
    {
        TryClearGuard("SetUpNewMultiPlayer");
    }

    private static void TryClearGuard(string source)
    {
        if (!QuickReloadState.IsRunStartupNetGuardActive())
        {
            return;
        }

        QuickReloadState.ClearRunStartupNetGuard();
        Log.Info($"[QUICKRELOAD]: Cleared load-screen net update guard after {source}.");
    }
}
