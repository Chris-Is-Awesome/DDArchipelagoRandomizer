using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DDoor.ArchipelagoRandomizer;

internal class Archipelago
{
	public static event Action OnConnected;
	public static event Action OnDisconnected;
	private static readonly Archipelago instance = new();
	private APSaveData apSaveSlot1Data = APSaveData.Load(1);
	private APSaveData apSaveSlot2Data = APSaveData.Load(2);
	private APSaveData apSaveSlot3Data = APSaveData.Load(3);
	private APSaveData GetAPSaveDataForSlot(int saveIndex) => saveIndex switch
	{
		1 => apSaveSlot1Data,
		2 => apSaveSlot2Data,
		3 => apSaveSlot3Data,
		_ => throw new IndexOutOfRangeException("Invalid save index"),
	};
	private UIManager uiManager;
	private Dictionary<string, object> slotData;
	private IEnumerator checkItemsReceived;
	private IEnumerator incomingItemHandler;
	private IEnumerator outgoingItemHandler;
	private ConcurrentQueue<(ItemInfo item, int index)> incomingItems;
	private ConcurrentQueue<ItemInfo> outgoingItems;
	private int itemIndex;
	private bool isConnected;
	internal bool IsConnected() => isConnected;
	private bool hasCompleted;
	private readonly float itemReceiveDelay = 3f;

	public static Archipelago Instance => instance;
	public ArchipelagoSession Session { get; private set; }
	public PlayerInfo CurrentPlayer
	{
		get
		{
			if (Session == null)
			{
				return null;
			}

			return Session.Players.GetPlayerInfo(Session.ConnectionInfo.Slot);
		}
	}
	public List<ItemRandomizer.ItemPlacement> ScoutedPlacements { get; private set; }

	private Archipelago() { }

	public async Task<LoginSuccessful> Connect(APSaveData apSaveData)
	{
		Session = ArchipelagoSessionFactory.CreateSession(apSaveData.URL, apSaveData.Port);
		string message;
		uiManager = UIManager.Instance;

		LoginResult loginResult = Session.TryConnectAndLogin(
			"Death's Door",
			apSaveData.SlotName,
			ItemsHandlingFlags.AllItems,
			password: apSaveData.Password,
			requestSlotData: true
		);

		switch (loginResult)
		{
			case LoginFailure failure:
				string errors = string.Join(", ", failure.Errors);
				message = $"Failed to connect to Archipelago: {errors}";
				uiManager.ShowNotification(message);
				throw new LoginValidationException(message);
			case LoginSuccessful success:

				if (apSaveData.Seed != "" && apSaveData.Seed != Session.RoomState.Seed)
				{
					// Reject if the saved data has the wrong seed (TODO: possibly other validation?)

					message = $"Failed to connect to Archipelago: Saved data has a different seed than the server. Each new room needs a new save.";
					uiManager.ShowNotification(message);
					throw new LoginValidationException(message);
				}
				await OnSocketOpened(success, apSaveData);
				Logger.Log($"Successfully connected to Archipelago at {apSaveData.URL}:{apSaveData.Port} as {apSaveData.SlotName} on team {success.Team}. Have fun!");
				return success;
			default:
				message = $"Unexpected LoginResult type when connecting to Archipelago: {loginResult}";
				uiManager.ShowNotification(message);
				throw new LoginValidationException(message);
		}
	}

	public void Disconnect()
	{
		if (!isConnected)
		{
			return;
		}

		Session.Socket.DisconnectAsync();
	}

	public void Update()
	{
		if (!isConnected)
		{
			return;
		}

		checkItemsReceived?.MoveNext();

		if (CanPlayerReceiveItems())
		{
			incomingItemHandler?.MoveNext();
			outgoingItemHandler?.MoveNext();
		}
	}

	public void SendLocationChecked(string locationName)
	{
		if (!isConnected)
		{
			return;
		}

		long locationId = Locations.ItemChangerLocationToAPLocationID(locationName);
		Session.Locations.CompleteLocationChecks(locationId);
	}

