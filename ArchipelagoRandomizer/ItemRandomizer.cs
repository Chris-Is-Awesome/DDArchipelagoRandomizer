using HarmonyLib;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
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
	private int? soulMultiplier = null;

	private int SoulMultiplier
	{
		get
		{
			// Initialize value
			if (!soulMultiplier.HasValue)
			{
				// Ensure multiplier never goes below 1
				soulMultiplier = Mathf.Max(1, (int)Archipelago.Instance.GetSlotData<long>("soul_multiplier"));
			}

			return soulMultiplier.Value;
		}
	}

	private bool newGame = false;

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

		IC.Item item;

		if (TrapManager.Instance.traps.ContainsKey(itemName))
		{
			TrapManager.Instance.trapQueue.Enqueue(TrapManager.Instance.traps[itemName]);
			Logger.Log($"Received {itemName} from {playerName}");
			IC.CountableInventoryItem tempItem = new IC.CountableInventoryItem
			{
				UniqueId = itemName,
				CountId = itemName,
				DisplayName = itemName,
				Icon = "Unknown"
			};
			item = tempItem;
		}
		else if (!IC.Predefined.TryGetItem(itemName, out item))
		{
			Logger.LogError($"Received unknown item {itemName} from {playerName}");
			icon = IC.ItemIcons.Get("Unknown");
			message = $"You got an unknown item: {itemName}. Please report!";
			IC.CornerPopup.Show(icon, message);
			return;
		}

		string modifiedItemName = ModifyItemName(itemName);
		Logger.Log($"Received {modifiedItemName} from {playerName}");
		itemNotifications.Enqueue(new ItemNotification(modifiedItemName, location, playerSlot, item));

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
			int playerSlot = itemNotification.PlayerSlot;
			IC.Item item = itemNotification.Item;
			string location = itemNotification.Location;
			string itemName = itemNotification.ItemName;

			bool receivedFromSelf = playerSlot == Archipelago.Instance.CurrentPlayer.Slot;
			string playerName = Archipelago.Instance.Session.Players.GetPlayerName(playerSlot);
			Sprite icon = IC.ItemIcons.Get(item.Icon);
			string message = $"You got {itemName}";

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
				ItemDisplayName = receivedFromSelf ? itemName : itemName + $" from {playerName}",
				ItemIcon = item.Icon
			};
			icSaveData.AddToTrackerLog(logEntry);
			itemNotifications.TryDequeue(out _);

			// Add delay between each item notification received so player has time to read the notifications (only needed if Fast Items is on)
			if (Archipelago.Instance.apConfig.ReceiveItemsFast)
			{
				float timePreDelay = Time.time;
				while (Time.time - timePreDelay < (itemNotifications.Count < 5 ? itemNotificationDelay : 1))
				{
					yield return null;
				}
			}
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
			newGame = false;
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
		if (Archipelago.Instance.ScoutedPlacements.Count(placement => placement.Location == "Grove of Spirits Door") == 0)
		{
			// If the Grove of Spirits Door is not placed, some of the cutscene skip work does not function correctly
			// Specifically, activating the shop means the Demonic Forest Spirit will never spawn
			// Place the Grove of Spirits Door at its vanilla location in order to have IC use its alternate system
			IC.Predefined.TryGetItem("Grove of Spirits Door", out IC.Item predefinedItem);
			icSaveData.Place(predefinedItem, "Grove of Spirits Door");
		}

		if (GoalModifications.Instance.IsGreenTabletGoal())
		{
			// Place the goal item at Green Tablet if Green Tablet is a possible goal
			IC.Predefined.TryGetItem("Green Ancient Tablet of Knowledge", out IC.Item predefinedItem);
			IC.Item item = new GoalModifications.GoalItem("Goal", predefinedItem?.Icon ?? "Unknown", "Green Ancient Tablet of Knowledge");
			icSaveData.Place(item, "Green Ancient Tablet of Knowledge");
			Logger.Log($"Placed goal item at Green Ancient Tablet of Knowledge");
		}

		if (GoalModifications.Instance.IsLordOfDoorsGoal())
		{
			// Place a goal item at Rusty Belltower Key if Lord of Doors is a possible goal so that the belltower is accessible
			IC.Predefined.TryGetItem("Rusty Belltower Key", out IC.Item predefinedItem);
			IC.Item item = new GoalModifications.GoalItem("Goal", predefinedItem?.Icon ?? "Unknown", "Rusty Belltower Key");
			icSaveData.Place(item, "Rusty Belltower Key");
			Logger.Log($"Placed goal item at Rusty Belltower Key");
		}

		// Determine starting weapon
		long startWeapon = Archipelago.Instance.GetSlotData<long>("start_weapon");
		string startingWeaponId = startWeapon switch
		{
			1 => "daggers",
			2 => "hammer",
			3 => "sword_heavy",
			4 => "umbrella",
			_ => "sword"
		};
		icSaveData.StartingWeapon = startingWeaponId;

		GameSave saveFile = TitleScreen.instance.saveMenu.saveSlots[TitleScreen.instance.saveMenu.index].saveFile;
		saveFile.weaponId = icSaveData.StartingWeapon;

		// Day or night setting
		if (Archipelago.Instance.GetSlotData<long>("start_day_or_night") == 1)
		{
			Logger.Log("Should be Night, but now on the save file");
			LightNight.nightTime = true;
			saveFile.SetNightState(true);
		}

		// Life seed required count
		long lifeSeedCount = Archipelago.Instance.GetSlotData<long>("plant_pot_number");
		if (lifeSeedCount == 0)
		{
			lifeSeedCount = 50; // 0 is not a possible yaml option, so the slot data must be missing the number. Original default was 50.
		}
		icSaveData.GreenTabletDoorCost = (int)lifeSeedCount;


		// Save, since ItemChanger doesn't do it for us due to when we run this method
		saveFile.Save();
		newGame = true;
	}

	private string ModifyItemName(string itemName)
	{
		switch (itemName)
		{
			case "100 Souls":
				int amount = int.Parse(itemName.Split(' ')[0]) * SoulMultiplier;
				return $"{amount} Souls";
		}

		// Nothing to modify
		return itemName;
	}

	internal void QueueTriggerGroveOfSpiritsDoorCheck()
	{
		SceneManager.sceneLoaded += TriggerGroveofSpiritsDoorCheck;
	}

	internal void TriggerGroveofSpiritsDoorCheck(Scene scene, LoadSceneMode _)
	{
		if (scene.name == "lvl_HallOfDoors")
		{
			Logger.Log("Triggering GoS door check");
			if (icSaveData.UnnamedPlacements.ContainsKey("Grove of Spirits Door"))
			{
				icSaveData.UnnamedPlacements["Grove of Spirits Door"].Trigger();
			}
			SceneManager.sceneLoaded -= TriggerGroveofSpiritsDoorCheck;
		}
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
		[HarmonyPrefix, HarmonyPatch(typeof(Inventory), nameof(Inventory.GainSoul))]
		private static void GainSoul_CurrencyMultiplierPatch(ref int count)
		{
			if (!Archipelago.Instance.IsConnected())
			{
				return;
			}

			count *= Instance.SoulMultiplier;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), [typeof(InventoryItem), typeof(int)])]
		private static void AddItem_CurrencyMultiplierPatch(InventoryItem i, ref int quantity)
		{
			if (!Archipelago.Instance.IsConnected() || i.id != "currency")
			{
				return;
			}

			quantity *= Instance.SoulMultiplier;
		}

		/// <summary>
		/// Place items and do other setup at the start of a new save file
		/// </summary>
		[HarmonyPrefix, HarmonyPatch(typeof(SaveSlot), nameof(SaveSlot.LoadSave))]
		[HarmonyAfter("deathsdoor.itemchanger")] // Needs to go after ItemChanger has loaded its save
		private static void PreLoadFilePatch()
		{
			if (Archipelago.Instance.IsConnected())
			{
				instance.PlaceItems();
			}
		}

		/// <summary>
		/// Increment the amount of souls the file starts with by the amount in slot_data
		/// </summary>
		[HarmonyPostfix, HarmonyPatch(typeof(SaveSlot), nameof(SaveSlot.LoadSave))]
		[HarmonyAfter("deathsdoor.itemchanger")]
		private static void PostLoadFilePatch()
		{
			if (!Archipelago.Instance.IsConnected() || !Instance.newGame)
			{
				return;
			}
			// Set starting souls
			int startingSouls = (int)Archipelago.Instance.GetSlotData<long>("starting_souls");
			if (startingSouls > 0)
			{
				GameSave.currentSave.AddToCountKey("currency", startingSouls);
				GameSave.currentSave.Save();
			}
			Instance.newGame = false;
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

		[HarmonyPrefix, HarmonyPatch(typeof(IC.ItemIcons), nameof(IC.ItemIcons.Get))]
		private static void PreGetPatch(string name)
		{
			string filename = name + ".png";
			if (IC.ItemIcons.sprites.ContainsKey(filename))
			{
				return; // sprite is already pre-loaded
			}
			else
			{
				if (Assembly.GetExecutingAssembly().GetManifestResourceNames().Contains("DDoor.Resources.Item_Changer_Icons." + filename))
				{
					IC.ItemIcons.sprites[filename] = LoadSpriteFromAssembly(filename); // Get around that ItemChanger only takes paths by pre-loading the sprite
				}
			}

			static Sprite LoadSpriteFromAssembly(string name) // Preparing for DD use is borrowed from ItemChanger's ItemIcons.LoadSprite
			{
				Assembly assembly = Assembly.GetExecutingAssembly();
				Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
				using (MemoryStream memstream = new MemoryStream())
				{
					assembly.GetManifestResourceStream("DDoor.Resources.Item_Changer_Icons." + name).CopyTo(memstream);
					tex.LoadImage(memstream.ToArray(), true);
				}
				tex.filterMode = FilterMode.Bilinear;
				return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
			}
		}

		/// <summary>
		/// Replace the item name 100 Souls with the appropriate multiple in dialogue (prefixing ItemChanger)
		/// </summary>
		/// <param name="message">The message that ItemChanger is passing in to replace the NPC's speech</param>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(IC.SpeechPopup), nameof(IC.SpeechPopup.Modify), [typeof(NPCCharacter), typeof(int), typeof(string)])]
		private static void PreModify(ref string message)
		{
			if (message.Contains("100 Souls"))
			{
				int amount = 100 * Instance.SoulMultiplier;
				message = message.Replace("100 Souls", $"{amount} Souls");
			}
		}

	}
}