using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using IC = DDoor.ItemChanger;

namespace DDoor.ArchipelagoRandomizer;

/// <summary>
/// Shop upgrades as checks, planting checks and reporting opened key doors (apworld 0.4.0+).
/// Everything here is inert unless the connected seed's slot data turns it on.
/// </summary>
internal static class ShopAndPlanting
{
	// Game inventory ids of the four stats, with the names used for AP items/locations
	internal static readonly Dictionary<string, string> StatNames = new()
	{
		{ "stat_melee", "Strength" },
		{ "stat_dexterity", "Dexterity" },
		{ "stat_haste", "Haste" },
		{ "stat_magic", "Magic" },
	};
	private const int UpgradesPerStat = 5;

	// BaseKey.uniqueId of the 12 coloured key doors
	private static readonly string[] KeyDoorIds =
	[
		"keydoor_covenant", "keydoor_graveyardsummit", "keydoor_graveyard1", "ffort_key1", "ffort_key2",
		"mlock_0", "mlock_1", "mlock_elevator",
		"fstlock_0", "fstlock_1", "fstlock_2", "fstlock_4",
	];

	private static bool shopEnabled;
	private static Dictionary<string, List<int>> shopPrices = new();
	private static List<int> plantingThresholds = new();

	// Which stat the game is currently asking a price for (set around the game's own cost lookups)
	private static string costContextStat;

	internal static bool ShopActive => shopEnabled && Archipelago.Instance.IsConnected();

	internal static void Init()
	{
		Archipelago.OnConnected += ReadSlotData;
		SceneManager.sceneLoaded += (_, _) => SyncAll();
	}

	private static void ReadSlotData()
	{
		shopEnabled = Archipelago.Instance.TryGetSlotData("shop_upgrades", out object shop) && shop is bool b && b;
		shopPrices = new();
		if (Archipelago.Instance.TryGetSlotData("shop_price_table", out object prices) && prices is JObject table)
		{
			foreach (KeyValuePair<string, JToken> kvp in table)
			{
				shopPrices[kvp.Key] = kvp.Value.ToObject<List<int>>();
			}
		}
		plantingThresholds = new();
		if (Archipelago.Instance.TryGetSlotData("planting_thresholds", out object thresholds) && thresholds is JArray arr)
		{
			plantingThresholds = arr.ToObject<List<int>>();
		}
		Logger.Log($"Shop upgrades: {shopEnabled}, planting checks at: {string.Join(", ", plantingThresholds)}");
	}

	private static void SyncAll()
	{
		if (!Archipelago.Instance.IsConnected() || GameSave.currentSave == null || PlayerGlobal.instance == null)
		{
			return;
		}
		SyncPlanting();
		SyncKeyDoors();
	}

	// ---------------------------------------------------------------- shared
	private static string ShopLocation(string statId, int level) => $"Shop - {StatNames[statId]} Upgrade {level}";

	private static string PlantingLocation(int count) => $"Planted {count} Life Seed{(count == 1 ? "" : "s")}";

	private static string PurchasedKey(string statId) => $"AP_ShopPurchased-{statId}";

	private static int Purchased(string statId) => GameSave.GetSaveData().GetCountKey(PurchasedKey(statId));

	/// <summary>Sends a check for one of the new (non-ItemChanger) locations, at most once per save.</summary>
	private static void CheckLocation(string location)
	{
		if (GameSave.GetSaveData().IsKeyUnlocked($"AP_PickedUp-{location}"))
		{
			return;
		}
		IC.SaveData icSaveData = IC.SaveData.Open();
		if (icSaveData != null && icSaveData.UnnamedPlacements.TryGetValue(location, out IC.Item item))
		{
			// Same path as an ItemChanger pickup: marks the save, records it and sends it to the server
			item.Trigger();
			// Items for this player get their popup when they arrive from the server; show other players' items now
			if (item is ItemRandomizer.DDItem ddItem && ddItem.IsForAnotherPlayer)
			{
				try
				{
					IC.CornerPopup.Show(IC.ItemIcons.Get(ddItem.Icon), $"Sent {ddItem.DisplayName}");
				}
				catch (System.Exception e)
				{
					Logger.LogWarning($"Couldn't show popup for {location}: {e.Message}");
				}
			}
		}
		else
		{
			GameSave.GetSaveData().SetKeyState($"AP_PickedUp-{location}", true, true);
			Archipelago.Instance.GetAPSaveData().AddCheckedLocation(location);
			Archipelago.Instance.SendLocationChecked(location);
		}
		Logger.Log($"Checked {location}");
	}

