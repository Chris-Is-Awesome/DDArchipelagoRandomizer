using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using IC = DDoor.ItemChanger;

namespace DDoor.ArchipelagoRandomizer;

#nullable enable
internal class EntranceRandomizer : MonoBehaviour
{
    private static EntranceRandomizer? instance;
    public static EntranceRandomizer? Instance => instance;
    private bool loadingIn = true;

    private Dictionary<string, string>? entrancePairings = null;

    internal Dictionary<string, string>? GetEntrancePairings()
    {
        if (entrancePairings == null)
        {
            JObject eP = (JObject)Archipelago.Instance.GetSlotData<object>("entrance_pairings");
            entrancePairings = eP.ToObject<Dictionary<string, string>>();
        }
        return entrancePairings;
    }

    private readonly bool entranceRandomization = Archipelago.Instance.GetSlotData<long>("entrance_randomization") > 0; // Any value greater than 0 indicates entrance randomization

    private void Awake()
    {
        instance = this;
        SceneManager.sceneLoaded += RestorePlayerControl;
    }

    private void Destroy()
    {
        SceneManager.sceneLoaded -= RestorePlayerControl;
    }

    private void RestorePlayerControl(Scene scene, LoadSceneMode _)
    {
        if (SceneTransitions.IsSceneInRandomizedTransitions(scene.name))
        {
            PlayerGlobal.instance?.UnPauseInput_Cutscene();
            GameSceneManager.instance.reloadPlayerScene = true;
        }
    }

    private void AddFoundEntrance(string? loadingZoneId, string? sceneToLoad)
    {
        if (loadingZoneId != null && sceneToLoad != null)
        {
            SceneTransitions.SceneTransition? sceneTransition = SceneTransitions.GetOriginalSceneTransition(loadingZoneId, sceneToLoad);
            if (sceneTransition != null)
            {
                string? entranceName = sceneTransition?.startingRegion;
                if (entranceName != null)
                {
                    Archipelago.Instance.StoreFoundEntrance(entranceName);
                }
            }
        }
    }

