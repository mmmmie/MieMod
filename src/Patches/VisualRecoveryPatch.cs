using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace QuickReload;

[HarmonyPatch(typeof(NRun), nameof(NRun._Ready))]
static class VisualRecoveryPatch
{
    static void Postfix(NRun __instance)
    {
        if (!QuickReloadState.TryConsumePendingVisualRecovery())
        {
            return;
        }

        TaskHelper.RunSafely(RecoverVisualStateAsync(__instance));
    }

    private static async Task RecoverVisualStateAsync(NRun run)
    {
        try
        {
            var tree = run.GetTree();
            if (tree != null)
            {
                // Wait one frame so the scene finishes entering before forcing fade in.
                await run.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }

            var game = NGame.Instance;
            if (game?.Transition != null)
            {
                await game.Transition.FadeIn(0.01f);
                Log.Info("[QUICKRELOAD]: Applied post-reload visual recovery fade-in.");
            }

            NModalContainer.Instance?.Clear();
        }
        catch (Exception ex)
        {
            Log.Warn($"[QUICKRELOAD]: Visual recovery failed: {ex}");
        }
    }
}
