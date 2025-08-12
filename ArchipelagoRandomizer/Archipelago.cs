using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
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
	private readonly string apSaveDataPath = $"{Application.persistentDataPath}/SAVEDATA/Save_slot#-Archipelago.json";
	private APSaveData apSaveData;
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

		long locationId = Locations.locationData.First(entry => entry.itemChangerName == locationName).apLocationId;
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

		if (apSaveData == null)
		{
			string path = apSaveDataPath.Replace("#", (saveIndex).ToString());
			if (File.Exists(path))
			{
				string json = File.ReadAllText(path);
				apSaveData = JsonConvert.DeserializeObject<APSaveData>(json);
			}
			else
			{
				apSaveData = new APSaveData();
				apSaveData.CreateEmptySave();
			}	
		}

		if (apSaveData == null)
		{
			Logger.LogError($"Failed to find AP connection apSaveData save file for slot {saveIndex}:");
		}

		return apSaveData;
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
		itemIndex = TitleScreen.instance.saveMenu.saveSlots[TitleScreen.instance.index].saveFile.GetCountKey("AP_ItemsReceived");
		checkItemsReceived = CheckItemsReceieved();
		incomingItemHandler = IncomingItemHandler();
		outgoingItemHandler = OutgoingItemHandler();
		incomingItems = new ConcurrentQueue<(ItemInfo item, int index)>();
		outgoingItems = new ConcurrentQueue<ItemInfo>();
		Session.Socket.SocketClosed += OnSocketClosed;
		SaveAPData(apSaveData);

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
		int locationsCheckedOnServer = Session.Locations.AllLocationsChecked.Count;
		int locationsCheckedOnSave = apSaveData.LocationsChecked.Count;

		if (locationsCheckedOnServer < locationsCheckedOnSave)
		{
			for (int i = locationsCheckedOnServer; i < locationsCheckedOnSave; i++)
			{
				string checkedLocation = apSaveData.LocationsChecked[i];
				SendLocationChecked(checkedLocation);
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
			string itemChangerName = Items.itemData.First(entry => entry.apItemId == item.ItemId).itemChangerName;
			string locationName = "";
			if (item.LocationGame == "Death's Door")
			{
				locationName = Locations.locationData.First(entry => entry.apLocationId == item.LocationId).itemChangerName;
			}
			else
			{
				locationName = item.LocationDisplayName;
			}
			ItemRandomizer.Instance.ReceivedItem(itemChangerName, locationName, item.Player);
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
				Logger.Log($"Sent {item.ItemName} at {item.LocationDisplayName} to {item.Player.Name}");
			}

			yield return true;
		}
	}

	private void SaveAPData(APSaveData data)
	{
		string path = GetAPSaveDataPath();

		// Create file
		if (!File.Exists(path))
		{
			string json = JsonConvert.SerializeObject(data, Formatting.Indented);
			File.WriteAllText(path, json);
			Logger.Log($"Created AP save data file at: {path}");
		}
		// Update connection info
		else
		{
			apSaveData.UpdateConnectionInfo(data);

			if (Session.Locations.AllLocationsChecked.Count < 1)
			{
				apSaveData.ClearLocationsChecked();
			}
			else if (data.LocationsChecked.Count < 1)
			{
				foreach (long checkedLocationId in Session.Locations.AllLocationsChecked)
				{
					string checkedLocationName = Session.Locations.GetLocationNameFromId(checkedLocationId);
					apSaveData.AddCheckedLocation(checkedLocationName);
				}
			}

			Logger.Log($"Updated AP save data file at: {path}");
		}

		apSaveData = data;
	}

	private async Task ScoutAllLocations()
	{
		ScoutedPlacements = [.. (await Session.Locations.ScoutLocationsAsync(
			Session.Locations.AllLocations.ToArray()
		)).Select(kvp => new ItemRandomizer.ItemPlacement(
			APItemNameToDDItemName(kvp.Value),
			Locations.locationData.First(entry => entry.apLocationId == kvp.Value.LocationId).itemChangerName,
			kvp.Value.Player.Name,
			kvp.Value.Player != CurrentPlayer
		))];
	}

	private string APItemNameToDDItemName(ScoutedItemInfo scoutedItemInfo)
	{
		if (scoutedItemInfo.ItemGame == "Death's Door")
		{
			return Items.itemData.First(entry => entry.apItemId == scoutedItemInfo.ItemId).itemChangerName;
		}
		else
		{
			return scoutedItemInfo.ItemDisplayName;
		}
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

	private string GetAPSaveDataPath()
	{
		return apSaveDataPath.Replace("#", GetSaveIndex().ToString());
	}

	private int GetSaveIndex()
	{
		return TitleScreen.instance != null
			? TitleScreen.instance.saveMenu.index + 1
			: int.Parse(GameSave.currentSave.saveId.Substring(4));
	}

	public class APSaveData
	{
		public string URL { get; set; }
		public int Port { get; set; }
		public string SlotName { get; set; }
		public string Password { get; set; }
		public List<string> LocationsChecked
		{
			get
			{
				JObject jObject = GetJSONObject();
				return jObject[nameof(LocationsChecked)]?.ToObject<List<string>>();
			}
		}

		public void CreateEmptySave()
		{
			JObject jObject = [];
			jObject[nameof(LocationsChecked)] = new JArray();
			UpdateJSON(jObject);
		}

		public void UpdateConnectionInfo(APSaveData data)
		{
			JObject jObject = GetJSONObject();

			if (jObject.Value<string>(nameof(URL)) != data.URL)
			{
				jObject[nameof(URL)] = data.URL;
			}

			if (jObject.Value<int>(nameof(Port)) != data.Port)
			{
				jObject[nameof(Port)] = data.Port;
			}

			if (jObject.Value<string>(nameof(SlotName)) != data.SlotName)
			{
				jObject[nameof(SlotName)] = data.SlotName;
			}

			if (jObject.Value<string>(nameof(Password)) != data.Password)
			{
				jObject[nameof(Password)] = data.Password;
			}

			UpdateJSON(jObject);
		}

		public void AddCheckedLocation(string location)
		{
			JObject jObject = GetJSONObject();
			JArray jArray = (JArray)jObject[nameof(LocationsChecked)];
			jArray.Add(location);
			UpdateJSON(jObject);
		}

		public void ClearLocationsChecked()
		{
			LocationsChecked.Clear();
			JObject jObject = GetJSONObject();
			jObject[nameof(LocationsChecked)] = new JArray();
			UpdateJSON(jObject);
		}

		private void UpdateJSON(JObject jObject)
		{
			using StreamWriter sw = new StreamWriter(instance.GetAPSaveDataPath());
			using (JsonTextWriter jtw = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
			{
				jObject.WriteTo(jtw);
			}
		}

		private JObject GetJSONObject()
		{
			string apSaveDataPath = instance.GetAPSaveDataPath();
			string json = File.ReadAllText(apSaveDataPath);
			return JObject.Parse(json);
		}
	}
}