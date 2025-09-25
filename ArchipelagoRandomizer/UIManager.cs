using DDoor.AddUIToOptionsMenu;
using DDoor.ArchipelagoRandomizer.UI;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using UnityEngine;

namespace DDoor.ArchipelagoRandomizer;

internal class UIManager
{
	public static readonly UIManager instance = new();
	public static UIManager Instance => instance;
	private static readonly ConnectionMenu connectionMenu = new();
	private static readonly NotificationPopup notificationHandler = new();

	private static string UpdateVersion = "";
	private static bool UpdateAvailable = false;

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
		OptionsToggle optionsToggle = new(itemText: "SKIP CUTSCENES", gameObjectName: "ARCHIPELAGO_UI_ToggleSkipCutscenes", id: "ToggleSkipCutscenes", relevantScenes: [IngameUIManager.RelevantScene.TitleScreen], toggleAction: Archipelago.Instance.ToggleSkipCutscenes, toggleValueInitializer: Archipelago.Instance.InitializeSkipCutscenes, contextText: "BUTTON:CONFIRM Toggle Skip Cutscenes BUTTON:BACK Back");
		IngameUIManager.AddOptionsMenuItem(optionsToggle);
	}

	internal void CheckPluginVersion()
	{
		UpdateVersion = MyPluginInfo.PLUGIN_VERSION;
		try
		{
			HttpWebRequest Request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/Chris-Is-Awesome/DDArchipelagoRandomizer/releases");
			Request.UserAgent = "request";
			HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
			StreamReader Reader = new StreamReader(response.GetResponseStream());
			string JsonResponse = Reader.ReadToEnd();
			JArray Releases = JArray.Parse(JsonResponse);
			UpdateVersion = Releases[0]["tag_name"].ToString();
			UpdateAvailable = IsNewerVersion(UpdateVersion.Replace("v", ""));
		}
		catch (Exception e)
		{
			Logger.Log(e.Message);
		}
		if (UpdateAvailable)
		{
			PathUtil.GetByPath("TitleScreen", "UI_PauseCanvas").AddComponent<NotificationDisplay>();
			Logger.Log("Added notification to scene");
		}

		static bool IsNewerVersion(string newVersion)
		{
			Version currentVersion = new Version(MyPluginInfo.PLUGIN_VERSION);
			Version latestVersion = new Version(newVersion);

			return latestVersion.CompareTo(currentVersion) > 0;
		}
	}

	internal void DisplayUpdateNotification()
	{
		ShowNotification($"DDArchipelagoRandomizer update available: {UpdateVersion}");
	}

	private class NotificationDisplay : MonoBehaviour
	{
		private void Start()
		{
			Instance.DisplayUpdateNotification();
		}
	}
	
}