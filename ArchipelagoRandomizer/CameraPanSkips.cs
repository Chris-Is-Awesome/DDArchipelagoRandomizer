using HarmonyLib;
using UnityEngine.SceneManagement;

namespace DDoor.ArchipelagoRandomizer;

/// <summary>
/// With Skip Cutscenes on, skips the short camera pans that show what a switch, brazier, lever or cleared fight just
/// opened (the Castle Lockstone braziers, the Lost Cemetery ghosts, crow souls flying into an Ancient Door and so on).
/// These only move the camera: whatever they show has already happened, so nothing else needs to change.
/// </summary>
internal static class CameraPanSkips
{
    private static bool ShouldSkip()
    {
        // The credits reuse some of these pans as part of the credits sequence
        return Archipelago.Instance != null && Archipelago.Instance.IsConnected()
            && Archipelago.Instance.apConfig.SkipCutscenes && SceneManager.GetActiveScene().name != "_CREDITS";
    }

    [HarmonyPatch]
    private class Patches
    {
        /// <summary>
        /// Pans that fade out to another room, show something there and fade back
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CameraFocusTrigger), nameof(CameraFocusTrigger.doUnlock))]
        private static void SkipRoomPan(CameraFocusTrigger __instance)
        {
            if (!ShouldSkip())
            {
                return;
            }
            Logger.Log($"Skipping camera pan {__instance.name}");
            ScreenFade.instance.FadeIn(0.1f, true, null);
            PlayerGlobal.instance.UnPauseInput_Cutscene();
            __instance.timer = 0;
            __instance.phase = 0;
            if (__instance.finishedKey != null)
            {
                __instance.finishedKey.Unlock();
            }
            GameSave.SaveGameState();
        }

        /// <summary>
        /// Pans that move the camera to something in the same room (a raising platform) and back
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CameraFocusTriggerMove), nameof(CameraFocusTriggerMove.doUnlock))]
        private static void SkipMovePan(CameraFocusTriggerMove __instance)
        {
            if (!ShouldSkip() || __instance.timer <= 0)
            {
                return;
            }
            Logger.Log($"Skipping camera pan {__instance.name}");
            // What the game does when the lock is already open on load (forceOpen), without un-pausing input that
            // this pan never paused
            if (__instance.pauseInput)
            {
                PlayerGlobal.instance.UnPauseInput_Cutscene();
            }
            __instance.timer = 0;
            __instance.phase = -1;
        }

        /// <summary>
        /// Pans that pull the camera towards a door, lever or bridge for a few seconds after it opens
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CameraFocusObject_Unlock), nameof(CameraFocusObject_Unlock.doUnlock))]
        private static void SkipFocusPan(CameraFocusObject_Unlock __instance)
        {
            if (!ShouldSkip() || !__instance.unlocked)
            {
                return;
            }
            // What the game does when the lock is already open on load
            __instance.timer = 999;
            if (__instance.focus != null)
            {
                __instance.focus.strength = 0;
            }
        }
    }
}