	public void SendCompletion()
	{
		if (hasCompleted || !isConnected)
		{
			return;
		}

		Logger.Log("Goal completed. Sending completion...");
		StatusUpdatePacket statusUpdatePacket = new StatusUpdatePacket();
		statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
		Session.Socket.SendPacket(statusUpdatePacket);
		hasCompleted = true;
	}

	public APSaveData GetAPSaveData()
	{
		int saveIndex = GetSaveIndex();
		return GetAPSaveDataForSlot(saveIndex);
	}

	public T GetSlotData<T>(string key)
	{
		object value = default(T);

		if (slotData == null || !slotData.TryGetValue(key, out value))
		{
			Logger.LogError($"No slot data with key '{key}' was found, returning null.");
			return (T)value;
		}

		return (T)value;
	}

	private async Task OnSocketOpened(LoginSuccessful loginSuccess, APSaveData apSaveData)
	{
		slotData = loginSuccess.SlotData;
		itemIndex = TitleScreen.instance.saveMenu.saveSlots[TitleScreen.instance.saveMenu.index].saveFile.GetCountKey("AP_ItemsReceived");
		checkItemsReceived = CheckItemsReceieved();
		incomingItemHandler = IncomingItemHandler();
		outgoingItemHandler = OutgoingItemHandler();
		incomingItems = new ConcurrentQueue<(ItemInfo item, int index)>();
		outgoingItems = new ConcurrentQueue<ItemInfo>();
		Session.Socket.SocketClosed += OnSocketClosed;
		apSaveData.Seed = Session.RoomState.Seed;
		apSaveData.Save();

		Logger.Log("Slot data:");
		foreach (KeyValuePair<string, object> kvp in slotData)
		{
			Logger.Log($"     {kvp.Key}: {kvp.Value}");
		}

		await ScoutAllLocations();

		isConnected = true;
		SyncLocationsChecked(apSaveData);
		OnConnected?.Invoke();
	}

	private void OnSocketClosed(string reason)
	{
		isConnected = false;
		incomingItemHandler = null;
		outgoingItemHandler = null;
		incomingItems = new ConcurrentQueue<(ItemInfo item, int index)>();
		outgoingItems = new ConcurrentQueue<ItemInfo>();

		Session.Socket.SocketClosed -= OnSocketClosed;
		Session = null;

		if (PlayerGlobal.instance != null)
		{
			UIManager.Instance.ShowNotification("You were disconnected from Archipelago. Return to title screen to reconnect.\nAny items obtained now will be sent once you reconnect.");
		}

		OnDisconnected?.Invoke();
		Logger.Log("Disconnected from Archipelago!");
	}

	private void SyncLocationsChecked(APSaveData apSaveData)
	{
		// This resync now accounts for collect/same slot co-op/starting a new file
		// Some locations may be checked on server and not checked locally
		// We want to send out any locally checked locations (i.e. if lose connection during play), even if the server has more locations checked than us.
		foreach (string locationName in apSaveData.LocationsChecked)
		{
			long locationId = Locations.ItemChangerLocationToAPLocationID(locationName);
			if (!Session.Locations.AllLocationsChecked.Contains(locationId))
			{
				SendLocationChecked(locationName);
			}
		}
	}

	private IEnumerator CheckItemsReceieved()
	{
		while (isConnected)
		{
			if (Session.Items.Index > itemIndex)
			{
				ItemInfo item = Session.Items.AllItemsReceived[itemIndex];
				incomingItems.Enqueue((item, itemIndex));
				itemIndex++;
				yield return true;
			}
			else
			{
				yield return true;
				continue;
			}
		}
	}

	private IEnumerator IncomingItemHandler()
	{
		while (isConnected)
		{
			if (!incomingItems.TryPeek(out (ItemInfo item, int index) pendingItem))
			{
				yield return true;
				continue;
			}

			// Add delay between each item received so player has time to read the notifications
			float delay = Time.time;
			while (Time.time - delay < (incomingItems.Count < 5 ? itemReceiveDelay : 1))
			{
				yield return null;
			}

			ItemInfo item = pendingItem.item;
			string itemItemChangerName = Items.APItemInfoToDDItemName(item);
			string locationName = Locations.APItemInfoToDDLocationName(item);
			ItemRandomizer.Instance.ReceivedItem(itemItemChangerName, locationName, item.Player);
			incomingItems.TryDequeue(out _);

			yield return true;
		}
	}

