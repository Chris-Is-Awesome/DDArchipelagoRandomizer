using HarmonyLib;
using System;

namespace DDoor.ArchipelagoRandomizer;

/// <summary>
/// Works around problems in ItemChanger patches.
/// </summary>
internal static class ItemChangerFixes
{
    [HarmonyPatch]
    private class Patches
    {
        /// <summary>
        /// ItemChanger's patches on BaseLock.KeysAreUnlocked and KeysAreUnlockedImmediate (for the Forest Horn drop)
        /// read the lock's first key without checking it exists. A few locks have an empty key slot (in the Lord of
        /// Doors arena and the Stranded Sailor caves, for example), so they threw every frame there. The game itself
        /// skips empty key slots, so do the same.
        /// </summary>
        [HarmonyFinalizer, HarmonyPatch(typeof(BaseLock), nameof(BaseLock.KeysAreUnlockedImmediate))]
        private static Exception KeysAreUnlockedImmediateFinalizer(Exception __exception, BaseLock __instance, ref bool __result) =>
            Recover(__exception, __instance, ref __result, key => GameSave.GetSaveData().IsKeyUnlocked(key.uniqueId));

        [HarmonyFinalizer, HarmonyPatch(typeof(BaseLock), nameof(BaseLock.KeysAreUnlocked))]
        private static Exception KeysAreUnlockedFinalizer(Exception __exception, BaseLock __instance, ref bool __result) =>
            Recover(__exception, __instance, ref __result, key => key.active);

        private static Exception Recover(Exception exception, BaseLock lockObject, ref bool result, Func<BaseKey, bool> isUnlocked)
        {
            if (exception is not NullReferenceException || lockObject.key == null || Array.IndexOf(lockObject.key, null) < 0)
            {
                return exception;
            }

            bool unlocked = !lockObject.useOrSwitch;
            foreach (BaseKey key in lockObject.key)
            {
                if (key == null)
                {
                    continue;
                }
                if (lockObject.useOrSwitch && isUnlocked(key))
                {
                    unlocked = true;
                    break;
                }
                if (!lockObject.useOrSwitch && !isUnlocked(key))
                {
                    unlocked = false;
                    break;
                }
            }
            result = unlocked;
            return null;
        }
    }
}
