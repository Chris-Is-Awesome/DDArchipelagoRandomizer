using DDoor.AddUIToOptionsMenu;
using HarmonyLib;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DDoor.ArchipelagoRandomizer;

public static class CutsceneFlags
{
    private static GameSave GetGameSave() => GameSave.GetSaveData();
    private static readonly string hallOfDoorsScene = "lvl_HallOfDoors";
    internal static bool skippedCutscenes = false;

    // These cutscenes must be skipping to prevent invisible collision obstacles
    private static readonly string[] blockingCutscenes = ["crow_cut1", "gd_intro_done", "phcs_5", "bard_fort_intro"];
    private static readonly string[] optionalCutscenes =
        ["cts_bus", "handler_intro", "sdoor_tutorial_hub", "sdoor_tutorial", "handler_intro2", "handler_intro3", "cts_handler", "bosskill_forestmother",
         "covenant_intro", "c_met_lod", "lod_meet1", "lod_meet2", "lod_meet3", "lod_meet4",
        "gm_act_0", "gm_act_1", "gm_act_2", "gm_act_3", "gran_pot_1", "gran_pot_2", "grandma_romp_intro_watched", "gd_gran_eulogy_done",
        "bard_bar_intro", "bard_cracked_block", "bard_fortress", "bard_crows", "bard_betty_cave", "bard_pre_betty",
        "pothead_intro_1", "pothead_intro_2","pothead_intro_3","potkey_intro","pothead_confession1","pothead_m_4","phcs_1","phcs_1.5","phcs_break","phcs_2","phcs_3","ach_pothead",
        "frog_boss_wall_chat", "frog_dung_meet_1", "watched_frogwall", "frog_boss_swim_chat", "frog_dung_meet_3", "watched_frogswim", "frog_boss_sewer_chat", "frog_dung_meet_2", "watched_frogsewer", "frog_wall_chat_last", "frog_dung_meet_last", "frog_ghoul_intro", "c_swamp_intro"];


    // Late-game dialogue that is only watched in one place, so it is safe to mark as watched as soon as a save is
    // loaded: the Grey Crow and Death in the void behind Death's Door (the game itself skips these when you retry
    // the Grey Crow fight), the Lord of Doors' monologue before and during the final fight, and the Chandler's
    // chat after meeting the Lord of Doors. Unlike the list above these are also skipped on saves already under way.
    private static readonly string[] lateGameCutscenes =
        ["cts_death1", "watched_death_expo", "crowboss_intro_watched",
         "finallod_intro", "lod_demon1", "lod_demon2", "lod_demon3", "lod_demon4", "lod_demon5",
         "handler_lod_meet"];

    // Late-game cutscenes whose key also moves the story on, so they are only marked as watched once the game
    // would play them (see SkipLateGameStoryCutscenes)
    private static readonly string graveyardScene = "lvl_Graveyard";
    private static readonly string silentServantsScene = "lvl_SilentServants";
    private static readonly string crowCutscenesPath = "MainRoom/_CONTENTS/CrowCutscenesNEW";

    private static void SkipCutsceneSet(string[] cutsceneList)
    {
        foreach (string cutsceneKey in cutsceneList)
        {
            SetCutsceneToState(cutsceneKey, true);
        }
    }

    private static void SetCutsceneToState(string cutsceneKey, bool state)
    {
        GetGameSave().SetKeyState(cutsceneKey, state, true);
    }

    internal static void RemoveOfficeBlocker(Scene scene, LoadSceneMode _)
    {
        if (scene.name == hallOfDoorsScene)
        {
            GameObject officeBlocker = PathUtil.GetByPath(scene.name, "ENDGAME_STATE_CONTROL/_BASE_GAME_STATE/PROGRESSION_CONTROL/Default/BLOCKER_Office");
            officeBlocker.SetActive(false);
        }
    }

    internal static void ActivateShopKeep(Scene scene, LoadSceneMode _)
    {
        if (scene.name == hallOfDoorsScene)
        {
            GetGameSave().SetKeyState("shop_prompted", true, true);
            GameObject sleepingShopkeep = PathUtil.GetByPath(hallOfDoorsScene, "ENDGAME_STATE_CONTROL/_BASE_GAME_STATE/BANK_PROG_CONTROL/PreTutorial");
            sleepingShopkeep.SetActive(false);
            GameObject awakeShopkeep = PathUtil.GetByPath(hallOfDoorsScene, "ENDGAME_STATE_CONTROL/_BASE_GAME_STATE/BANK_PROG_CONTROL/ActiveBanker");
            awakeShopkeep.SetActive(true);
        }
    }

