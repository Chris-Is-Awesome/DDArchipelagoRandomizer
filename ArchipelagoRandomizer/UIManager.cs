using DDoor.AddUIToOptionsMenu;
using DDoor.ArchipelagoRandomizer.UI;
using UnityEngine.SceneManagement;

namespace DDoor.ArchipelagoRandomizer;

internal class UIManager
{
	public static readonly UIManager instance = new();
	private static readonly ConnectionMenu connectionMenu = new();
	private static readonly NotificationPopup notificationHandler = new();
	private static bool cachedBackgroundSprite = false;

	public static UIManager Instance => instance;

	private UIManager()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode _)
	{
		if (!cachedBackgroundSprite && scene.name == "TitleScreen")
		{
			CustomUI.CacheBackgroundSprite();
		}
	}

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

	internal void AddDeathlinkToggle()
	{
		OptionsToggle optionsToggle = new("DEATHLINK", "UI_ToggleDeathlink", "ToggleDeathlink", [IngameUIManager.RelevantScene.TitleScreen], Archipelago.Instance.ToggleDeathlink, Archipelago.Instance.InitializeDeathlinkToggle);
		IngameUIManager.AddOptionsToggle(optionsToggle);
		IngameUIManager.RetriggerModifyingOptionsMenuTitleScreen();
	}
}