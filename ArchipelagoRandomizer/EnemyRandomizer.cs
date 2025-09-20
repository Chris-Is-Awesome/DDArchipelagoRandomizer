using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DDoor.ArchipelagoRandomizer;

internal class EnemyRandomizer : MonoBehaviour
{
	public enum Enemy
	{
		BatBlack,
		BatWhite,
		Brute,
		BruteGold,
		DekuScrub,
		Dodger,
		Fireplant,
		FireplantNoHookshot,
		Ghoul,
		GhoulLongRange,
		GhoulRapid,
		GrimaceKnight,
		Grunt,
		GruntFrog,
		GruntGrave,
		GruntPot,
		GruntRedeemer,
		GruntRedeemerIndoor,
		GruntYeti,
		Headroller,
		HeadrollerHeadless,
		Jumper,
		Knight,
		KnightGreenPlague,
		Lurker,
		LurkerMother,
		Mage,
		MageFire,
		MageGrounded,
		MagePlague,
		MagePlagueSingleshot,
		MageRedPurple,
		PlagueKnight,
		PlagueKnightSlowFire,
		Plant,
		ServantAll,
		ServantBomb,
		ServantFire,
		ServantHookshot,
		SlimeBig,
		SlimeBigGreen,
		SlimeMed,
		SlimeMedGreen,
		SlimeSmall,
		SlimeSmallGreen,
	}

	private static EnemyRandomizer instance;
	private Dictionary<string, EnemyData[]> enemiesToCache;

	public static EnemyRandomizer Instance => instance;

	private void Awake()
	{
		instance = this;
		enemiesToCache = new()
		{
			{"AVARICE_WAVES_Secret", [
				new EnemyData { type = Enemy.Lurker, name = "_E_LURKER" },
				new EnemyData { type = Enemy.Knight, name = "_E_KNIGHT" },
				new EnemyData { type = Enemy.MageRedPurple, name = "_E_MAGE_REDPURPLE Variant" },
				new EnemyData { type = Enemy.BruteGold, name = "_E_BRUTE_GOLD Variant" },
				new EnemyData { type = Enemy.GruntRedeemerIndoor, name = "_E_GRUNT_Redeemer_Indoor Variant" },
				new EnemyData { type = Enemy.MagePlagueSingleshot, name = "_E_MAGE_Plague_SingleShot" },
				new EnemyData { type = Enemy.BatWhite, name = "_E_BAT_White" },
				new EnemyData { type = Enemy.GhoulRapid, name = "_E_GHOUL_Rapid Variant" },
				new EnemyData { type = Enemy.LurkerMother, name = "_E_LURKER_MOTHER" },
			]},
			{"lvl_Forest", [
				new EnemyData { type = Enemy.DekuScrub, name = "_E_DEKU_SCRUB" },
				new EnemyData { type = Enemy.Plant, name = "_E_PLANT" },
			]},
			{"lvl_Graveyard", [
				new EnemyData { type = Enemy.GruntRedeemer, name = "_E_GRUNT_Redeemer" },
				new EnemyData { type = Enemy.GruntGrave, name = "_E_GRUNT_Grave Variant" },
				new EnemyData { type = Enemy.BatBlack, name = "_E_BAT_Black Variant" },
				new EnemyData { type = Enemy.HeadrollerHeadless, name = "_E_HEADROLLER_HEADLESS Variant" },
			]},
			{"lvl_GrandmaGardens", [
				new EnemyData { type = Enemy.SlimeMedGreen, name = "_E_Slime_med_GREEN Variant" },
				new EnemyData { type = Enemy.SlimeSmallGreen, name = "_E_Slime_small_GREEN Variant" },
				new EnemyData { type = Enemy.SlimeBigGreen, name = "_E_Slime_big_GREEN Variant" },
			]},
			{"lvl_Swamp", [
				new EnemyData { type = Enemy.FireplantNoHookshot, name = "_E_FIREPLANT_Nohookshot" },
				new EnemyData { type = Enemy.GhoulLongRange, name = "_E_GHOUL_LongRange Variant" },
				new EnemyData { type = Enemy.Grunt, name = "_E_GRUNT" },
				new EnemyData { type = Enemy.MagePlague, name = "_E_MAGE_Plague Variant" },
			]},
			{"lvl_mountaintops", [
				new EnemyData { type = Enemy.Brute, name = "_E_BRUTE" },
				new EnemyData { type = Enemy.Fireplant, name = "_E_FIREPLANT" },
				new EnemyData { type = Enemy.MageGrounded, name = "_E_MAGE_Grounded Variant" },
			]},
			{"AVARICE_WAVES_Fortress", [
				new EnemyData { type = Enemy.SlimeSmall, name = "_E_Slime_small" },
				new EnemyData { type = Enemy.Jumper, name = "_E_JUMPER" },
				new EnemyData { type = Enemy.SlimeMed, name = "_E_Slime_med" },
				new EnemyData { type = Enemy.MageFire, name = "_E_MAGE_Fire Variant" },
				new EnemyData { type = Enemy.GruntYeti, name = "_E_GRUNT_Yeti Variant" },
			]},
			{"lvl_SailorMountain", [
				new EnemyData { type = Enemy.PlagueKnightSlowFire, name = "_E_PLAGUE_KNIGHT_SlowFire" },
			]},
			{"lvl_GrandmaBasement", [
				new EnemyData { type = Enemy.SlimeBig, name = "_E_Slime_big" },
				new EnemyData { type = Enemy.Headroller, name = "_E_HEADROLLER" },
				new EnemyData { type = Enemy.PlagueKnight, name = "_E_PLAGUE_KNIGHT" },
			]},
			{"TEST_AREA", [
				new EnemyData { type = Enemy.Mage, name = "_E_MAGE" },
				new EnemyData { type = Enemy.GruntPot, name = "_E_GRUNT_Pot Variant" },
				new EnemyData { type = Enemy.GrimaceKnight, name = "_E_GRIMACE_KNIGHT" },
				new EnemyData { type = Enemy.Dodger, name = "_E_DODGER" },
				new EnemyData { type = Enemy.KnightGreenPlague, name = "_E_KNIGHT_GreenPlague" },
				new EnemyData { type = Enemy.Ghoul, name = "_E_GHOUL" },
			]},
			{"lvl_SilentServant_Fight", [
				new EnemyData { type = Enemy.ServantAll, name = "_E_SERVANT_All" },
				new EnemyData { type = Enemy.ServantBomb, name = "_E_SERVANT_Bombs" },
				new EnemyData { type = Enemy.ServantFire, name = "_E_SERVANT_FIRE Variant" },
				new EnemyData { type = Enemy.ServantHookshot, name = "_E_SERVANT_Hookshot Variant" },
			]},
			{"boss_Frog", [
				new EnemyData { type = Enemy.GruntFrog, name = "_E_GRUNT_Frog" },
			]},
		};
	}

