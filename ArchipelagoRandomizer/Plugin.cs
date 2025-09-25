using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine.Events;
using AGM = DDoor.AlternativeGameModes;

namespace DDoor.ArchipelagoRandomizer;

[BepInPlugin("deathsdoor.archipelagorandomizer", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("deathsdoor.itemchanger")]
[BepInDependency("deathsdoor.alternativegamemodes")]
[BepInDependency("deathsdoor.magicui")]
[BepInDependency("deathsdoor.adduitooptionsmenu")]
[BepInDependency("deathsdoor.returntospawn", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	private static Plugin instance;
	private Harmony harmony;

	public static Plugin Instance => instance;
	public int InitStatus { get; internal set; } = 0;

#nullable enable
	internal event UnityAction? OnUpdate;

	private void Awake()
	{
		instance = this;

		try
		{
			Logger = base.Logger;
			Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

			AGM.AlternativeGameModes.Add("ARCHIPELAGO", () =>
			{
				ArchipelagoRandomizerMod.Instance.OnFileCreated();
			});

			harmony = new Harmony("deathsdoor.archipelagorandomizer");
			harmony.PatchAll();
			UIManager.Instance.AddOptionsMenuItems();
			UIManager.Instance.CheckPluginVersion();

			InitStatus = 1;


		}
		catch (System.Exception err)
		{
			InitStatus = 2;
			throw err;
		}
	}

	private void Update()
	{
		OnUpdate?.Invoke();
	}

	private static void DisableItemChangerShortcutDoorTriggerOverride()
	{
		// For Entrance Randomization, we have to wrap IC's ShortcutDoor Trigger Prefix with some additional code to prevent receiving checks when coming through a door
		// Thus, we unpatch it here and then redo the patch in EntranceRandomizer.cs
		Instance.harmony.Unpatch(AccessTools.Method(typeof(ShortcutDoor), nameof(ShortcutDoor.Trigger)), HarmonyPatchType.Prefix, "deathsdoor.itemchanger");
	}
}

#nullable disable