using System.Linq;
﻿using DDoor.AddUIToOptionsMenu;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Utility;
using IC = DDoor.ItemChanger;

namespace DDoor.ArchipelagoRandomizer;

#nullable enable
internal class GoalModifications : MonoBehaviour
{
    private static GoalModifications? instance;
    public static GoalModifications? Instance => instance;
    private Goal? goal;
    private Goal GetGoal()
    {
        goal ??= Archipelago.Instance.GetSlotData<long>("goal") switch
        {
            0 => Goal.LordOfDoors,
            1 => Goal.TrueEnding,
            2 => Goal.GreenTablet,
            3 => Goal.HolyBird,
            4 => Goal.Lorekeeper,
            5 => Goal.Truthseeker,
            10 => Goal.Any,
            11 => Goal.All,
            12 => Goal.Custom,
            _ => Goal.LordOfDoors,
        };
        return (Goal)goal;
    }

    private System.Collections.Generic.HashSet<Ending>? endings;
    private int requiredCount;

    /// <summary>
    /// The endings that count for the goal and whether all of them are needed. Seeds from apworld 0.5.0 on send
    /// this directly (it's the only way to know a Custom goal); older seeds go by the Goal option.
    /// </summary>
    private System.Collections.Generic.HashSet<Ending> GoalEndings()
    {
        if (endings != null)
        {
            return endings;
        }
        endings = new();
        if (Archipelago.Instance.TryGetSlotData("goal_endings", out object value) && value is Newtonsoft.Json.Linq.JArray list)
        {
            foreach (string name in list.Values<string>())
            {
                if (EndingByName.TryGetValue(name, out Ending ending))
                {
                    endings.Add(ending);
                }
            }
            bool all = !Archipelago.Instance.TryGetSlotData("goal_requires_all", out object allValue) || System.Convert.ToBoolean(allValue);
            requiredCount = Archipelago.Instance.TryGetSlotData("goal_required_count", out object count)
                ? System.Convert.ToInt32(count)
                : all ? endings.Count : 1;
        }
        if (endings.Count == 0)
        {
            switch (GetGoal())
            {
                case Goal.TrueEnding: endings.Add(Ending.TrueEnding); break;
                case Goal.GreenTablet: endings.Add(Ending.GreenTablet); break;
                case Goal.Any:
                case Goal.All:
                    endings.Add(Ending.LordOfDoors);
                    endings.Add(Ending.TrueEnding);
                    endings.Add(Ending.GreenTablet);
                    break;
                default: endings.Add(Ending.LordOfDoors); break;
            }
            requiredCount = GetGoal() == Goal.Any ? 1 : endings.Count;
        }
        return endings;
    }

    /// <summary>More than one ending is needed, so each one is recorded in the save until all are done</summary>
    /// <summary>How many of the goal's endings are needed</summary>
    private int RequiredCount() => System.Math.Max(1, System.Math.Min(GoalEndings().Count, requiredCount));

    /// <summary>More than one ending is needed, so each one is recorded in the save until enough are done</summary>
    private bool NeedsSeveralEndings() => RequiredCount() > 1;

    internal bool IsLordOfDoorsGoal() => GoalEndings().Contains(Ending.LordOfDoors);
    internal bool IsTrueEndingGoal() => GoalEndings().Contains(Ending.TrueEnding);
    internal bool IsGreenTabletGoal() => GoalEndings().Contains(Ending.GreenTablet);

    internal enum Ending
    {
        LordOfDoors,
        TrueEnding,
        GreenTablet,
        HolyBird,
        Lorekeeper,
        Truthseeker,
    }

    private static readonly string[] EndingNames = ["defeat the Lord of Doors", "get the True Ending", "open the Green Tablet door",
        "visit all the shrines (Holy Bird)", "find all the Shiny Things (Lorekeeper)", "get all the Ancient Tablets (Truthseeker)"];

    /// <summary>The goal names the apworld uses in slot data</summary>
    private static readonly System.Collections.Generic.Dictionary<string, Ending> EndingByName = new()
    {
        { "Lord of Doors", Ending.LordOfDoors },
        { "True Ending", Ending.TrueEnding },
        { "Green Tablet", Ending.GreenTablet },
        { "Holy Bird", Ending.HolyBird },
        { "Lorekeeper", Ending.Lorekeeper },
        { "Truthseeker", Ending.Truthseeker },
    };

    private static string ReachedKey(Ending ending) => $"AP_Reached-{ending}";

    // ---------------------------------------------------------------- collection goals
    /// <summary>Holy Bird / Lorekeeper / Truthseeker: the locations to check, and endings standing in for locations that became goal events</summary>
    private sealed class CollectionGoal
    {
        public Ending Ending;
        public string Noun = "";
        public long[] Locations = [];
        public Ending[] Endings = [];
        public int LastCount = -1;
    }

