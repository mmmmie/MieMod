using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace MieMod.QuickRestart;

static class QuickRestartRunner
{
    public static async Task RestartAsync(NPauseMenu pauseMenu)
    {
        Log.Info("[MIEMOD]: Closing to menu...");
        DisablePauseMenuButtons(pauseMenu);

        SerializableRun serializableRun;
        RunState runState;

        try
        {
            ReadSaveResult<SerializableRun>? readRunSaveResult = SaveManager.Instance.LoadRunSave();
            serializableRun = readRunSaveResult.SaveData
                ?? throw new InvalidOperationException("Run save data was null.");
            runState = RunState.FromSerializable(serializableRun);
        }
        catch (Exception ex)
        {
            Log.Error($"[MIEMOD]: Save validation failed: {ex}");
            return;
        }

        var game = NGame.Instance ?? throw new InvalidOperationException("NGame.Instance was null during quick restart.");

        var runMusicController = NRunMusicController.Instance;
        runMusicController?.StopMusic();
        await game.Transition.FadeOut(0.3f);
        RunManager.Instance.CleanUp();

        try
        {
            RunManager.Instance.SetUpSavedSinglePlayer(runState, serializableRun);
            SfxCmd.Play(runState.Players[0].Character.CharacterTransitionSfx);
            game.ReactionContainer.InitializeNetworking((INetGameService)new NetSingleplayerGameService());
            await game.LoadRun(runState, serializableRun.PreFinishedRoom);
        }
        catch (Exception ex)
        {
            Log.Error($"[MIEMOD]: Run load failed after cleanup: {ex}");
            await game.ReturnToMainMenu();
        }
    }

    private static void DisablePauseMenuButtons(NPauseMenu pauseMenu)
    {
        var buttonContainer = pauseMenu.GetNode<VBoxContainer>("PanelContainer/ButtonContainer");
        foreach (var button in buttonContainer.GetChildren())
        {
            if (button is NPauseMenuButton pauseMenuButton)
            {
                pauseMenuButton.Disable();
            }
        }
    }
}
