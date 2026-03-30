using System.Threading;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace QuickReload;

[HarmonyPatch(typeof(NRun), nameof(NRun._Process))]
static class GameplayHotkeyPatch
{
    private static int _restartInProgress;
    private static bool _f5WasPressed;

    static void Postfix()
    {
        bool isPressed = Input.IsKeyPressed(Key.F5);
        if (!isPressed)
        {
            _f5WasPressed = false;
            return;
        }

        if (_f5WasPressed)
        {
            return;
        }
        _f5WasPressed = true;

        if (RunManager.Instance.NetService.Type == NetGameType.Client)
        {
            Log.Info("[QUICKRELOAD]: Ignoring F5 quick reload on client.");
            return;
        }

        if (Interlocked.CompareExchange(ref _restartInProgress, 1, 0) != 0)
        {
            Log.Info("[QUICKRELOAD]: F5 quick reload ignored because another restart is already in progress.");
            return;
        }

        Log.Info("[QUICKRELOAD]: F5 pressed during gameplay, triggering quick reload.");
        TaskHelper.RunSafely(RestartFromHotkey());
    }

    private static async Task RestartFromHotkey()
    {
        try
        {
            await QuickReloadRunner.RestartAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _restartInProgress, 0);
        }
    }
}
