using DDoor.AddUIToOptionsMenu;
using DDoor.ArchipelagoRandomizer.UI;
using UnityEngine.SceneManagement;

namespace DDoor.ArchipelagoRandomizer;

internal class UIManager
{
	public static readonly UIManager instance = new();
	private static readonly ConnectionMenu connectionMenu = new();
	private static readonly NotificationPopup notificationHandler = new();

	public static UIManager Instance => instance;

	private UIManager() { }

	public void ShowConnectionMenu()
	{
		connectionMenu.Show();
	}

	public void HideConnectionMenu()
	{
		connectionMenu.Hide();
	}

	public void ShowNotification(string message)
	{
		notificationHandler.Show(message);
	}

	internal void AddOptionsMenuItems()
	{
		AddDeathlinkToggle();
		AddItemHandlingToggle();
		AddCutsceneToggle();
		IngameUIManager.RetriggerModifyingOptionsMenuTitleScreen();
	}

	private void AddDeathlinkToggle()
	{
		OptionsToggle optionsToggle = new(itemText: "DEATHLINK", gameObjectName: "ARCHIPELAGO_UI_ToggleDeathlink", id: "ToggleDeathlink", relevantScenes: [IngameUIManager.RelevantScene.TitleScreen], toggleAction: Archipelago.Instance.ToggleDeathlink, toggleValueInitializer: Archipelago.Instance.InitializeDeathlinkToggle, contextText: "BUTTON:CONFIRM Toggle Deathlink BUTTON:BACK Back");
		IngameUIManager.AddOptionsMenuItem(optionsToggle);
	}

	private void AddItemHandlingToggle()
	{
		OptionsToggle optionsToggle = new(itemText: "FAST ITEMS", gameObjectName: "ARCHIPELAGO_UI_ToggleItemHandling", id: "ToggleItemHandling", relevantScenes: [IngameUIManager.RelevantScene.TitleScreen, IngameUIManager.RelevantScene.InGame], toggleAction: Archipelago.Instance.ToggleItemHandling, toggleValueInitializer: Archipelago.Instance.InitializeItemHandling, contextText: "BUTTON:CONFIRM Toggle Fast Items BUTTON:BACK Back");
		IngameUIManager.AddOptionsMenuItem(optionsToggle);
	}

	private void AddCutsceneToggle()
	{
		OptionsToggle optionsToggle = new("SKIP CUTSCENES", "UI_ToggleSkipCutscenes", "ToggleSkipCutscenes", [IngameUIManager.RelevantScene.TitleScreen], Archipelago.Instance.ToggleSkipCutscenes, Archipelago.Instance.InitializeSkipCutscenes);
		IngameUIManager.AddOptionsMenuItem(optionsToggle);
	}
}