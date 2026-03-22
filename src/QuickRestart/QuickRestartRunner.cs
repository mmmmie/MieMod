using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MieMod.QuickRestart.Multiplayer;

namespace MieMod.QuickRestart;

static class QuickRestartRunner
{
    public static async Task RestartAsync(NPauseMenu pauseMenu)
    {
        if (RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
        {
            Log.Info("[MIEMOD]: Single player run detected, restarting.");
            DisablePauseMenuButtons(pauseMenu);
            await RestartSinglePlayer();
        }
        else
        {
            Log.Info("[MIEMOD]: Multiplayer run detected, restarting.");
            DisablePauseMenuButtons(pauseMenu);
            await RestartMultiPlayer();
        }
    }

    private static async Task RestartSinglePlayer()
    {
        SerializableRun serializableRun;
        RunState runState;

        await WaitForPendingSave();
        try
        {
            ReadSaveResult<SerializableRun>? readRunSaveResult = SaveManager.Instance.LoadRunSave();
            serializableRun = readRunSaveResult.SaveData
                              ?? throw new InvalidOperationException("[MIEMOD]: Run save data was null.");
            runState = RunState.FromSerializable(serializableRun);
        }
        catch (Exception ex)
        {
            Log.Error($"[MIEMOD]: Save validation failed: {ex}");
            return;
        }

        var game = NGame.Instance ??
                   throw new InvalidOperationException("NGame.Instance was null during quick restart.");

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

    private static async Task RestartMultiPlayer()
    {
        var game = NGame.Instance ??
                   throw new InvalidOperationException("[MIEMOD]: NGame.Instance was null during quick restart.");

        var netService = RunManager.Instance.NetService;
        if (netService is { IsConnected: true })
        {
            ulong lobbyId = 0;
            var rawLobbyId = netService.GetRawLobbyIdentifier();
            if (!string.IsNullOrEmpty(rawLobbyId) && !ulong.TryParse(rawLobbyId, out lobbyId))
            {
                Log.Warn($"[MIEMOD]: Failed to parse lobby id from '{rawLobbyId}', defaulting to 0.");
            }

            netService.SendMessage(new QuickRestartMessage { lobbyId = lobbyId });
            Log.Info($"[MIEMOD]: Sent QuickRestartMessage before cleanup. lobbyId={lobbyId}");
        }
        else
        {
            Log.Warn("[MIEMOD]: Net service not connected before cleanup, skipping QuickRestartMessage.");
        }

        await game.Transition.FadeOut(0.3f);
        RunManager.Instance.CleanUp();
        await WaitForPendingSave();

        var mainMenu = NMainMenu.Create(false);
        game.RootSceneContainer.SetCurrentScene(mainMenu);
        // mainMenu.SubmenuStack = mainMenu.GetNode<NMainMenuSubmenuStack>((NodePath) "%Submenus");

        NMultiplayerSubmenu? multiplayerSubmenu = mainMenu.OpenMultiplayerSubmenu();

        var platformType = !SteamInitializer.Initialized || CommandLineHelper.HasArg("fastmp")
            ? PlatformType.None
            : PlatformType.Steam;
        var localPlayerId = PlatformUtil.GetLocalPlayerId(platformType);

        // from NMultiplayerSubmenu
        ReadSaveResult<SerializableRun> readSaveResult =
            SaveManager.Instance.LoadAndCanonicalizeMultiplayerRunSave(localPlayerId);
        if (!readSaveResult.Success || readSaveResult.SaveData == null)
        {
            Log.Warn("[MIEMOD]: Broken multiplayer run save detected, big problem");
            return;
        }

        QuickRestartState.SetAutoReady(true);
        multiplayerSubmenu.StartHost(readSaveResult.SaveData);
    }

    private static async Task WaitForPendingSave()
    {
        var currentRunSaveTask = SaveManager.Instance.CurrentRunSaveTask;
        if (currentRunSaveTask != null)
        {
            Log.Info("[MIEMOD]: Saving in progress, waiting for it to be finished before quick restart.");
            try
            {
                await currentRunSaveTask;
            }
            catch (Exception ex)
            {
                Log.Error($"[MIEMOD]: Save task failed while waiting to quick restart: {ex}");
            }
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