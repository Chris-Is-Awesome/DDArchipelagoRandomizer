using HarmonyLib;

namespace DDoor.ArchipelagoRandomizer;

public static class CutsceneFlags
{
    private static GameSave GetSaveData() => GameSave.GetSaveData();

    // ItemChanger sets shop_prompted to wake Chandler up after the Chandler cutscene

    private static void SetLostCemeteryCutscenes()
    {
        GameSave gameSave = GetSaveData();
        gameSave.SetKeyState("crow_cut1", true, true); // Cutscene of Grey Crow on bridge in Lost Cemetery (invisible hitbox blocks bridge otherwise)
    }

    private static void SkipGrandmaCutscenes()
    {
        GameSave gameSave = GetSaveData();
        gameSave.SetKeyState("gm_act_0", true, true);
        gameSave.SetKeyState("gm_act_1", true, true);
        gameSave.SetKeyState("gm_act_2", true, true);
        gameSave.SetKeyState("gm_act_3", true, true);
        gameSave.SetKeyState("gran_pot_1", true, true);
        gameSave.SetKeyState("gran_pot_2", true, true);
        gameSave.SetKeyState("grandma_romp_intro_watched", true, true);
    }

    private static void SkipBardCutscenes()
    {
        GameSave gameSave = GetSaveData();
        gameSave.SetKeyState("bard_bar_intro", true, true);
        gameSave.SetKeyState("bard_cracked_block", true, true);
        gameSave.SetKeyState("bard_fort_intro", true, true);
        gameSave.SetKeyState("bard_fortress", true, true);
        gameSave.SetKeyState("bard_crows", true, true);
        gameSave.SetKeyState("bard_betty_cave", true, true);
        gameSave.SetKeyState("bard_pre_betty", true, true);
    }

    private static void SkipPotheadCutscenes()
    {
        GameSave gameSave = GetSaveData();
        gameSave.SetKeyState("pothead_intro_1", true, true);
        gameSave.SetKeyState("pothead_intro_2", true, true);
        gameSave.SetKeyState("pothead_intro_3", true, true);
        gameSave.SetKeyState("potkey_intro", true, true);
        gameSave.SetKeyState("pothead_confession1", true, true);
        gameSave.SetKeyState("pothead_m_4", true, true);
        gameSave.SetKeyState("phcs_1", true, true);
        gameSave.SetKeyState("phcs_1.5", true, true);
        gameSave.SetKeyState("phcs_5", true, true);
        gameSave.SetKeyState("phcs_break", true, true);
        gameSave.SetKeyState("phcs_2", true, true);
        gameSave.SetKeyState("phcs_3", true, true);
        gameSave.SetKeyState("ach_pothead", true, true);
    }

    private static void SkipFrogCutscenes()
    {
        GameSave gameSave = GetSaveData();
        gameSave.SetKeyState("frog_boss_wall_chat", true, true);
        gameSave.SetKeyState("frog_dung_meet_1", true, true);
        gameSave.SetKeyState("watched_frogwall", true, true);
        gameSave.SetKeyState("frog_boss_swim_chat", true, true);
        gameSave.SetKeyState("frog_dung_meet_3", true, true);
        gameSave.SetKeyState("watched_frogswim", true, true);
        gameSave.SetKeyState("frog_boss_sewer_chat", true, true);
        gameSave.SetKeyState("frog_dung_meet_2", true, true);
        gameSave.SetKeyState("watched_frogsewer", true, true);
        gameSave.SetKeyState("frog_wall_chat_last", true, true);
        gameSave.SetKeyState("frog_dung_meet_last", true, true);
        gameSave.SetKeyState("frog_ghoul_intro", true, true);
        gameSave.SetKeyState("c_swamp_intro", true, true);
    }

    private static void SkipChandler()
    {
        GameSave gameSave = GetSaveData();
        gameSave.SetKeyState("cts_bus", true, true);
        gameSave.SetKeyState("handler_intro", true, true);
        gameSave.SetKeyState("sdoor_tutorial_hub", true, true);
        gameSave.SetKeyState("sdoor_tutorial", true, true);
        gameSave.SetKeyState("handler_intro2", true, true);
        gameSave.SetKeyState("handler_intro3", true, true);
        gameSave.SetKeyState("cts_handler", true, true);

        // If skipping Chandler, need to set shop to be available
        gameSave.SetKeyState("shop_prompted", true, true);
        gameSave.SetKeyState("bosskill_forestmother", true);

        ItemRandomizer.Instance.QueueTriggerGroveOfSpiritsDoorCheck(); // Since we are skipping the cutscene, we need to grant the item given during the cutscene

        // gameSave.SetSpawnPoint("lvl_hallofdoors", "bus_override_spawn"); //Test if this is necessary
    }

    private static void SkipMiscCutscenes()
    {
        GameSave gameSave = GetSaveData();
        gameSave.SetKeyState("gd_intro_done", true, true);
        gameSave.SetKeyState("covenant_intro", true, true);
        gameSave.SetKeyState("c_met_lod", true, true);
    }

    [HarmonyPatch]
    private class Patches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(SaveSlot), "useSaveFile")]
        private static void SkipCutscenes()
        {
            if (Archipelago.Instance.IsConnected())
            {
                // Only apply skips if playing an AP file
                SetLostCemeteryCutscenes(); // This cutscene skip is essential for rando, make sure it is set, even on started files (for backwards compatibility with started saves)
                if (!GameSave.GetSaveData().IsKeyUnlocked("cts_bus")) //Only skip cutscenes if file has not been started
                {
                    if (Archipelago.Instance.apConfig.SkipCutscenes)
                    {
                        SkipChandler();
                        SkipMiscCutscenes();
                        SkipPotheadCutscenes();
                        SkipGrandmaCutscenes();
                        SkipFrogCutscenes();
                        SkipBardCutscenes();
                    }
                }
                GameSave.SaveGameState();
            }

        }
    }
}