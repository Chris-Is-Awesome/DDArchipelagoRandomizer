using UnityEngine;
using UnityEngine.SceneManagement;
using static DDoor.ArchipelagoRandomizer.Archipelago;

namespace DDoor.ArchipelagoRandomizer;

internal class ArchipelagoRandomizerMod
{
	private static readonly ArchipelagoRandomizerMod instance = new();
	private GameObject archipelagoRandomizer;
	private bool hasInited;

	public static ArchipelagoRandomizerMod Instance => instance;

	private ArchipelagoRandomizerMod() { }

	/// <summary>
	/// Called only when an Archipelago save file is created. Useful for saving data to save file.
	/// </summary>
	public void OnFileCreated()
	{
		GameSave.currentSave.SetKeyState("ArchipelagoRandomizer", true);

		InitMod();
	}

	/// <summary>
	/// Initializes the mod. This runs the first time an Archipelago save file is loaded in a session.
	/// </summary>
	public void InitMod()
	{
		if (hasInited)
		{
			return;
		}

		SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) =>
		{
			if (SceneManager.GetActiveScene().name == "TitleScreen" && scene.name != "TitleScreen")
			{
				DisableMod();
			}
		};

		hasInited = true;
	}

	/// <summary>
	/// Enables the mod. This runs every time an Archipelago save file is loaded.
	/// </summary>
	public void EnableMod(APSaveData connectionInfo)
	{
		if (!hasInited)
		{
			InitMod();
		}

		ConnectToArchipelago(connectionInfo);
	}

	private async void ConnectToArchipelago(APSaveData connectionInfo)
	{
		try
		{
			// Once connected, start item randomizer
			if (await Archipelago.Instance.Connect(connectionInfo) != null)
			{
				archipelagoRandomizer = new GameObject("ArchipelagoRandomizer");
				ItemRandomizer itemRando = archipelagoRandomizer.AddComponent<ItemRandomizer>();
				EntranceRandomizer entranceRandomizer = archipelagoRandomizer.AddComponent<EntranceRandomizer>();
				GoalModifications goalMods = archipelagoRandomizer.AddComponent<GoalModifications>();
				MapManager mapManager = archipelagoRandomizer.AddComponent<MapManager>();
				TrapManager trapManager = archipelagoRandomizer.AddComponent<TrapManager>();
				archipelagoRandomizer.AddComponent<DeathManager>();
				Object.DontDestroyOnLoad(archipelagoRandomizer);
				UIManager.Instance.HideConnectionMenu();
				SaveMenu saveMenu = TitleScreen.instance.saveMenu;
				saveMenu.saveSlots[saveMenu.index].LoadSave();
				GameSave.currentSave.SetKeyState("ArchipelagoRandomizer", true);
				GameSave.currentSave.Save();
			}
		}
		catch (LoginValidationException ex)
		{
			Logger.LogError(ex.Message);
		}
	}

	/// <summary>
	/// Disables the mod. This runs every time you load into TitleScreen
	/// </summary>
	private void DisableMod()
	{
		SceneManager.sceneLoaded -= MapManager.Instance.UpdateMap;
		SceneManager.sceneLoaded -= ItemRandomizer.Instance.TriggerGroveofSpiritsDoorCheck;
		SceneManager.sceneLoaded -= CutsceneFlags.RemoveOfficeBlocker;
		SceneManager.sceneLoaded -= CutsceneFlags.ActivateShopKeep;
		CutsceneFlags.skippedCutscenes = false;
		Object.Destroy(archipelagoRandomizer);
		Archipelago.Instance.Disconnect();
	}
}