	// ---------------------------------------------------------------- planting
	private static void SyncPlanting()
	{
		if (plantingThresholds.Count == 0)
		{
			return;
		}
		int planted = GameSave.GetSaveData().GetCountKey("plant_count");
		foreach (int threshold in plantingThresholds.Where(t => t <= planted))
		{
			CheckLocation(PlantingLocation(threshold));
		}
	}

	// ---------------------------------------------------------------- key doors
	private static void SyncKeyDoors()
	{
		List<string> opened = KeyDoorIds.Where(id => GameSave.GetSaveData().IsKeyUnlocked(id)).ToList();
		Archipelago.Instance.StoreOpenedKeyDoors(opened);
	}

	// ---------------------------------------------------------------- shop
	internal static void ApplyStatUpgrade(string statId)
	{
		Inventory.instance.AddItem(statId, 1, true);
		PlayerGlobal.instance?.CheckUpgrades();
	}

	private static bool IsApStat(UI_StatUpgrade cell) => ShopActive && cell != null && shopPrices.ContainsKey(cell.itemId);

	private static int PriceFor(string statId, int purchased)
	{
		List<int> prices = shopPrices[statId];
		return prices[Mathf.Clamp(purchased, 0, prices.Count - 1)];
	}

	/// <summary>Received "Progressive Strength" etc.</summary>
	internal readonly struct StatUpgradeItem(string statId) : IC.Item
	{
		public string DisplayName => $"Progressive {StatNames[statId]}";
		public string Icon => "Soul";
		public void Trigger() => ApplyStatUpgrade(statId);
	}

	[HarmonyPatch]
	private static class Patches
	{
		// Show the number of upgrades bought (not the stat level) on the shop bar
		[HarmonyPostfix, HarmonyPatch(typeof(UI_StatUpgrade), nameof(UI_StatUpgrade.Start))]
		private static void ShowPurchasesOnStart(UI_StatUpgrade __instance) => ShowPurchases(__instance);

		[HarmonyPostfix, HarmonyPatch(typeof(UI_StatUpgrade), nameof(UI_StatUpgrade.checkItem))]
		private static void ShowPurchasesOnCheck(UI_StatUpgrade __instance) => ShowPurchases(__instance);

		private static void ShowPurchases(UI_StatUpgrade __instance)
		{
			if (!IsApStat(__instance))
			{
				return;
			}
			__instance.statValue = Purchased(__instance.itemId);
			__instance.updateBar();
		}

		// Buying sends a check instead of raising the stat
		[HarmonyPrefix, HarmonyPatch(typeof(UI_StatUpgrade), nameof(UI_StatUpgrade.Purchase))]
		private static bool Purchase(UI_StatUpgrade __instance)
		{
			if (!IsApStat(__instance))
			{
				return true;
			}
			string statId = __instance.itemId;
			int purchased = Purchased(statId);
			if (purchased >= UpgradesPerStat)
			{
				return false;
			}
			int cost = PriceFor(statId, purchased);
			InventoryItem currency = Inventory.instance.GetItem("currency");
			if (currency == null || currency.stackCount < cost)
			{
				return false;
			}

			Inventory.instance.ConsumeItem(currency, cost, false);
			GameSave.GetSaveData().SetCountKey(PurchasedKey(statId), purchased + 1);

			// Same feedback as the game's own purchase
			ScreenFade.instance.fadeColor = new Color(1f, 1f, 1f, 0.4f);
			ScreenFade.instance.FadeIn(0.25f, true, null);
			__instance.purchaseTween = 1f;
			__instance.statValue = purchased + 1;
			__instance.updateBar();
			GameSave.GetSaveData().Save();
			__instance.checkCanAfford();

			// Last, so a problem sending the check can't leave the shop showing the old tier and price
			try
			{
				CheckLocation(ShopLocation(statId, purchased + 1));
			}
			catch (System.Exception e)
			{
				Logger.LogError($"Sending shop check failed: {e}");
			}
			return false;
		}