	private IEnumerator OutgoingItemHandler()
	{
		while (isConnected)
		{
			if (!outgoingItems.TryDequeue(out ItemInfo item))
			{
				yield return true;
				continue;
			}

			if (item.Player != CurrentPlayer)
			{
				Logger.Log($"Sent {item.ItemDisplayName} at {item.LocationDisplayName} to {item.Player.Name}");
			}

			yield return true;
		}
	}

	private async Task ScoutAllLocations()
	{
		ScoutedPlacements = [.. (await Session.Locations.ScoutLocationsAsync(
			[.. Session.Locations.AllLocations]
		)).Select(kvp => ItemPlacementForAPItemInfo(kvp.Value))];
	}

	private ItemRandomizer.ItemPlacement ItemPlacementForAPItemInfo(ItemInfo itemInfo)
	{
		// Split out for ScoutAllLocations() for debugging
		string item = Items.APItemInfoToDDItemName(itemInfo);
		string location = Locations.APItemInfoToDDLocationName(itemInfo);
		string forPlayer = itemInfo.Player.Name;
		bool isForAnotherPlayer = itemInfo.Player != CurrentPlayer;
		return new ItemRandomizer.ItemPlacement(item, location, forPlayer, isForAnotherPlayer);
	}

	private bool CanPlayerReceiveItems()
	{
		return (
			// !hasCompleted && AP games can continue after goal
			PlayerGlobal.instance != null &&
			!PlayerGlobal.instance.InputPaused() &&
			PlayerGlobal.instance.IsAlive()
		);
	}

	private static string GetAPSaveDataPath(int saveIndex) => $"{Application.persistentDataPath}/SAVEDATA/Save_slot{saveIndex}-Archipelago.json";

	private int GetSaveIndex()
	{
		return TitleScreen.instance != null
			? TitleScreen.instance.saveMenu.index + 1
			: int.Parse(GameSave.currentSave.saveId.Substring(4));
	}

	public void ClearAPSaveSlot(int saveIndex)
	{
		GetAPSaveDataForSlot(saveIndex).Erase();
		switch (saveIndex)
		{
			case 1: apSaveSlot1Data = new(1); break;
			case 2: apSaveSlot2Data = new(2); break;
			case 3: apSaveSlot3Data = new(3); break;
			default: throw new IndexOutOfRangeException("Invalid save index");
		}
	}

#nullable enable
	public class APSaveData
	{
		public string URL { get; set; } = "";
		public int Port { get; set; }
		public string SlotName { get; set; } = "";
		public string Password { get; set; } = "";
    public int SaveSlotIndex { get; set; }
		public string Seed { get; set; } = "";
		public List<string> LocationsChecked { get; } = [];

		public APSaveData(int saveIndex)
		{
			SaveSlotIndex = saveIndex;
		}

		public void Save()
		{
			string path = GetAPSaveDataPath(SaveSlotIndex);

			// Create file
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, json);
			Logger.Log($"Saved AP save data file at: {path}");
		}

		public static APSaveData Load(int saveIndex)
		{
			string path = GetAPSaveDataPath(saveIndex);

			APSaveData? apSaveData = null;

			// Create file
			if (File.Exists(path))
			{
				string json = File.ReadAllText(path);
				apSaveData = JsonConvert.DeserializeObject<APSaveData>(json);
			}
			apSaveData ??= new APSaveData(saveIndex);
			return apSaveData;
		}

		public void Erase()
		{
			string path = GetAPSaveDataPath(SaveSlotIndex);
			if (File.Exists(path)) { File.Delete(path); }
		}

		public void AddCheckedLocation(string location)
		{
			LocationsChecked.Add(location);
			Save();
		}
	}
#nullable disable

	[HarmonyPatch]
	private class Patches
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(SaveSlot), nameof(SaveSlot.EraseSave))]
		private static void Postfix(SaveSlot __instance)
		{
			Instance.ClearAPSaveSlot(int.Parse(__instance.saveId.Substring(__instance.saveId.Length-1)));
		}
	}
}