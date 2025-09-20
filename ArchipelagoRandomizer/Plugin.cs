using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using AGM = DDoor.AlternativeGameModes;

namespace DDoor.ArchipelagoRandomizer;

[BepInPlugin("deathsdoor.archipelagorandomizer", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("deathsdoor.itemchanger")]
[BepInDependency("deathsdoor.alternativegamemodes")]
[BepInDependency("deathsdoor.magicui")]
[BepInDependency("deathsdoor.adduitooptionsmenu")]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	private static Plugin instance;

	public static Plugin Instance => instance;
	public int InitStatus { get; internal set; } = 0;

	#nullable enable
	internal event UnityAction? OnUpdate;

	private void Awake()
	{
		instance = this;

		// Only show Unity logs that are errors
		Application.logMessageReceivedThreaded += (condition, stackTrace, type) =>
		{
			if (type == LogType.Log || type == LogType.Warning)
				return;

			Logger.LogError($"{condition}\n{stackTrace}");
		};

		try
		{
			Logger = base.Logger;
			Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

			AGM.AlternativeGameModes.Add("ARCHIPELAGO", ArchipelagoRandomizerMod.Instance.OnFileCreated);

			new Harmony("deathsdoor.archipelagorandomizer").PatchAll();
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

	public static Coroutine StartRoutine(IEnumerator routine)
	{
		return Instance.StartCoroutine(routine);
	}
}

#nullable disable