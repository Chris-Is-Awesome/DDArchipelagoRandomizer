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
                    if (!GameSave.GetSaveData().IsKeyUnlocked("cts_handler")) // Only queue GoS door to trigger if Chandler scene has not already been watched/skipped
                    {
                        ItemRandomizer.Instance.QueueTriggerGroveOfSpiritsDoorCheck();
                        SkipCutsceneSet(optionalCutscenes);
                    }
                }
                skippedCutscenes = true;
            }
        }
    }
}