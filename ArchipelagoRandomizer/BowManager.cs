using HarmonyLib;
using System;
using UnityEngine;
using IC = DDoor.ItemChanger;

namespace DDoor.ArchipelagoRandomizer;

/// <summary>
/// Start With Bow (YAML) off: the plain arrow can't be aimed or shot until the Reaper's Bow is received. The spells
/// (Fire, Bomb, Hookshot) still work.
/// </summary>
[HarmonyPatch]
internal static class BowManager
{
	internal const string ItemName = "Reaper's Bow";
	private const string HasBowKey = "AP_ReapersBow";
	private const float messageCooldown = 4f;

	private static WeaponOffhandControl currentControl;
	private static float lastMessageTime = float.NegativeInfinity;

	/// <summary>Seeds from apworlds before 0.5.0 don't have the option: you always have the bow there</summary>
	private static bool StartsWithBow =>
		!Archipelago.Instance.TryGetSlotData("start_with_bow", out object value) || value == null || Convert.ToBoolean(value);

	internal static bool HasBow()
	{
		if (!Archipelago.Instance.IsConnected() || StartsWithBow)
		{
			return true;
		}
		return GameSave.currentSave != null && GameSave.currentSave.GetCountKey(HasBowKey) > 0;
	}

	/// <summary>Received the "Reaper's Bow" item</summary>
	internal readonly struct BowItem : IC.Item
	{
		public string DisplayName => ItemName;
		public string Icon => "Arrow2";
		public void Trigger()
		{
			GameSave.currentSave?.SetCountKey(HasBowKey, 1);
		}
	}

	private static bool IsBlocked(WeaponOffhandControl control)
	{
		return control != null && control.currentWeaponitem != null && control.currentWeaponitem.id == "arrow" && !HasBow();
	}

	[HarmonyPrefix, HarmonyPatch(typeof(WeaponOffhandControl), nameof(WeaponOffhandControl.FixedUpdate))]
	private static void RememberControl(WeaponOffhandControl __instance)
	{
		currentControl = __instance;
		try
		{
			if (IsBlocked(__instance) && Buttons.Held("Shoot") && Time.realtimeSinceStartup - lastMessageTime > messageCooldown)
			{
				lastMessageTime = Time.realtimeSinceStartup;
				UIManager.Instance.ShowNotification("You don't have the Reaper's Bow yet");
			}
		}
		catch (Exception e)
		{
			Logger.LogError($"Bow check failed: {e}");
		}
	}

	// The game asks this before aiming; the plain arrow says no without the bow
	[HarmonyPostfix, HarmonyPatch(typeof(_ArrowPower), nameof(_ArrowPower.CanAim))]
	private static void BlockAiming(_ArrowPower __instance, ref bool __result)
	{
		if (__result && currentControl != null && currentControl.arrowPower == __instance && IsBlocked(currentControl))
		{
			__result = false;
		}
	}

	// In case something shoots without aiming first
	[HarmonyPrefix, HarmonyPatch(typeof(_ArrowPower), nameof(_ArrowPower.Held))]
	private static bool BlockHeld(_ArrowPower __instance)
	{
		return !(currentControl != null && currentControl.arrowPower == __instance && IsBlocked(currentControl));
	}
}
