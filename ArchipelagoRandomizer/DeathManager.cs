using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DDoor.ArchipelagoRandomizer;

[HarmonyPatch]
internal class DeathManager : MonoBehaviour
{
	private static DeathManager instance;

	private DeathLinkService deathLinkService;
	private DamageablePlayer damageable;
	private readonly List<string> deathMessages =
	[
		"{name}'s last feather has fallen...",
		"{name} lost their soul.",
		"{name} reaped their last soul...",
		"{name} drank too much squid soup.",
	];
	private const float deathCooldown = 10f;
	private bool diedFromDeathLink;
	private bool preventDeath;

	public static DeathManager Instance => instance;
	public static bool IsDeathLinkEnabled { get; private set; }

	private void Awake()
	{
		instance = this;
		if (Archipelago.Instance.apConfig.DeathLinkEnabled)
		{
			deathLinkService = Archipelago.Instance.Session.CreateDeathLinkService();
			enabled = true;
			IsDeathLinkEnabled = true;
		}
		else
		{
			enabled = false;
			IsDeathLinkEnabled = false;
		}
	}

	private void OnEnable()
	{
		deathLinkService.EnableDeathLink();
		deathLinkService.OnDeathLinkReceived += OnDeathReceived;
		Archipelago.OnDisconnected += OnDisable;
		IsDeathLinkEnabled = true;
	}

	private void OnDisable()
	{
		if (!IsDeathLinkEnabled)
		{
			return;
		}

		deathLinkService.OnDeathLinkReceived -= OnDeathReceived;
		Archipelago.OnDisconnected -= OnDisable;
		IsDeathLinkEnabled = false;
	}

	private static void SendDeath()
	{
		if (instance.diedFromDeathLink)
		{
			instance.diedFromDeathLink = false;
			return;
		}

		Logger.Log("You died! Sending death to everyone...");
		string player = Archipelago.Instance.CurrentPlayer.Name;
		string message = instance.deathMessages[Random.Range(0, instance.deathMessages.Count - 1)];
		string cause = message.Replace("{name}", player);
		Logger.Log(cause);
		instance.deathLinkService.SendDeathLink(new DeathLink(player, cause));
	}

	private void OnDeathReceived(DeathLink deathLink)
	{
		if (!preventDeath)
		{
			StartCoroutine(DoDeathReceived(deathLink));
		}
	}

	private IEnumerator DoDeathReceived(DeathLink deathLink)
	{
		Logger.Log("Received death! You will die when next possible...");
		while (!PlayerGlobal.instance)
		{
			// if Player doesn't exist yet
			yield return null;
		}
		PlayerGlobal player = PlayerGlobal.instance;
		preventDeath = true;

		if (damageable == null)
		{
			damageable = player.GetComponent<DamageablePlayer>();
		}

		while (!CanDie())
		{
			yield return null;
		}

		diedFromDeathLink = true;

		// Apply damage instead of calling Die method to avoid issues
		damageable.SetHealth(1f);
		damageable.ReceiveDamage(10f, 0, player.transform.position, player.transform.position, Damageable.DamageType.Hole, 1);
		StartCoroutine(DeathCooldownTimer());
	}

	private IEnumerator DeathCooldownTimer()
	{
		yield return new WaitForSecondsRealtime(deathCooldown);
		preventDeath = false;
	}

	private bool CanDie()
	{
		return damageable != null && !PlayerGlobal.instance.InputPaused() && PlayerGlobal.instance.IsAlive();
	}

	[HarmonyPostfix, HarmonyPatch(typeof(DamageablePlayer), nameof(DamageablePlayer.Die))]
	public static void DeathLinkSendDeathPatch()
	{
		if (IsDeathLinkEnabled)
		{
			SendDeath();
		}
	}
}