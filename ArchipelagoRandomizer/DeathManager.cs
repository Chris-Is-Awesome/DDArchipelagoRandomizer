using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DDoor.ArchipelagoRandomizer;

[HarmonyPatch]
internal class DeathManager : MonoBehaviour
{
	private static DeathManager instance;

	private DeathLinkService deathLinkService;
	private string playerName;
	private readonly List<string> deathMessages =
	[
		"{name}'s last feather has fallen...",
		"{name} lost their soul.",
		"{name} reaped their last soul...",
		"{name} drank too much squid soup.",
	];

	/// <summary>Deaths received this long after any death (yours or a received one) are ignored</summary>
	private const float deathCooldown = 10f;
	/// <summary>The game can call Die more than once for one death; only send one DeathLink per death</summary>
	private const float sendCooldown = 3f;

	// Received DeathLinks arrive on the connection's thread, not Unity's main thread. Doing anything with the game
	// from there corrupts its state and crashes it, so the handler only stores the death and Update applies it.
	private readonly object pendingLock = new();
	private DeathLink pendingDeath;

	private bool killingFromDeathLink;
	private DeathLink countedDeathLink;

	// Counters for DeathLink Amnesty and Death Wish, kept in the save so they survive quitting
	private const string OwnDeathsKey = "AP_DeathLinkOwnDeaths";
	private const string ReceivedKey = "AP_DeathLinkReceived";

	private static int DeathLinkAmnesty => SlotNumber("death_link_amnesty", 0);
	private static int DeathWish => Math.Max(1, SlotNumber("death_wish", 1));

	private static int SlotNumber(string key, int fallback)
	{
		// Seeds from apworlds before 0.5.0 don't have these
		return Archipelago.Instance.TryGetSlotData(key, out object value) && value != null ? Convert.ToInt32(value) : fallback;
	}

	private static int AddToCounter(string key, int amount)
	{
		GameSave save = GameSave.GetSaveData();
		if (save == null)
		{
			return 0;
		}
		int value = Math.Max(0, save.GetCountKey(key) + amount);
		save.SetCountKey(key, value);
		save.Save();
		return value;
	}
	private float lastDeathTime = float.NegativeInfinity;
	private float lastSendTime = float.NegativeInfinity;
	private bool subscribed;

	public static DeathManager Instance => instance;
	public static bool IsDeathLinkEnabled { get; private set; }

	private void Awake()
	{
		instance = this;
		if (Archipelago.Instance.apConfig.DeathLinkEnabled)
		{
			playerName = Archipelago.Instance.CurrentPlayer.Name;
			deathLinkService = Archipelago.Instance.Session.CreateDeathLinkService();
			deathLinkService.EnableDeathLink();
			deathLinkService.OnDeathLinkReceived += OnDeathReceived;
			Archipelago.OnDisconnected += Unsubscribe;
			subscribed = true;
			IsDeathLinkEnabled = true;
			Logger.Log("DeathLink is on.");
		}
		else
		{
			IsDeathLinkEnabled = false;
			enabled = false;
		}
	}

	private void OnDestroy()
	{
		Unsubscribe();
		if (instance == this)
		{
			instance = null;
		}
	}

	private void Unsubscribe()
	{
		IsDeathLinkEnabled = false;
		if (!subscribed)
		{
			return;
		}
		subscribed = false;
		deathLinkService.OnDeathLinkReceived -= OnDeathReceived;
		Archipelago.OnDisconnected -= Unsubscribe;
	}

	/// <summary>
	/// Runs on the connection's thread
	/// </summary>
	private void OnDeathReceived(DeathLink deathLink)
	{
		lock (pendingLock)
		{
			pendingDeath = deathLink;
		}
	}