		// The game prices by stat level only; remember which stat is being priced so GetCostPerLevel can answer
		[HarmonyPrefix, HarmonyPatch(typeof(UI_StatUpgrade), nameof(UI_StatUpgrade.checkCanAfford))]
		private static void CellCostContext(UI_StatUpgrade __instance) =>
			costContextStat = IsApStat(__instance) ? __instance.itemId : null;

		[HarmonyPrefix, HarmonyPatch(typeof(UIMenuStatUpgrades), nameof(UIMenuStatUpgrades.checkCurrency))]
		private static void MenuCostContextCheck(UIMenuStatUpgrades __instance) => MenuCostContext(__instance);

		[HarmonyPrefix, HarmonyPatch(typeof(UIMenuStatUpgrades), nameof(UIMenuStatUpgrades.purchase))]
		private static void MenuCostContextPurchase(UIMenuStatUpgrades __instance) => MenuCostContext(__instance);

		private static void MenuCostContext(UIMenuStatUpgrades __instance)
		{
			UI_StatUpgrade cell = __instance.grid != null && __instance.index >= 0 && __instance.index < __instance.grid.Length
				? __instance.grid[__instance.index]
				: null;
			costContextStat = IsApStat(cell) ? cell.itemId : null;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(UI_StatUpgrade), nameof(UI_StatUpgrade.checkCanAfford))]
		private static void ClearCellCostContext() => costContextStat = null;

		[HarmonyPostfix, HarmonyPatch(typeof(UIMenuStatUpgrades), nameof(UIMenuStatUpgrades.checkCurrency))]
		private static void ClearMenuCheckContext() => costContextStat = null;

		[HarmonyPostfix, HarmonyPatch(typeof(UIMenuStatUpgrades), nameof(UIMenuStatUpgrades.purchase))]
		private static void ClearMenuPurchaseContext() => costContextStat = null;

		[HarmonyPostfix, HarmonyPatch(typeof(Inventory), nameof(Inventory.GetCostPerLevel))]
		private static void CostPerLevel(int lvl, ref int __result)
		{
			if (costContextStat != null && ShopActive && shopPrices.ContainsKey(costContextStat))
			{
				__result = PriceFor(costContextStat, lvl);
			}
		}

		// Planting a seed may reach a planting check
		[HarmonyPostfix, HarmonyPatch(typeof(HealPlant), nameof(HealPlant.UpdatePlant))]
		private static void Planted(HealPlant p)
		{
			if (!Archipelago.Instance.IsConnected())
			{
				return;
			}
			Logger.Log($"Planted seed in pot '{p.id}', total {GameSave.GetSaveData().GetCountKey("plant_count")}");
			SyncPlanting();
		}

		// Opening a coloured key door is reported to the tracker
		[HarmonyPostfix, HarmonyPatch(typeof(BaseKey), nameof(BaseKey.Unlock))]
		private static void Unlocked(BaseKey __instance)
		{
			if (!Archipelago.Instance.IsConnected() || !KeyDoorIds.Contains(__instance.uniqueId))
			{
				return;
			}
			Logger.Log($"Opened key door '{__instance.uniqueId}' in {SceneManager.GetActiveScene().name}");
			SyncKeyDoors();
		}
	}
}
