using HarmonyLib;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using IC = DDoor.ItemChanger;

namespace DDoor.ArchipelagoRandomizer;

internal class ItemRandomizer : MonoBehaviour
{
	private static ItemRandomizer instance;
	private IC.SaveData icSaveData;

	public static ItemRandomizer Instance => instance;

	internal IEnumerator itemNotificationHandler;
	internal ConcurrentQueue<ItemNotification> itemNotifications;
	internal readonly float itemNotificationDelay = 3f;

	private void Awake()
	{
		instance = this;
		itemNotifications = new ConcurrentQueue<ItemNotification>();
		itemNotificationHandler = ItemNotificationHandler();
	}

	private void OnEnable()
	{
		Logger.Log("Item randomizer started!");
	}

	private void Update()
	{
		Archipelago.Instance.Update();
	}

	public void ReceivedItem(string itemName, string location, int playerSlot)
	{
		string playerName = Archipelago.Instance.Session.Players.GetPlayerName(playerSlot);
		Sprite icon;
		string message;

		if (!IC.Predefined.TryGetItem(itemName, out IC.Item item))
		{
			Logger.LogError($"Received unknown item {itemName} from {playerName}");
			icon = IC.ItemIcons.Get("Unknown");
			message = $"You got an unknown item: {itemName}. Please report!";
			IC.CornerPopup.Show(icon, message);
			return;
		}

		Logger.Log($"Received {itemName} from {playerName}");
		itemNotifications.Enqueue(new ItemNotification(itemName, location, playerSlot, item));

		GameSave.currentSave.IncreaseCountKey("AP_ItemsReceived");
		item?.Trigger();

		GameSave.SaveGameState();
	}

	private IEnumerator ItemNotificationHandler()
	{
		while (Archipelago.Instance.IsConnected())
		{
			if (!itemNotifications.TryPeek(out ItemNotification itemNotification))
			{
				yield return true;
				continue;
			}
			// Add delay between each item notification received so player has time to read the notifications (only needed if Fast Items is on)
			if (Archipelago.Instance.apConfig.ReceiveItemsFast)
			{
				float timePreDelay = Time.time;
				while (Time.time - timePreDelay < (itemNotifications.Count < 5 ? itemNotificationDelay : 1))
				{
					yield return null;
				}
			}
			int playerSlot = itemNotification.PlayerSlot;
			IC.Item item = itemNotification.Item;
			string location = itemNotification.Location;
			string itemName = itemNotification.ItemName;

			bool receivedFromSelf = playerSlot == Archipelago.Instance.CurrentPlayer.Slot;
			string playerName = Archipelago.Instance.Session.Players.GetPlayerName(playerSlot);
			Sprite icon = IC.ItemIcons.Get(item.Icon);
			string message = $"You got {item.DisplayName}";

			if (!receivedFromSelf)
			{
				message += $" from {playerName}!";
			}

			IC.CornerPopup.Show(icon, message);
			// Update recent items display
			IC.TrackerLogEntry logEntry = new IC.TrackerLogEntry()
			{
				LocationName = location,
				ItemName = itemName,
				ItemDisplayName = receivedFromSelf ? item.DisplayName : item.DisplayName + $" from {playerName}",
				ItemIcon = item.Icon
			};
			icSaveData.AddToTrackerLog(logEntry);
			itemNotifications.TryDequeue(out _);
		}
	}


	private void PickedUpItem(DDItem item)
	{
		GameSave.currentSave.SetKeyState($"AP_PickedUp-{item.Location}", true, true);
		Archipelago.Instance.GetAPSaveData().AddCheckedLocation(item.Location);
		Archipelago.Instance.SendLocationChecked(item.Location);
	}