    private System.Collections.Generic.List<CollectionGoal>? collectionGoals;
    private float nextCollectionCheck;

    private System.Collections.Generic.List<CollectionGoal> CollectionGoals()
    {
        if (collectionGoals != null)
        {
            return collectionGoals;
        }
        collectionGoals = new();
        if (!Archipelago.Instance.TryGetSlotData("collection_goals", out object value) || value is not Newtonsoft.Json.Linq.JObject goals)
        {
            return collectionGoals;
        }
        foreach (Newtonsoft.Json.Linq.JProperty goal in goals.Properties())
        {
            if (!EndingByName.TryGetValue(goal.Name, out Ending ending))
            {
                continue;
            }
            CollectionGoal data = new()
            {
                Ending = ending,
                Noun = ending switch { Ending.HolyBird => "shrines", Ending.Lorekeeper => "Shiny Things", _ => "tablet locations" },
                Locations = goal.Value["locations"]?.ToObject<long[]>() ?? [],
            };
            System.Collections.Generic.List<Ending> stand = new();
            foreach (string name in goal.Value["endings"]?.ToObject<string[]>() ?? [])
            {
                if (EndingByName.TryGetValue(name, out Ending e)) stand.Add(e);
            }
            data.Endings = stand.ToArray();
            collectionGoals.Add(data);
        }
        return collectionGoals;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCollectionCheck || !Archipelago.Instance.IsConnected())
        {
            return;
        }
        nextCollectionCheck = Time.unscaledTime + 1f;
        GameSave save = GameSave.GetSaveData();
        if (save == null || PlayerGlobal.instance == null)
        {
            return;
        }
        System.Collections.Generic.List<CollectionGoal> goals = CollectionGoals();
        if (goals.Count == 0)
        {
            return;
        }
        System.Collections.Generic.HashSet<long> done = new(Archipelago.Instance.Session.Locations.AllLocationsChecked);
        foreach (CollectionGoal goal in goals)
        {
            if (save.IsKeyUnlocked(ReachedKey(goal.Ending)))
            {
                continue;
            }
            int count = goal.Locations.Count(done.Contains) + goal.Endings.Count(e => save.IsKeyUnlocked(ReachedKey(e)));
            int total = goal.Locations.Length + goal.Endings.Length;
            if (count >= total)
            {
                ReachEnding(goal.Ending);
            }
            else if (goal.LastCount >= 0 && count > goal.LastCount)
            {
                UIManager.Instance.ShowNotification($"{EndingByName.First(kvp => kvp.Value == goal.Ending).Key}: {count}/{total} {goal.Noun}");
            }
            goal.LastCount = count;
        }
    }

    /// <summary>
    /// One of the endings was reached. With the All goal the endings are recorded in the save and the goal is sent
    /// once all three are done; otherwise any reached ending that counts completes the goal.
    /// </summary>
    internal static void ReachEnding(Ending ending)
    {
        if (!Archipelago.Instance.IsConnected() || Instance == null)
        {
            return;
        }
        // Remembered whether or not it counts: collection goals use it for locations that became goal events
        GameSave reached = GameSave.GetSaveData();
        if (reached != null && !reached.IsKeyUnlocked(ReachedKey(ending)))
        {
            reached.SetKeyState(ReachedKey(ending), true, true);
        }
        if (!Instance.GoalEndings().Contains(ending))
        {
            return;
        }
        if (!Instance.NeedsSeveralEndings())
        {
            Archipelago.Instance.SendCompletion();
            return;
        }
        GameSave save = GameSave.GetSaveData();
        save.SetKeyState($"AP_Ending-{ending}", true, true);
        if (ending == Ending.LordOfDoors)
        {
            RestoreHeldTruthKeys(save);
        }
        save.Save();
        CheckAllEndings(true);
    }

    /// <summary>
    /// All goal: sends completion if all three endings are recorded (also on load, for endings reached while
    /// disconnected) and otherwise says which are left.
    /// </summary>
    internal static void CheckAllEndings(bool announce)
    {
        if (!Archipelago.Instance.IsConnected() || Instance == null || !Instance.NeedsSeveralEndings())
        {
            return;
        }
        GameSave save = GameSave.GetSaveData();
        if (save == null)
        {
            return;
        }
        System.Collections.Generic.List<string> left = new();
        int done = 0;
        foreach (Ending ending in Instance.GoalEndings())
        {
            if (save.IsKeyUnlocked($"AP_Ending-{ending}"))
            {
                done++;
            }
            else
            {
                left.Add(EndingNames[(int)ending]);
            }
        }
        int needed = Instance.RequiredCount();
        if (done >= needed)
        {
            Archipelago.Instance.SendCompletion();
        }
        else if (announce)
        {
            string still = needed == Instance.GoalEndings().Count
                ? $"Still to do: {string.Join(", ", left)}."
                : $"{needed - done} more of: {string.Join(", ", left)}.";
            UIManager.Instance.ShowNotification($"Goal: {done}/{needed} done. {still}");
        }
    }

    // The Lost Cemetery's crow cutscenes go by the highest story key you have: ... the Death's Door opening, ...
    // c_loddead, credits_done, truth_expo (set by the True Ending's TruthExpo scene), truth_done. In the normal game
    // the True Ending comes after the Lord of Doors, so with truth_expo set the door to the Grey Crow never shows.
    // With the All goal the True Ending can come first, so these keys are held back until the Lord of Doors is beaten.
    private static readonly string[] TruthKeys = ["truth_expo", "truth_done"];
    private static string HeldKey(string key) => $"AP_Held-{key}";

    /// <summary>All goal, Lord of Doors not beaten yet: take the True Ending's story keys away for now</summary>
    internal static void HoldTruthKeys()
    {
        // Only matters when the Lord of Doors still has to be beaten after the True Ending
        if (!Archipelago.Instance.IsConnected() || Instance == null || !Instance.NeedsSeveralEndings()
            || !Instance.IsLordOfDoorsGoal() || !Instance.IsTrueEndingGoal())
        {
            return;
        }
        GameSave save = GameSave.GetSaveData();
        if (save == null || save.IsKeyUnlocked($"AP_Ending-{Ending.LordOfDoors}") || save.IsKeyUnlocked("c_loddead"))
        {
            return;
        }
        foreach (string key in TruthKeys)
        {
            if (save.IsKeyUnlocked(key))
            {
                save.SetKeyState(key, false, false);
                save.SetKeyState(HeldKey(key), true, false);
                Logger.Log($"All goal: holding back {key} until the Lord of Doors is beaten, so the Grey Crow's door can open");
            }
        }
    }

    private static void RestoreHeldTruthKeys(GameSave save)
    {
        foreach (string key in TruthKeys)
        {
            if (save.IsKeyUnlocked(HeldKey(key)))
            {
                save.SetKeyState(HeldKey(key), false, false);
                save.SetKeyState(key, true, false);
            }
        }
    }

    private void Awake()
    {
        instance = this;
        SceneManager.sceneLoaded += ModifyGoalScenes;
    }

    internal void ModifyGoalScenes(Scene scene, LoadSceneMode _)
    {
        // Before the scene's objects run Start, which is when the Lost Cemetery picks its crow cutscene
        HoldTruthKeys();

        string truthRuinsScene = "lvlConnect_Fortress_Mountaintops";
        if (scene.name == truthRuinsScene)
        {
            if (!IsTrueEndingGoal())
            {
                // Game appears to already disable if don't have all 7 tablets
                PathUtil.GetByPath(truthRuinsScene, "SceneMover/R_TruthRuins/_CONTENTS/TRUTH/_Trigger").SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= ModifyGoalScenes;
    }

    [HarmonyPatch]
    private class Patches
    {
        /// <summary>
        /// Sends completion for main ending if that is the goal setting
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(LodBoss2), nameof(LodBoss2.NextPhase))]
        private static void EndGameCsPatch(LodBoss2 __instance)
        {
            if (Archipelago.Instance.IsConnected())
            {
                if (__instance.deathCutscene && Instance != null && Instance.IsLordOfDoorsGoal())
                {
                    ReachEnding(Ending.LordOfDoors);
                }
            }
        }

        /// <summary>
        /// Sends completion for the true ending if that is the goal setting
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ActivateTrigger), nameof(ActivateTrigger.DoActivateTrigger))]
        private static void TrueEndingCsPatch(ActivateTrigger __instance)
        {
            if (Archipelago.Instance.IsConnected())
            {
                if (Instance != null && Instance.IsTrueEndingGoal())
                {
                    if (__instance.target.name == "TRUTH")
                    {
                        ReachEnding(Ending.TrueEnding);
                    }
                }
            }
        }
    }

    private enum Goal
    {
        LordOfDoors,
        TrueEnding,
        GreenTablet,
        HolyBird,
        Lorekeeper,
        Truthseeker,
        Any,
        All,
        Custom,
    }

    internal readonly struct GoalItem : IC.Item
	{
		public string DisplayName { get; }
		public string Icon { get; }
		public string Location { get; }
		public Ending Ending { get; }

		public void Trigger()
		{
			ReachEnding(Ending);
		}

		public GoalItem(string displayName, string icon, string location, Ending ending)
		{
			DisplayName = displayName;
			Icon = icon;
			Location = location;
			Ending = ending;
		}
	}
}
#nullable disable