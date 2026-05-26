using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using QuickReload.Multiplayer;

namespace QuickReload;

[HarmonyPatch(typeof(RunManager), "InitializeRunLobby")]
static class QuickReloadNetworkPatch
{
    static void Postfix(RunManager __instance)
    {
        Log.Info($"[QUICKRELOAD]: RunManager.InitializeRunLobby postfix called on {__instance.NetService.Type}. netId={__instance.NetService.NetId}");
        if (__instance.NetService.Type != NetGameType.Host && __instance.NetService.Type != NetGameType.Client)
        {
            Log.Info("[QUICKRELOAD]: Not a multiplayer run, skipping QuickReload message handler registration.");
            return;
        }
        __instance.NetService.RegisterMessageHandler<QuickReloadMessage>(
            new MessageHandlerDelegate<QuickReloadMessage>(OnQuickReloadMessage));
        Log.Info($"[QUICKRELOAD]: Registered QuickReloadMessage handler on {__instance.NetService.Type}.");
    }

    static void OnQuickReloadMessage(QuickReloadMessage message, ulong senderId)
    {
        Log.Info($"[QUICKRELOAD]: QuickReloadMessage received from {senderId}. playerId={message.playerId}");
        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            Log.Info("[QUICKRELOAD]: QuickReloadMessage received on host, ignoring.");
            return;
        }

        QuickReloadState.SetPendingRestart(message.playerId);
        Log.Info("[QUICKRELOAD]: QuickReload pending state set for client.");
    }
}
