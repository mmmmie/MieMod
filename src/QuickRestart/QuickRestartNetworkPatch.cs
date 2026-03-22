using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MieMod.QuickRestart.Multiplayer;

namespace MieMod.QuickRestart;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.InitializeRunLobby))]
static class QuickRestartNetworkPatch
{
    static void Postfix(RunManager __instance)
    {
        Log.Info($"[MIEMOD]: RunManager.InitializeRunLobby postfix called on {__instance.NetService.Type}. netId={__instance.NetService.NetId}");
        if (__instance.NetService.Type != NetGameType.Host && __instance.NetService.Type != NetGameType.Client)
        {
            Log.Info("[MIEMOD]: Not a multiplayer run, skipping QuickRestart message handler registration.");
            return;
        }
        __instance.NetService.RegisterMessageHandler<QuickRestartMessage>(
            new MessageHandlerDelegate<QuickRestartMessage>(OnQuickRestartMessage));
        Log.Info($"[MIEMOD]: Registered QuickRestartMessage handler on {__instance.NetService.Type}.");
    }

    static void OnQuickRestartMessage(QuickRestartMessage message, ulong senderId)
    {
        Log.Info($"[MIEMOD]: QuickRestartMessage received from {senderId}. lobbyId={message.lobbyId}");
        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            Log.Info("[MIEMOD]: QuickRestartMessage received on host, ignoring.");
            return;
        }

        QuickRestartState.SetPendingRestart(message.lobbyId);
        Log.Info("[MIEMOD]: QuickRestart pending state set for client.");
    }
}
