using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace DDoor.ArchipelagoRandomizer;

internal class EnemyRandomizer : MonoBehaviour
{
	/* List of enemies that have been removed (kept as a reference in case they make a comeback
	 * 
	 * _E_PLAGUE_KNIGHT_SlowFire
	 *		Scene: lvl_SailorMountain
	 *		Reason: Their pathfinding doesn't work often times, and they'll get stuck on walls during chase state
	*/

	public enum EnemyType
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
		MagePlague,
		MagePlagueSingleshot,
		MageRedPurple,
		PlagueKnight,
		Plant,
		PotMimicAvariceExplode,
		PotMimicAvariceMagic,
		PotMimicAvariceMelee,
		PotMimicExplode,
		PotMimicMagic,
		PotMimicMelee,
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
	private static readonly Array enemyValues = Enum.GetValues(typeof(EnemyType));
	private Dictionary<string, EnemyData[]> enemiesToCache;
	private readonly Dictionary<EnemyType, EnemyData> enemyTypeLookup = new();
	private readonly Dictionary<EnemyType, ConfigEntry<bool>> enemyTypeEnabled = [];
	private List<string> enemiesToNotReplace;
	private bool hasSpawnedFireColumn;

	public static EnemyRandomizer Instance => instance;

	private void Awake()
	{
		instance = this;
		enemiesToCache = new()
		{
			{"AVARICE_WAVES_Secret", [
				new EnemyData { type = EnemyType.Lurker, name = "_E_LURKER", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.Knight, name = "_E_KNIGHT", weightTier = EnemyData.WeightTier.Tier4 },
				new EnemyData { type = EnemyType.MageRedPurple, name = "_E_MAGE_REDPURPLE Variant", weightTier = EnemyData.WeightTier.Tier2 },
				new EnemyData { type = EnemyType.BruteGold, name = "_E_BRUTE_GOLD Variant", weightTier = EnemyData.WeightTier.Tier3 },
				new EnemyData { type = EnemyType.GruntRedeemerIndoor, name = "_E_GRUNT_Redeemer_Indoor Variant", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.MagePlagueSingleshot, name = "_E_MAGE_Plague_SingleShot", weightTier = EnemyData.WeightTier.Tier2 },
				new EnemyData { type = EnemyType.BatWhite, name = "_E_BAT_White", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.GhoulRapid, name = "_E_GHOUL_Rapid Variant", weightTier = EnemyData.WeightTier.Tier2 },
				new EnemyData { type = EnemyType.LurkerMother, name = "_E_LURKER_MOTHER", weightTier = EnemyData.WeightTier.Tier3 },
			]},
			{"lvl_Forest", [
				new EnemyData { type = EnemyType.DekuScrub, name = "_E_DEKU_SCRUB", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.Plant, name = "_E_PLANT", weightTier = EnemyData.WeightTier.Tier1 },
			]},
			{"lvl_Graveyard", [
				new EnemyData { type = EnemyType.GruntRedeemer, name = "_E_GRUNT_Redeemer", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.GruntGrave, name = "_E_GRUNT_Grave Variant", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.BatBlack, name = "_E_BAT_Black Variant", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.HeadrollerHeadless, name = "_E_HEADROLLER_HEADLESS Variant", weightTier = EnemyData.WeightTier.Tier1 },
			]},
			{"lvl_GrandmaGardens", [
				new EnemyData { type = EnemyType.SlimeMedGreen, name = "_E_Slime_med_GREEN Variant", weightTier = EnemyData.WeightTier.Tier2 },
				new EnemyData { type = EnemyType.SlimeSmallGreen, name = "_E_Slime_small_GREEN Variant", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.SlimeBigGreen, name = "_E_Slime_big_GREEN Variant", weightTier = EnemyData.WeightTier.Tier4 },
				new EnemyData { type = EnemyType.PotMimicAvariceExplode, name = "POT_Mimic_Explode_AVARICE Variant", weightTier = EnemyData.WeightTier.Tier2 },
				new EnemyData { type = EnemyType.PotMimicAvariceMagic, name = "POT_Mimic_Magic_AVARICE Variant", weightTier = EnemyData.WeightTier.Tier2 },
				new EnemyData { type = EnemyType.PotMimicAvariceMelee, name = "POT_Mimic_Melee_AVARICE Variant", weightTier = EnemyData.WeightTier.Tier2 },
			]},
			{"lvl_GrandmaMansion", [
				new EnemyData { type = EnemyType.PotMimicMagic, name = "POT_Mimic_Magic", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.PotMimicExplode, name = "POT_Mimic_Explode", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.PotMimicMelee, name = "POT_Mimic_Melee", weightTier = EnemyData.WeightTier.Tier1 },
			]},
			{"lvl_Swamp", [
				new EnemyData { type = EnemyType.FireplantNoHookshot, name = "_E_FIREPLANT_Nohookshot", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.GhoulLongRange, name = "_E_GHOUL_LongRange Variant", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.Grunt, name = "_E_GRUNT", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.MagePlague, name = "_E_MAGE_Plague Variant", weightTier = EnemyData.WeightTier.Tier3 },
			]},
			{"lvl_mountaintops", [
				new EnemyData { type = EnemyType.Brute, name = "_E_BRUTE", weightTier = EnemyData.WeightTier.Tier2 },
				new EnemyData { type = EnemyType.Fireplant, name = "_E_FIREPLANT", weightTier = EnemyData.WeightTier.Tier1 },
			]},
			{"AVARICE_WAVES_Fortress", [
				new EnemyData { type = EnemyType.SlimeSmall, name = "_E_Slime_small", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.Jumper, name = "_E_JUMPER", weightTier = EnemyData.WeightTier.Tier3 },
				new EnemyData { type = EnemyType.SlimeMed, name = "_E_Slime_med", weightTier = EnemyData.WeightTier.Tier2 },
				new EnemyData { type = EnemyType.GruntYeti, name = "_E_GRUNT_Yeti Variant", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.MageFire, name = "_E_MAGE_Fire Variant", weightTier = EnemyData.WeightTier.Tier2 },
			]},
			{"lvl_GrandmaBasement", [
				new EnemyData { type = EnemyType.SlimeBig, name = "_E_Slime_big", weightTier = EnemyData.WeightTier.Tier3 },
				new EnemyData { type = EnemyType.Headroller, name = "_E_HEADROLLER", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.PlagueKnight, name = "_E_PLAGUE_KNIGHT", weightTier = EnemyData.WeightTier.Tier2 },
			]},
			{"TEST_AREA", [
				new EnemyData { type = EnemyType.Mage, name = "_E_MAGE", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.GruntPot, name = "_E_GRUNT_Pot Variant", weightTier = EnemyData.WeightTier.Tier1 },
				new EnemyData { type = EnemyType.GrimaceKnight, name = "_E_GRIMACE_KNIGHT", weightTier = EnemyData.WeightTier.Tier4 },
				new EnemyData { type = EnemyType.Dodger, name = "_E_DODGER", weightTier = EnemyData.WeightTier.Tier3 },
				new EnemyData { type = EnemyType.KnightGreenPlague, name = "_E_KNIGHT_GreenPlague", weightTier = EnemyData.WeightTier.Tier5 },
				new EnemyData { type = EnemyType.Ghoul, name = "_E_GHOUL", weightTier = EnemyData.WeightTier.Tier1 },
			]},
			{"lvl_SilentServant_Fight", [
				new EnemyData { type = EnemyType.ServantAll, name = "_E_SERVANT_All", weightTier = EnemyData.WeightTier.Tier5 },
				new EnemyData { type = EnemyType.ServantBomb, name = "_E_SERVANT_Bombs", weightTier = EnemyData.WeightTier.Tier5 },
				new EnemyData { type = EnemyType.ServantFire, name = "_E_SERVANT_FIRE Variant", weightTier = EnemyData.WeightTier.Tier5 },
				new EnemyData { type = EnemyType.ServantHookshot, name = "_E_SERVANT_Hookshot Variant", weightTier = EnemyData.WeightTier.Tier5 },
			]},
			{"boss_Frog", [
				new EnemyData { type = EnemyType.GruntFrog, name = "_E_GRUNT_Frog", weightTier = EnemyData.WeightTier.Tier1 },
			]},
		};
		enemiesToNotReplace = new()
		{
			"redeemer_BOSS",
			"redeemer_cutscene",
			"BOSS_GraveDigger",
			"_FORESTMOTHER_BOSS",
			"BOSS_lord_of_doors NEW",
			"BOSS_lord_of_doors_Betty",
			"BOSS_lord_of_doors_Garden",
			"lord_of_doors",
			"lord_of_doorsOLD",
			"BOSS_lord_of_doors_Forest",
			"grandma",
			"oldfrog",
			"FROG_BOSS_FAT",
			"FROG_BOSS_MAIN",
			"betty_boss",
			"OLD_CROW_BOSS(old)",
			"old_crow_boss_fbx",
		};

		// Setup enemyType -> EnemyData lookup dict
		foreach (EnemyData[] dataArr in enemiesToCache.Values)
		{
			foreach (EnemyData data in dataArr)
			{
				if (!enemyTypeLookup.ContainsKey(data.type))
				{
					enemyTypeLookup.Add(data.type, data);
					enemyTypeEnabled[data.type] = Plugin.Instance.Config.Bind("Enemy Randomizer", data.type.ToString(), true, $"Enable enemy {data.type}");
				}
			}
		}
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

							// If MageFire, cache the fire column pool so it can attack
							if (enemyData.type == EnemyType.MageFire)
							{
								GameObject fireColumnPool = GameObject.Find("_FIRE_COLUMN_POOL");
								Preloader.CacheObject(fireColumnPool);
							}

							// Modify silent servants
							if (foundEnemy.TryGetComponent(out AI_SilentServant ai))
							{
								DestroyImmediate(ai.GetComponentInChildren<CameraFocusObjectLimited>(true).gameObject);
								ai.hasHookshot = false;

								if (enemyData.type == EnemyType.ServantAll)
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

		Preloader.Instance.OnPreloadDone += AfterPreload;
	}

	private void OnDisable()
	{
		Preloader.Instance.OnPreloadDone -= AfterPreload;
		SceneManager.sceneLoaded -= SceneLoaded;
	}

	//TODO: Remove this, as this is for quick testing before figuring out enemy replacements

	//int index = 0;
	//private void Update()
	//{
	//	if (Input.GetKeyDown("t"))
	//	{
	//		//SpawnEnemyAtPlayer((EnemyType)index);
	//		//Logger.Log($"Spawned enemy: {(EnemyType)index}");
	//		//SpawnEnemyAtPlayer(EnemyType.PlagueKnightSlowFire);
	//		index++;
	//	}
	//}

	private void AfterPreload()
	{
		SceneManager.sceneLoaded += SceneLoaded;
	}

	private void SceneLoaded(Scene scene, LoadSceneMode _)
	{
		hasSpawnedFireColumn = false;
		ReplaceEnemiesInScene();
	}

	public GameObject SpawnEnemyAtPlayer(EnemyType enemyType)
	{
		Transform player = PlayerGlobal.instance.transform;
		Vector3 position = player.position - player.forward * 2f;
		GameObject newEnemy = Instantiate(GetEnemy(enemyType));
		newEnemy.transform.position = position;
		return newEnemy;
	}

	private void ReplaceEnemiesInScene()
	{
		Stopwatch sw = Stopwatch.StartNew();
		Scene scene = SceneManager.GetActiveScene();

		foreach (GameObject rootObj in scene.GetRootGameObjects())
		{
			foreach (Component comp in rootObj.GetComponentsInChildren<Component>(true))
			{
				GameObject replacement = null;

				switch (comp)
				{
					// Replace enemies that don't spawn from spawners
					case AI_Brain brain:
						// Don't replace silent servants because there's no good way to trigger
						// the death cutscene afterwards, so player gets stuck and gets no item
						if (brain is AI_SilentServant)
							break;

						replacement = GetEnemyReplacement(comp);

						if (replacement == null)
							break;

						Vector3 enemyPos = brain.transform.position;
						Transform enemyParent = brain.transform.parent;
						Destroy(brain.gameObject);
						GameObject newEnemy = Instantiate(replacement, enemyParent);
						newEnemy.transform.position = enemyPos;
						break;
					// Replace enemies that spawn from spawners
					case WaveEnemy wave:
						replacement = GetEnemyReplacement(comp);

						if (replacement == null)
							break;

						wave.prefab = replacement;
						break;
					// Replace enemies that spawn from eggs (Lurkers)
					case LurkerEgg egg:
						replacement = GetEnemyReplacement(comp);

						if (replacement == null)
							break;

						egg.contentsPrefab = replacement;
						break;
				}
			}
		}

		sw.Stop();
		Logger.Log($"Took {sw.ElapsedMilliseconds}ms to replace all enemies in {scene.name}");
	}

	private GameObject GetEnemyReplacement(Component comp)
	{
		string enemyName = comp switch
		{
			AI_Brain brain => brain.name,
			WaveEnemy wave => wave.prefab.name,
			LurkerEgg egg => egg.contentsPrefab.name,
			_ => ""
		};

		// Ignore exclusions
		if (enemiesToNotReplace.Contains(enemyName))
			return null;

		GameObject randomEnemy = GetRandomEnemy();
		return randomEnemy;
	}

	private GameObject GetRandomEnemy()
	{
		List<EnemyData> enemies = enemyTypeLookup.Values.Where(data => enemyTypeEnabled[data.type].Value == true).ToList();
		int totalWeight = enemies.Sum(e => e.GetWeight());
		int roll = Random.Range(0, totalWeight);
		int cumulative = 0;

		foreach (EnemyData enemy in enemies)
		{
			cumulative += enemy.GetWeight();

			if (roll < cumulative)
			{
				// If MageFire, instantiate the fire column pool so it can attack
				if (!hasSpawnedFireColumn && enemy.type == EnemyType.MageFire)
				{
					Instantiate(Preloader.GetCachedObject<GameObject>("_FIRE_COLUMN_POOL"));
					hasSpawnedFireColumn = true;
				}

				return Preloader.GetCachedObject<GameObject>(enemy.name);
			}
		}

		return null;
	}

	private GameObject GetEnemy(EnemyType enemyType)
	{
		if (enemyTypeLookup.TryGetValue(enemyType, out EnemyData data))
		{
			// If MageFire, instantiate the fire column pool so it can attack
			if (!hasSpawnedFireColumn && enemyType == EnemyType.MageFire)
			{
				Instantiate(Preloader.GetCachedObject<GameObject>("_FIRE_COLUMN_POOL"));
				hasSpawnedFireColumn = true;
			}

			return Preloader.GetCachedObject<GameObject>(data.name);
		}

		Logger.LogError($"No enemy {enemyType} was found in cache list");
		return null;
	}

	private IEnumerator SilentServantDeath(AI_SilentServant servant)
	{
		yield return new WaitForSeconds(3);
		Destroy(servant.gameObject);
	}

	private struct EnemyData
	{
		public EnemyType type;
		public string name;
		public WeightTier weightTier;
		/// <summary>
		/// Set this greater than 0 to set a custom weight for this enemy
		/// </summary>
		public int weight = -1;

		public EnemyData() { }

		public int GetWeight()
		{
			return weight > 0 ? weight : (int)weightTier;
		}

		public enum WeightTier
		{
			Tier1 = 50,
			Tier2 = 30,
			Tier3 = 15,
			Tier4 = 5,
			Tier5 = 1,
		}

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

		// Spawns the randomized enemy whewn a Lurker (spider) egg explodes
		[HarmonyPrefix]
		[HarmonyPatch(typeof(LurkerEgg), nameof(LurkerEgg.Explode))]
		private static bool LurkerEggExplodePatch(LurkerEgg __instance, Vector3 source, float modifier = 1f)
		{
			if (!__instance.didExplode)
			{
				__instance.didExplode = true;
				Vector3 vector = __instance.transform.position - source;
				vector.Normalize();
				vector.x += Random.Range(-0.1f, 0.1f);
				vector.z += Random.Range(-0.1f, 0.1f);
				float y = Mathf.Atan2(vector.x, vector.z) * 57.29578f;
				GameObject gameObject = Instantiate(__instance.contentsPrefab, __instance.transform.position, Quaternion.Euler(0f, y, 0f));
				gameObject.transform.SetParent(GameRoom.GetCurrentContents());
				Destroy(__instance.gameObject);
				return false;
			}

			return true;
		}

		// Fixes boomerang throwers to not jump in place in scenes that don't have Node objects
		[HarmonyPostfix]
		[HarmonyPatch(typeof(AI_Jumper), nameof(AI_Jumper.SetState))]
		private static void BoomerangThrowerFixJumping(AI_Jumper __instance, AI_Brain.AIState newState)
		{
			if (newState == AI_Brain.AIState.Jump && !(bool)__instance.currentNode)
			{
				__instance.endJump = PlayerGlobal.instance.transform.position;
			}
		}
	}
}