    [HarmonyPatch]
    private class Patches
    {
        [HarmonyPatch(typeof(ShortcutDoor), nameof(ShortcutDoor.Awake))]
        [HarmonyPostfix]
        private static void PostShortcutDoorAwake(ShortcutDoor __instance)
        {
            if (Archipelago.Instance.IsConnected() && Instance != null && Instance.entranceRandomization)
            {
                DoorTrigger doorTrigger = __instance.doorTrigger;
                SceneTransitions.SceneTransition? newSceneTransition = SceneTransitions.GetConnectedSceneTransition(doorTrigger.doorId, doorTrigger.sceneToLoad);
                doorTrigger.sceneToLoad = newSceneTransition?.toSceneName;
                doorTrigger.targetDoor = newSceneTransition?.loadingZoneId;
            }
            if (!IC.ItemChangerPlugin.TryGetPlacedItem(typeof(IC.DoorLocation), __instance.keyId, out IC.Item? item))
            {
                // If the door isn't randomized, don't disable it
                return;
            }
            string doorName = IC.Predefined.predefinedLocations.First(kvp => kvp.Value.GetType() == typeof(IC.DoorLocation) && ((IC.DoorLocation)kvp.Value).UniqueId == __instance.keyId).Key;
            if (Archipelago.Instance.IsConnected() && !Archipelago.Instance.Session.Items.AllItemsReceived.Any(apitem => apitem.ItemName == doorName))
            {
                __instance.unlocked = false;
                if (__instance.hubDoor || __instance.keyId == "sdoor_tutorial")
                {
                    __instance.gameObject.SetActive(false);
                    IC.BoolItem.closedHubDoors[__instance.keyId] = __instance.gameObject;
                }

            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShortcutDoor), nameof(ShortcutDoor.Trigger))]
        [HarmonyBefore("deathsdoor.itemchanger")]
        private static bool PreShortcutDoorTrigger(ShortcutDoor __instance)
        {
            //Borrowed directly from ItemChanger EXCEPT we apply checks for if we are entering Hall of Doors

            if (!IC.ItemChangerPlugin.TryGetPlacedItem(typeof(IC.DoorLocation), __instance.keyId, out IC.Item? item))
            {
                return true;
            }

            // Original ItemChanger comment:
            // The check can be collected from either side of the door; if this
            // is not desirable - for instance if some form of transition rando
            // has been applied - then you should also check isHub here.

            string collectedKey = IC.DoorLocation.keyPrefix + __instance.keyId;
            GameSave save = GameSave.GetSaveData();

            // MODIFICATION TO ITEMCHANGER METHOD IN THIS CHECK
            // Instead of only checking if the Door has not been collected,
            // We want to check if it hasn't been collected AND one of three options
            // 1. The door is unlocked already (for collecting in HoD when you already have the door)
            // 2. We are not in Hall of Doors (for collecting in other levels)
            // 3. It is the Grove of Spirits Door (for Chandler cutscene triggering GoS door)
            if (!save.IsKeyUnlocked(collectedKey) && (__instance.unlocked || !SceneManager.GetSceneByName("lvl_HallOfDoors").isLoaded || __instance.keyId == IC.DoorLocation.groveDoorKey))
            {
                save.SetKeyState(collectedKey, true);
                IC.CornerPopup.Show(item);
                item.Trigger();

                // Darwin normally only wakes up after defeating DFS.
                // If the Grove of Spirits door is replaced by something
                // else, this is problematic as it prevents the player from
                // upgrading stats until potentially much later in the game.
                //
                // It would be preferrable to directly patch whatever is
                // making the decision to wake Darwin up, but it has been
                // very difficult to locate the exact object that does it.
                //
                // We set these keys after the Grove of Spirits door check
                // because setting the DFS one earlier also disables the
                // Chandler cutscene, which triggers the check in the first
                // place.
                if (__instance.keyId == IC.DoorLocation.groveDoorKey)
                {
                    save.SetKeyState("shop_prompted", true);
                    save.SetKeyState(IC.DoorLocation.dfsKey, true);
                }
            }

            if (!__instance.unlocked)
            {
                PlayerGlobal.instance.UnPauseInput();
                PlayerGlobal.instance.UnPauseInput_Cutscene();
            }

            // If the door is already open, allow you to go through it anyway.
            return __instance.unlocked;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DoorTrigger), nameof(DoorTrigger.OnTriggerEnter))]
        private static bool PreOnTriggerEnter(DoorTrigger __instance, Collider collider)
        {
            if (__instance.parentDoor == null) // All Shortcut Doors will have been modified on Awake
            {
                if (!__instance.triggered && Archipelago.Instance.IsConnected() && Instance != null && Instance.entranceRandomization)
                {
                    SceneTransitions.SceneTransition? newSceneTransition = SceneTransitions.GetConnectedSceneTransition(__instance.doorId, __instance.sceneToLoad);
                    PlayerInputControl playerInputControl = collider.gameObject.GetComponent<PlayerInputControl>();
                    if (newSceneTransition != null && !__instance.triggered && playerInputControl != null)
                    {
                        __instance.sceneToLoad = newSceneTransition?.toSceneName;
                        __instance.targetDoor = newSceneTransition?.loadingZoneId;
                        DoorTrigger.currentTargetDoor = newSceneTransition?.loadingZoneId;
                        GameSave.GetSaveData().SetSpawnPoint(__instance.sceneToLoad, __instance.targetDoor);
                        Instance.AddFoundEntrance(__instance.targetDoor, __instance.sceneToLoad);
                    }
                }
                return true;
            }
            else
            {
                GameSave.GetSaveData().SetSpawnPoint(__instance.sceneToLoad, __instance.targetDoor);
                Instance?.AddFoundEntrance(__instance.targetDoor, __instance.sceneToLoad);
                // if have a ShortcutDoor, already have scene transition set
                if (!__instance.triggered && SceneManager.GetSceneByName(__instance.sceneToLoad).isLoaded)
                {
                    DoorTrigger.currentTargetDoor = __instance.targetDoor;
                    foreach (GameObject rootObject in SceneManager.GetSceneByName(__instance.sceneToLoad).GetRootGameObjects())
                    {
                        foreach (DoorTrigger doorTrigger in rootObject.GetComponentsInChildren<DoorTrigger>(true))
                        {
                            if (doorTrigger.doorId == __instance.targetDoor)
                            {
                                __instance.triggered = true;
                                collider.gameObject.GetComponent<PlayerInputControl>()?.PauseInput(true);
                                __instance.linkedLocalDoor = doorTrigger;
                                doorTrigger.teleportThroughThisDoor();
                                PlayerGlobal.instance.UnPauseInput_Cutscene();
                                GameSceneManager.instance.reloadPlayerScene = true;
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DoorTrigger), nameof(DoorTrigger.OnTriggerExit))]
        private static void PostOnTriggerExit(DoorTrigger __instance)
        {
            GameSceneManager.instance.reloadPlayerScene = true;
            PlayerGlobal.instance.UnPauseInput();
            PlayerGlobal.instance.UnPauseInput_Cutscene();
            if (__instance.parentDoor != null && !GameSave.GetSaveData().IsKeyUnlocked(__instance.parentDoor.keyId))
            {
                __instance.parentDoor.unlocked = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DoorTrigger), nameof(DoorTrigger.Awake))]
        private static bool PreDoorTriggerAwake(DoorTrigger __instance)
        {
            // Prevent breaking when loading in when Door hasn't been made yet
            if (Archipelago.Instance.IsConnected() && Instance != null && __instance.doorId != "" && !GameSave.GetSaveData().IsKeyUnlocked(DoorTrigger.currentTargetDoor) && Instance.entranceRandomization)
            {
                if (__instance.doorId == DoorTrigger.currentTargetDoor)
                {
                    if (PlayerGlobal.instance != null)
                    {
                        if (__instance.spawnPoint == null)
                        {
                            __instance.spawnPoint = __instance.gameObject.transform.GetChild(0);
                        }
                        PlayerGlobal.instance.SetPosition(__instance.spawnPoint.position, false, false);
                        PlayerGlobal.instance.SetSafePos(__instance.spawnPoint.position);
                        PlayerGlobal.instance.SetRotation(__instance.spawnPoint.rotation);
                        DoorTrigger.currentTargetDoor = "";
                        GameSceneManager.instance.reloadPlayerScene = true;
                        PlayerGlobal.instance.UnPauseInput();
                        PlayerGlobal.instance.UnPauseInput_Cutscene();
                        __instance.parentDoor = __instance.gameObject.GetComponentInParent<ShortcutDoor>();
                        Instance.loadingIn = false;
                        return false;
                    }
                    else
                    {
                        __instance.retryInStart = true;
                    }
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DoorTrigger), nameof(DoorTrigger.Start))]
        private static bool PreDoorTriggerStart(DoorTrigger __instance)
        {
            // Prevent breaking when loading into Hall of Doors/from a save
            if (Archipelago.Instance.IsConnected() && Instance != null && (__instance.retryInStart || Instance.loadingIn))
            {
                GameSceneManager.instance.reloadPlayerScene = true;
                if (PlayerGlobal.instance != null && Instance.entranceRandomization)
                {
                    if (__instance.spawnPoint == null)
                    {
                        __instance.spawnPoint = __instance.gameObject.transform.GetChild(0);
                    }
                    PlayerGlobal.instance.SetPosition(__instance.spawnPoint.position, false, false);
                    PlayerGlobal.instance.SetSafePos(__instance.spawnPoint.position);
                    PlayerGlobal.instance.SetRotation(__instance.spawnPoint.rotation);
                    DoorTrigger.currentTargetDoor = "";

                    PlayerGlobal.instance.UnPauseInput();
                    PlayerGlobal.instance.UnPauseInput_Cutscene();
                    __instance.parentDoor = __instance.gameObject.GetComponentInParent<ShortcutDoor>();
                    Instance.loadingIn = false;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ForestBuggy), nameof(ForestBuggy.loadNextScene))]
        private static void PreForestBuggyloadNextScene(ForestBuggy __instance)
        {
            if (Archipelago.Instance.IsConnected() && Instance != null && Instance.entranceRandomization)
            {
                SceneTransitions.SceneTransition? sceneTransition = SceneTransitions.GetConnectedSceneTransition(__instance.doorId, __instance.targetScene);
                if (sceneTransition != null)
                {
                    __instance.doorId = sceneTransition?.loadingZoneId;
                    __instance.targetScene = sceneTransition?.toSceneName;
                    Instance.AddFoundEntrance(__instance.doorId, __instance.targetScene);
                }
            }
        }
    }
}
#nullable disable