	private void Update()
	{
		DeathLink deathLink;
		lock (pendingLock)
		{
			deathLink = pendingDeath;
		}
		if (deathLink == null || !IsDeathLinkEnabled)
		{
			return;
		}

		PlayerGlobal player = PlayerGlobal.instance;
		if (player == null)
		{
			// Title screen or loading; apply it once the player is back
			return;
		}

		string cause = string.IsNullOrEmpty(deathLink.Cause) ? $"{deathLink.Source} died." : deathLink.Cause;
		if (!player.IsAlive() || Time.realtimeSinceStartup - lastDeathTime < deathCooldown)
		{
			// Already dead or just died: don't kill again after respawning
			Logger.Log($"Ignored DeathLink from {deathLink.Source} ({cause}): you just died.");
			ClearPending(deathLink);
			return;
		}

		// Death Wish (YAML): only every Nth DeathLink kills
		if (countedDeathLink != deathLink)
		{
			countedDeathLink = deathLink;
			int wish = DeathWish;
			if (wish > 1)
			{
				int received = AddToCounter(ReceivedKey, 1);
				if (received < wish)
				{
					Logger.Log($"DeathLink from {deathLink.Source} ({cause}): {received}/{wish} for Death Wish, not dying yet.");
					UIManager.Instance.ShowNotification($"DeathLink: {cause} ({received}/{wish})");
					ClearPending(deathLink);
					return;
				}
				AddToCounter(ReceivedKey, -received);
			}
		}

		DamageablePlayer damageable = player.GetComponent<DamageablePlayer>();
		if (damageable == null || !CanDie(player, damageable))
		{
			// In a cutscene, menu, roll or invulnerability window: try again next frame
			return;
		}

		Logger.Log($"Received DeathLink from {deathLink.Source}: {cause}");
		killingFromDeathLink = true;
		try
		{
			// Apply damage instead of calling Die so the game runs its normal death sequence
			damageable.SetHealth(1);
			damageable.ReceiveDamage(1, 0, player.transform.position, player.transform.position, Damageable.DamageType.Hole, 1);
		}
		catch (Exception ex)
		{
			Logger.LogError($"Couldn't apply DeathLink: {ex}");
		}
		finally
		{
			killingFromDeathLink = false;
		}

		if (!player.IsAlive())
		{
			ClearPending(deathLink);
			UIManager.Instance.ShowNotification($"DeathLink: {cause}");
		}
		// Otherwise the game ignored the damage this frame; it is still pending, so try again next frame
	}

	private void ClearPending(DeathLink deathLink)
	{
		lock (pendingLock)
		{
			// Keep a death that arrived in the meantime
			if (pendingDeath == deathLink)
			{
				pendingDeath = null;
			}
		}
	}

	private static bool CanDie(PlayerGlobal player, DamageablePlayer damageable)
	{
		return player.IsAlive() && !player.InputPaused() && !player.DidJustRespawn() && !damageable.IsInvul();
	}

	private void OnPlayerDied()
	{
		float now = Time.realtimeSinceStartup;
		bool fromDeathLink = killingFromDeathLink;
		lastDeathTime = now;

		// A death that comes in while you are dead or respawning belongs to this death
		lock (pendingLock)
		{
			pendingDeath = null;
		}

		if (fromDeathLink || now - lastSendTime < sendCooldown)
		{
			return;
		}
		lastSendTime = now;

		// DeathLink Amnesty (YAML): the first N of your deaths after each sent one are forgiven
		int amnesty = DeathLinkAmnesty;
		if (amnesty > 0)
		{
			int ownDeaths = AddToCounter(OwnDeathsKey, 1);
			if (ownDeaths <= amnesty)
			{
				Logger.Log($"You died: forgiven by DeathLink Amnesty ({ownDeaths}/{amnesty}), not sending.");
				UIManager.Instance.ShowNotification($"DeathLink Amnesty: death {ownDeaths}/{amnesty + 1}, not sent");
				return;
			}
			AddToCounter(OwnDeathsKey, -ownDeaths);
		}

		string message = deathMessages[UnityEngine.Random.Range(0, deathMessages.Count)];
		string cause = message.Replace("{name}", playerName);
		Logger.Log($"You died! Sending DeathLink: {cause}");
		DeathLinkService service = deathLinkService;
		string source = playerName;
		// Sending waits for the connection, so don't do it on the game's thread
		Task.Run(() =>
		{
			try
			{
				service.SendDeathLink(new DeathLink(source, cause));
			}
			catch (Exception ex)
			{
				Logger.LogError($"Couldn't send DeathLink: {ex.Message}");
			}
		});
	}

	[HarmonyPostfix, HarmonyPatch(typeof(DamageablePlayer), nameof(DamageablePlayer.Die))]
	public static void DeathLinkSendDeathPatch()
	{
		// Never let an error here break the game's own death handling
		try
		{
			if (IsDeathLinkEnabled && instance != null)
			{
				instance.OnPlayerDied();
			}
		}
		catch (Exception ex)
		{
			Logger.LogError($"DeathLink error on death: {ex}");
		}
	}
}
