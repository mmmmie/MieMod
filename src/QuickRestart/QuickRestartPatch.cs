using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;

namespace MieMod.QuickRestart;

[HarmonyPatch(typeof(NPauseMenu), nameof(NPauseMenu._Ready))]
static class QuickRestartPatch
{
    private const string QuickRestartNodeName = "MieMod_QuickRestartButton";
    private static readonly LocString RestartLoc = new("gameplay_ui", "PAUSE_MENU.RESTART");

    static void Postfix(NPauseMenu __instance)
    {
        var buttonContainer = __instance.GetNodeOrNull<VBoxContainer>("PanelContainer/ButtonContainer");
        if (buttonContainer == null)
        {
            Log.Warn("[MIEMOD]: Quick Restart: couldn't find button container.");
            return;
        }

        if (buttonContainer.GetNodeOrNull<Node>(QuickRestartNodeName) != null)
        {
            Log.Warn("[MIEMOD]: Quick Restart: button already exists, skipping.");
            return;
        }

        var saveAndQuitButton = buttonContainer.GetNode<NPauseMenuButton>("SaveAndQuit");
        var restartButton = saveAndQuitButton.Duplicate((int)(
            Node.DuplicateFlags.Groups |
            Node.DuplicateFlags.Scripts |
            Node.DuplicateFlags.UseInstantiation
        )) as NPauseMenuButton;

        if (restartButton == null)
        {
            Log.Warn("[MIEMOD]: Quick Restart: failed to duplicate template button.");
            return;
        }

        restartButton.Name = QuickRestartNodeName;
        restartButton.GetNode<MegaLabel>("Label").SetTextAutoSize(RestartLoc.GetFormattedText());
        MakeVisualsUnique(restartButton);

        var pauseMenu = saveAndQuitButton.GetParent();
        pauseMenu.AddChild(restartButton);
        pauseMenu.MoveChild(restartButton, saveAndQuitButton.GetIndex());

        ConnectFocusNeighbors(buttonContainer, restartButton);

        restartButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => OnQuickRestartPressed(__instance))
        );

        Log.Info("[MIEMOD]: Quick Restart button added.");
    }

    private static void ConnectFocusNeighbors(VBoxContainer buttonContainer, NPauseMenuButton restartButton)
    {
        var buttons = new List<NPauseMenuButton>();
        foreach (var button in buttonContainer.GetChildren())
        {
            if (button is NPauseMenuButton { Visible: true } pauseMenuButton)
            {
                buttons.Add(pauseMenuButton);
            }
        }

        var index = buttons.IndexOf(restartButton);
        if (index <= 0 || index >= buttons.Count - 1)
        {
            Log.Warn("[MIEMOD]: Quick Restart: unexpected button ordering, skipping focus neighbor update.");
            return;
        }

        var previousButton = buttons[index - 1];
        var nextButton = buttons[index + 1];

        previousButton.FocusNeighborBottom = restartButton.GetPath();
        restartButton.FocusNeighborTop = previousButton.GetPath();
        nextButton.FocusNeighborTop = restartButton.GetPath();
        restartButton.FocusNeighborBottom = nextButton.GetPath();
    }

    private static void MakeVisualsUnique(NPauseMenuButton button)
    {
        var image = button.GetNodeOrNull<TextureRect>("ButtonImage");
        if (image?.Material != null)
        {
            image.Material = image.Material.Duplicate() as Material;
        }

        var label = button.GetNodeOrNull<CanvasItem>("Label");
        if (label?.Material != null)
        {
            label.Material = label.Material.Duplicate() as Material;
        }
    }

    private static void OnQuickRestartPressed(NPauseMenu pauseMenu)
    {
        Log.Info("[MIEMOD]: Quick Restart pressed.");
        TaskHelper.RunSafely(QuickRestartRunner.RestartAsync(pauseMenu));
    }
}