    // Marks a key as unlocked in the save and in the scene that has just loaded: most key objects only read the
    // save when they wake up, which has already happened by the time sceneLoaded runs
    private static void SetKeyInLoadedScene(Scene scene, string cutsceneKey)
    {
        SetCutsceneToState(cutsceneKey, true);
        foreach (BaseKey key in Resources.FindObjectsOfTypeAll<BaseKey>())
        {
            if (key.uniqueId == cutsceneKey && key.gameObject.scene == scene)
            {
                key.active = true;
            }
        }
    }

    // With Skip Cutscenes on, skip the story cutscenes of the late game once they are due, so the game goes
    // straight to the state it would be in after watching them
    internal static void SkipLateGameStoryCutscenes(Scene scene, LoadSceneMode _)
    {
        try
        {
            GameSave save = GetGameSave();
            if (scene.name != hallOfDoorsScene && save.IsKeyUnlocked("c_oldcrowdead") && !save.IsKeyUnlocked("deathfreed_cutscene_watched"))
            {
                // The crows' cutscene in the Hall of Doors after the Grey Crow fight. Marked as watched before the
                // Hall of Doors loads, so the hall wakes up in its after-cutscene state. That cutscene is also what
                // opens the door to the Lord of Doors' throne room, so open it the way the game's own fallback does.
                Logger.Log("Skipping the crows' cutscene in the Hall of Doors after the Grey Crow fight");
                SetCutsceneToState("deathfreed_cutscene_watched", true);
                SetCutsceneToState("sdoor_LoDfinal", true);
            }
            if (scene.name == graveyardScene)
            {
                // Death's Door opening with all three Giant Souls (the Grey Crow opens it): the door is then simply open
                CombinedKey allSouls = PathUtil.GetByPath(scene.name, $"{crowCutscenesPath}/KEY:allsouls").GetComponent<CombinedKey>();
                if (!save.IsKeyUnlocked("door_open_cutscene") && allSouls != null && allSouls.checkUnlocked())
                {
                    Logger.Log("Skipping the Death's Door opening cutscene");
                    SetKeyInLoadedScene(scene, "door_open_cutscene");
                }
                // Death's talk at the door after the Grey Crow fight
                if (save.IsKeyUnlocked("c_oldcrowdead") && !save.IsKeyUnlocked("death_post_dd_cutscene"))
                {
                    Logger.Log("Skipping Death's cutscene after the Grey Crow fight");
                    SetKeyInLoadedScene(scene, "death_post_dd_cutscene");
                }
            }
            else if (scene.name == silentServantsScene)
            {
                // The Lord of Doors' speech before the gauntlet in his throne room
                if (save.IsKeyUnlocked("c_oldcrowdead") && !save.IsKeyUnlocked("lod_gauntlet_intro_done"))
                {
                    Logger.Log("Skipping the Lord of Doors gauntlet intro");
                    SetKeyInLoadedScene(scene, "lod_gauntlet_intro_done");
                }
            }
        }
        catch (System.Exception e)
        {
            Logger.LogError($"Could not skip late-game cutscenes in {scene.name}: {e}");
        }
    }

    [HarmonyPatch]
    private class Patches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(SaveSlot), nameof(SaveSlot.useSaveFile))]
        private static void SkipCutscenes()
        {
            if (Archipelago.Instance.IsConnected())
            {
                // Only apply skips if playing an AP file
                if (skippedCutscenes)
                {
                    return;
                }
                SkipCutsceneSet(blockingCutscenes);
                SceneManager.sceneLoaded += RemoveOfficeBlocker;
                SceneManager.sceneLoaded += ActivateShopKeep;
                if (Archipelago.Instance.apConfig.SkipCutscenes)
                {
                    SkipCutsceneSet(lateGameCutscenes);
                    SceneManager.sceneLoaded += SkipLateGameStoryCutscenes;
                }
                if (!GameSave.GetSaveData().IsKeyUnlocked("cts_handler")) // Chandler scene not watched/skipped yet
                {
                    // Send the Grove of Spirits Door check on arriving in the Hall of Doors whether or not cutscenes
                    // are skipped, instead of waiting for the Chandler cutscene to play
                    ItemRandomizer.Instance.QueueTriggerGroveOfSpiritsDoorCheck();
                    if (Archipelago.Instance.apConfig.SkipCutscenes)
                    {
                        SkipCutsceneSet(optionalCutscenes);
                    }
                }
                skippedCutscenes = true;
            }
        }
    }
}