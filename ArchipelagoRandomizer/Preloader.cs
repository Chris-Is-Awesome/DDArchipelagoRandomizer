using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DDoor.ArchipelagoRandomizer;

internal class Preloader : IDisposable
{
	private readonly static Preloader instance = new();
	private readonly Dictionary<string, List<OnLoadedSceneFunc>> objectsToCache;
	private readonly List<Object> cachedObjects;
	private Transform cacheHolder;
	private Stopwatch stopwatch;
	private string savedScene;

	public event Action OnPreloadDone;

	public static Preloader Instance => instance;
	public static bool IsPreloading { get; private set; }

	private Preloader()
	{
		objectsToCache = [];
		cachedObjects = [];
	}

	public static T GetCachedObject<T>(string objName) where T : Object
	{
		// Find the cached object
		Object obj = Instance.cachedObjects.Find(x => x.name == objName);

		if (obj == null)
		{
			Logger.LogError($"No object with name {objName} was found in the cached list of objects. Returning null.");
			return null;
		}

		if (obj is T typedObj)
			return typedObj;

		Logger.LogError($"Found cached object with name {objName}, but it's not a UnityEngine.Object type. Returning null.");
		return null;
	}

	public static void CacheObject(GameObject obj)
	{
		GameObject newObj = Object.Instantiate(obj, Instance.cacheHolder);
		newObj.name = newObj.name.Replace("(Clone)", "");
		Instance.cachedObjects.Add(newObj);
	}

	public void AddObjectToCacheList(string scene, OnLoadedSceneFunc onLoadedSceneCallback)
	{
		// If the scene is already in the cache list, add the new callback to it
		if (objectsToCache.ContainsKey(scene))
		{
			if (objectsToCache[scene].Contains(onLoadedSceneCallback))
				return;

			objectsToCache[scene].Add(onLoadedSceneCallback);
		}
		// If the scene is not already in cache list, add the new scene
		else
		{
			objectsToCache.Add(scene, [onLoadedSceneCallback]);
		}
	}

	public void StartPreload(Action onDone = null)
	{
		IsPreloading = true;
		savedScene = GameSave.GetSaveData().GetSpawnScene();
		Logger.Log("Starting preload...");

		// Create the parent that will hold the cached objects
		cacheHolder = new GameObject("Cached Objects").transform;
		cacheHolder.gameObject.SetActive(false);

		// Make cache holder object persistent
		Object.DontDestroyOnLoad(cacheHolder);

		Plugin.StartRoutine(PreloadAll(onDone));
	}

	public void Dispose()
	{
		OnPreloadDone = null;
	}

	private IEnumerator PreloadAll(Action onDone)
	{
		int loopCount = 0;
		bool hasDoneFadeOut = false;

		foreach (KeyValuePair<string, List<OnLoadedSceneFunc>> kvp in objectsToCache)
		{
			loopCount++;
			string sceneToLoad = kvp.Key;
			List<OnLoadedSceneFunc> objectsToCache = kvp.Value;
			GameSceneManager.instance.loadOnFadeOut = false;

			if (!hasDoneFadeOut)
			{
				stopwatch = Stopwatch.StartNew();

				// Start fadeout
				ScreenFade.instance.SetColor(Color.white, false);
				ScreenFade.instance.FadeOut(1f, true, null);
				ScreenFade.instance.LockFade();

				yield return new WaitWhile(ScreenFade.instance.IsFadingOut);
				yield return SceneManager.UnloadSceneAsync("TitleScreen");
				hasDoneFadeOut = true;
			}

			// Wait for scene to load
			Logger.Log($"Preloading scene {sceneToLoad}...");
			yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
			SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));
			GameSceneManager.currentScene = sceneToLoad;

			CacheObjects(objectsToCache);

			if (loopCount < this.objectsToCache.Count)
			{
				yield return SceneManager.UnloadSceneAsync(sceneToLoad);
			}
		}

		PreloadFinished(onDone);
	}

	private void CacheObjects(List<OnLoadedSceneFunc> callbacks)
	{
		for (int i = 0; i < callbacks.Count; i++)
		{
			// Get the objects to cache from the callback
			Object[] objs = callbacks[i]?.Invoke();

			foreach (Object obj in objs)
			{
				GameObject gameObj = obj as GameObject;
				Object newObj;

				// If object is a GameObject, change its parent so it persists
				if (gameObj != null)
				{
					newObj = Object.Instantiate(gameObj, cacheHolder);
					newObj.name = newObj.name.Replace("(Clone)", "");
				}
				else
				{
					newObj = Object.Instantiate(obj);
					Object.DontDestroyOnLoad(newObj);
				}

				cachedObjects.Add(newObj);
			}
		}
	}

	private void PreloadFinished(Action onDone)
	{
		stopwatch.Stop();
		Logger.Log($"Finished preloading {cachedObjects.Count} object(s) across {objectsToCache.Count} scene(s) in {stopwatch.ElapsedMilliseconds}ms");
		IsPreloading = false;
		objectsToCache.Clear();
		onDone?.Invoke();
		OnPreloadDone?.Invoke();

		GameSceneManager.DontSaveNext();
		GameSceneManager.LoadScene(savedScene, false);
		GameSceneManager.ReloadSaveOnLoad();

		// Start playing the scene's audio
		GameSceneManager.instance.didLoadPlayer();
	}

	public delegate Object[] OnLoadedSceneFunc();

	[HarmonyPatch]
	private class Patches
	{
		// Patches the new game press event to not immediately load into the saved scene. This will be done when preloading is finished.
		[HarmonyPrefix]
		[HarmonyPatch(typeof(SaveSlot), nameof(SaveSlot.LoadSave))]
		private static bool NewGamePatch(SaveSlot __instance)
		{
			if (!Archipelago.Instance.IsConnected())
				return true;

			__instance.useSaveFile();
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameRoom), nameof(GameRoom.init))]
		private static bool FixGameRoomNullRef()
		{
			return !IsPreloading;
		}

		// When returning to title, it clears the cached objects list
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSceneManager), nameof(GameSceneManager.ReturnToTitle))]
		private static void ReturnToTitlePatch()
		{
			if (!Archipelago.Instance.IsConnected())
				return;

			Instance.cachedObjects.Clear();
			Instance.objectsToCache.Clear();
			Object.Destroy(Instance.cacheHolder.gameObject);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LevelSpawn), nameof(LevelSpawn.Awake))]
		private static void PreLevelSpawnAwake(LevelSpawn __instance)
		{
			if (IsPreloading)
			{
				__instance.spawnInjuredFalling = false;
			}
		}
	}
}