	private void OnEnable()
	{
		Logger.Log("Enemy randomizer started!");

		// Preload enemies
		foreach (var kvp in enemiesToCache)
		{
			string sceneName = kvp.Key;
			EnemyData[] dataForEnemies = kvp.Value;

			Preloader.Instance.AddObjectToCacheList(sceneName, () =>
			{
				List<GameObject> enemies = new();

				foreach (EnemyData enemyData in dataForEnemies)
				{
					GameObject foundEnemy = null;

					foreach (AI_Brain brain in Resources.FindObjectsOfTypeAll<AI_Brain>())
					{
						if (brain.name == enemyData.name)
						{
							foundEnemy = brain.gameObject;
							foundEnemy.SetActive(true);

							// Modify silent servants
							if (foundEnemy.TryGetComponent(out AI_SilentServant ai))
							{
								DestroyImmediate(ai.GetComponentInChildren<CameraFocusObjectLimited>(true).gameObject);
								ai.hasHookshot = false;

								if (enemyData.type == Enemy.ServantAll)
								{
									ai.hasAllPowers = false;
									ai.hasBombs = true;
									ai.hasFire = true;
								}
							}
						}
					}

					if (foundEnemy == null)
					{
						Logger.LogError($"During enemy rando preload, failed to find enemy\n    {enemyData.name}\n    in {sceneName}");
						continue;
					}

					enemies.Add(foundEnemy);
				}

				Logger.Log($"Finished caching enemies for scene {sceneName}!");
				return enemies.ToArray();
			});
		}
	}

	// TODO: Remove this, as this is for quick testing before figuring out enemy replacements
	int index = 0;
	private void Update()
	{
		if (Input.GetKeyDown("t"))
		{
			SpawnEnemyAtPlayer((Enemy)index);
			index++;
		}
	}

	public GameObject SpawnEnemyAtPlayer(Enemy enemyType)
	{
		Transform player = PlayerGlobal.instance.transform;
		Vector3 position = player.position - player.forward * 2f;
		GameObject enemy = Instantiate(GetEnemy(enemyType));
		enemy.transform.position = position;
		return enemy;
	}

	private GameObject GetEnemy(Enemy enemyType)
	{
		foreach (EnemyData data in enemiesToCache.Values.SelectMany(x => x))
		{
			if (data.type == enemyType)
			{
				return Preloader.GetCachedObject<GameObject>(data.name);
			}
		}

		Logger.LogError($"No enemy {enemyType} was found in preload list");
		return null;
	}

	private IEnumerator SilentServantDeath(AI_SilentServant servant)
	{
		yield return new WaitForSeconds(3);
		Destroy(servant.gameObject);
	}

	private struct EnemyData
	{
		public Enemy type;
		public string name;
	}

	[HarmonyPatch]
	private class Patches
	{
		// Removes the corpse of killed silent servants, as the cutscene that removes them doesn't play
		[HarmonyPostfix]
		[HarmonyPatch(typeof(AI_SilentServant), nameof(AI_SilentServant.OnDeath))]
		private static void SilentServantDeathRemoveCorpsePatch(AI_SilentServant __instance)
		{
			if (!Archipelago.Instance.IsConnected())
				return;

			Instance.StartCoroutine(Instance.SilentServantDeath(__instance));
		}
	}
}