using System.Reflection;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;

namespace QuickReload;

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
        if (!QuickReloadState.TryConsumePendingAutoReady())
        {
            Log.Info("[QUICKRELOAD]: AutoReady postfix called, but no pending restart or autoReady is false.");
            return;
        }

        if (__instance is not Node node)
        {
            Log.Warn("[QUICKRELOAD]: AutoReady postfix called, but instance is not a Godot.Node.");
            return;
        }

        var confirm = node.GetNodeOrNull<NButton>((NodePath)"ConfirmButton");
        if (confirm == null)
        {
            Log.Warn("[QUICKRELOAD]: AutoReady postfix called, but ConfirmButton not found.");
            return;
        }

        TaskHelper.RunSafely(WaitForLobbyAndAutoReady(node, confirm, __instance));
    }

    private static async Task WaitForLobbyAndAutoReady(Node node, NButton confirm, object screen)
    {
        var tree = node.GetTree();
        if (tree == null)
        {
            Log.Warn("[QUICKRELOAD]: AutoReady aborted because SceneTree is null.");
            return;
        }

        const int maxFramesToWait = 600; // ~10s at 60fps.
        for (var frame = 0; frame < maxFramesToWait; frame++)
        {
            if (frame % 50 == 0)
            {
                Log.Info($"[QUICKRELOAD]: AutoReady waiting for lobby connectivity, frame {frame}.");
            }
            if (!GodotObject.IsInstanceValid(node) || !GodotObject.IsInstanceValid(confirm))
            {
                Log.Warn("[QUICKRELOAD]: AutoReady aborted because screen/button became invalid.");
                return;
            }

            if (AreAllLobbyPlayersConnected(screen))
            {
                confirm.EmitSignal(NClickableControl.SignalName.Released, confirm);
                NModalContainer.Instance?.Clear();
                QuickReloadState.Clear();
                Log.Info("[QUICKRELOAD]: AutoReady confirm fired after lobby connectivity check passed.");
                return;
            }

            await node.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        Log.Warn("[QUICKRELOAD]: AutoReady timed out waiting for all lobby players to connect; skipping auto-ready.");
        QuickReloadState.Clear();
    }

    private static bool AreAllLobbyPlayersConnected(object screen)
    {
        // All three load screens use an internal _runLobby; if unavailable, do not block auto-ready.
        var runLobby = Traverse.Create(screen).Field<LoadRunLobby>("_runLobby").Value;
        if (runLobby == null)
        {
            return true;
        }

        return runLobby.Run.Players.All(player => runLobby.ConnectedPlayerIds.Contains(player.NetId));
    }
}
