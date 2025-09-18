﻿using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DDoor.ArchipelagoRandomizer;

internal class Preloader
{
	private readonly static Preloader instance = new();
	private readonly Dictionary<string, List<OnLoadedSceneFunc>> objectsToCache;
	private readonly List<Object> cachedObjects;
	private Transform cacheHolder;
	private Stopwatch stopwatch;

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
		Object obj = Instance.cachedObjects.Find(x => x.name == objName)
			?? throw new Exception($"No object with name {objName} was found in the cached list of objects.");

		return (T)obj;
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
		Logger.Log("Starting preload...");

		// Create the parent that will hold the cached objects
		cacheHolder = new GameObject("Cached Objects").transform;
		cacheHolder.gameObject.SetActive(false);

		// Make cache holder object persistent
		Object.DontDestroyOnLoad(cacheHolder);

		Plugin.StartRoutine(PreloadAll(onDone));
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
			yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
			Logger.Log($"Preloading scene {sceneToLoad}...");
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

				// If object is a GameObject, change its parent so it persists
				if (gameObj != null)
					gameObj.transform.SetParent(cacheHolder, true);
				else
				{
					Object.DontDestroyOnLoad(obj);
				}

				cachedObjects.Add(obj);
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

		GameSceneManager.DontSaveNext();
		GameSceneManager.LoadScene(GameSave.GetSaveData().GetSpawnScene(), false);
		GameSceneManager.ReloadSaveOnLoad();
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
			__instance.useSaveFile();
			return false;
		}

		// When returning to title, it clears the cached objects list
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSceneManager), nameof(GameSceneManager.ReturnToTitle))]
		private static void ReturnToTitlePatch()
		{
			Instance.cachedObjects.Clear();
			Instance.objectsToCache.Clear();
			Object.Destroy(Instance.cacheHolder.gameObject);
		}
	}
}