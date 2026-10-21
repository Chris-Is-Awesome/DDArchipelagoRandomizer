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