	private void PlaceItems()
	{
		icSaveData = IC.SaveData.Open();

		if (icSaveData.UnnamedPlacements.Count > 0)
		{
			return; // ItemChanger Placements already exist
		}

		// Get ItemPlacements from scouted placements
		foreach (ItemPlacement itemPlacement in Archipelago.Instance.ScoutedPlacements)
		{
			// Get predefined item to use its icon
			IC.Predefined.TryGetItem(itemPlacement.Item, out IC.Item predefinedItem);

			// Don't use predefined icon for weapons because it errors at this point, since player doesn't exist yet,
			// so it doesn't have a way to reference WeaponSwitcher to determine upgrade level
			string icon = itemPlacement.IsForAnotherPlayer
				? "AP"
				: (itemPlacement.Item is "Bomb" or "Fire" or "Hookshot")
				? itemPlacement.Item
				: predefinedItem?.Icon ?? "Unknown";

			// Change name of item so it displays receiving player's name in message
			string itemName = itemPlacement.IsForAnotherPlayer
				? $"{itemPlacement.Item} for {itemPlacement.ForPlayer}"
				: itemPlacement.Item;

			// Place item. Using custom item here so we can override the Trigger() method
			// for knowing when an item was picked up to send it to server
			IC.Item item = new DDItem(itemName, icon, itemPlacement.Location, itemPlacement.IsForAnotherPlayer);
			icSaveData.Place(item, itemPlacement.Location);

			Logger.Log($"Placed {itemPlacement.Item} for {itemPlacement.ForPlayer} at {itemPlacement.Location}");
		}

		// Determine starting weapon
		long startWeapon = Archipelago.Instance.GetSlotData<long>("start_weapon"); // TODO: handle this with starting inventory?
		string startingWeaponId = startWeapon switch
		{
			1 => "daggers",
			2 => "umbrella",
			3 => "sword_heavy",
			4 => "hammer",
			_ => "sword"
		};
		icSaveData.StartingWeapon = startingWeaponId;

		GameSave saveFile = TitleScreen.instance.saveMenu.saveSlots[TitleScreen.instance.saveMenu.index].saveFile;
		saveFile.weaponId = icSaveData.StartingWeapon;
		if (Archipelago.Instance.GetSlotData<long>("start_day_or_night") == 1)
		{
			Logger.Log("Should be Night, but now on the save file");
			LightNight.nightTime = true;
			saveFile.SetNightState(true);
		}

		// Save, since ItemChanger doesn't do it for us due to when we run this method
		saveFile.Save();
	}

	public struct ItemPlacement(string item, string location, string forPlayer, bool isForAnotherPlayer)
	{
		public string Item { get; private set; } = item;
		public string Location { get; private set; } = location;
		public string ForPlayer { get; private set; } = forPlayer;
		public bool IsForAnotherPlayer { get; private set; } = isForAnotherPlayer;
	}

	private readonly struct DDItem : IC.Item
	{
		public string DisplayName { get; }
		public string Icon { get; }
		public string Location { get; }
		public bool IsForAnotherPlayer { get; }

		public void Trigger()
		{
			Instance.PickedUpItem(this);
		}

		public DDItem(string displayName, string icon, string location, bool isForAnotherPlayer)
		{
			DisplayName = displayName;
			Icon = icon;
			Location = location;
			IsForAnotherPlayer = isForAnotherPlayer;
		}
	}

	internal struct ItemNotification(string itemName, string location, int playerSlot, IC.Item item)
	{
		internal string ItemName = itemName;
		internal string Location = location;
		internal int PlayerSlot = playerSlot;
		internal IC.Item Item = item;
	}

	[HarmonyPatch]
	private class Patches
	{
		// / <summary>
		// / Sends completion for main ending
		// / </summary>
		// I think this will work?
		[HarmonyPrefix, HarmonyPatch(typeof(LodBoss2), nameof(LodBoss2.NextPhase))]
		private static void EndGameCsPatch(LodBoss2 __instance)
		{
			if (__instance.deathCutscene)
			{
				Archipelago.Instance.SendCompletion();
			}
		}

		[HarmonyPrefix, HarmonyPatch(typeof(SaveSlot), nameof(SaveSlot.LoadSave))]
		[HarmonyAfter("deathsdoor.itemchanger")] // Needs to go after ItemChanger has loaded its save
		private static void LoadFilePatch()
		{
			if (Archipelago.Instance.IsConnected())
			{
				instance.PlaceItems();
			}
		}

		/// <summary>
		/// Hooks ItemChanger's CornerPopup to suppress local item notifications for us
		/// </summary>
		[HarmonyPrefix, HarmonyPatch(typeof(IC.CornerPopup), nameof(IC.CornerPopup.Show), [typeof(IC.Item)])]
		private static bool ShowPatch(IC.Item x)
		{
			IC.LoggedItem loggedItem = (IC.LoggedItem)x;
			if (loggedItem.Item.GetType() == typeof(DDItem))
			{
				DDItem dDItem = (DDItem)loggedItem.Item;
				return dDItem.IsForAnotherPlayer; // if for this player, skip the notification
			}
			return true;
		}

		/// <summary>
		/// Hooks ItemChanger's LoggedItem Trigger to suppress local items being shown in the tracker log for us
		/// </summary>
		[HarmonyPrefix, HarmonyPatch(typeof(IC.LoggedItem), nameof(IC.LoggedItem.Trigger))]
		private static bool TriggerPatch(IC.LoggedItem __instance)
		{
			if (__instance.Item.GetType() == typeof(DDItem))
			{
				DDItem dDItem = (DDItem)__instance.Item;
				if (dDItem.IsForAnotherPlayer)
				{
					return true;
				}
				else // if for this player, skip the notification
				{
					dDItem.Trigger();
					return false;
				}
			}
			return true;
		}

	}
}