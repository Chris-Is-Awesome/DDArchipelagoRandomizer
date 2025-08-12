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
		AddCutsceneToggle();
		IngameUIManager.RetriggerModifyingOptionsMenuTitleScreen();
	}

	private void AddDeathlinkToggle()
	{
		OptionsToggle optionsToggle = new("DEATHLINK", "UI_ToggleDeathlink", "ToggleDeathlink", [IngameUIManager.RelevantScene.TitleScreen], Archipelago.Instance.ToggleDeathlink, Archipelago.Instance.InitializeDeathlinkToggle);
		IngameUIManager.AddOptionsToggle(optionsToggle);
	}

	private void AddCutsceneToggle()
	{
		OptionsToggle optionsToggle = new("SKIP CUTSCENES", "UI_ToggleSkipCutscenes", "ToggleSkipCutscenes", [IngameUIManager.RelevantScene.TitleScreen], Archipelago.Instance.ToggleSkipCutscenes, Archipelago.Instance.InitializeSkipCutscenes);
		IngameUIManager.AddOptionsToggle(optionsToggle);
	}

}