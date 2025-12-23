using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Packets;
using DDoor.AddUIToOptionsMenu;
using IC = DDoor.ItemChanger;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DDoor.ArchipelagoRandomizer;

public class TrapManager : MonoBehaviour
{
	private static TrapManager instance;
	public static TrapManager Instance => instance;

	public Dictionary<string, TrapType> traps = new() {
		{"Rotation Trap", TrapType.Rotation },
		{"Player Invisibility Trap", TrapType.PlayerInvisibility },
		{"Enemy Invisibility Trap", TrapType.EnemyInvisibility },
		{"Knockback Trap", TrapType.Knockback },
	};

	public Dictionary<string, TrapType> TrapNameToType = new()
	{
		// List borrowed from Tunic, commented out ones we don't yet have good equivalents for
		{ "Rotation Trap", TrapType.Rotation },
		{"Player Invisibility Trap", TrapType.PlayerInvisibility },
		{"Enemy Invisibility Trap", TrapType.EnemyInvisibility },
		{"Knockback Trap", TrapType.Knockback },
		// { "Ice Trap", TrapType.Ice },
		// {"Freeze Trap", TrapType.Ice },
		// {"Frozen Trap", TrapType.Ice },
		// {"Stun Trap", TrapType.Ice },
		// {"Paralyze Trap", TrapType.Ice },
		// {"Chaos Control Trap", TrapType.Ice },

		// {"Fire Trap", TrapType.Fire },
		// {"Damage Trap", TrapType.Fire },
		// {"Bomb", TrapType.Fire },  // Luigi's Mansion, yes it's just Bomb
		// {"Posession Trap", TrapType.Fire },  // Luigi's Mansion, damage-based trap
		// {"Nut Trap", TrapType.Fire },  // DKC, damage-based trap

		// {"Bee Trap", TrapType.Bee },

		{"Tiny Trap", TrapType.PlayerInvisibility },
		{"Poison Mushroom", TrapType.PlayerInvisibility },  // Luigi's Mansion, makes player smaller

		{"Screen Flip Trap", TrapType.Rotation },
		{"Mirror Trap", TrapType.Rotation },
		{"Reverse Trap", TrapType.Rotation },
		{"Reversal Trap", TrapType.Rotation },

		// {"Deisometric Trap", TrapType.Deisometric },
		// {"Confuse Trap", TrapType.Deisometric },
		// {"Confusion Trap", TrapType.Deisometric },
		// {"Fuzzy Trap", TrapType.Deisometric },
		// {"Confound Trap", TrapType.Deisometric },

		{"Bonk Trap", TrapType.Knockback },
		{"Banana Trap", TrapType.Knockback },
		{"Spring Trap", TrapType.Knockback },

		// {"Zoom Trap", TrapType.Zoom },  // Celeste, zooms camera in

		// {"Bald Trap", TrapType.Bald },  // Celeste, bald
		// {"Whoops! Trap", TrapType.Whoops }, // Here Comes Niko, drops the player from way high up
		// {"W I D E Trap", TrapType.Wide }, // Here Comes Niko, makes the fox W I D E
		// {"Home Trap", TrapType.Home }, // Here Comes Niko, teleports the player "home", overworld in this case
	};

	internal IEnumerator trapHandler;
	internal ConcurrentQueue<TrapType> trapQueue;
	private readonly float trapDelay = 3f;
	private Coroutine enemyInvisCoroutine;
	private readonly float enemyInvisTime = 15f;
	private float enemyInvisTimer = 0f;

	private void Awake()
	{
		instance = this;
		trapQueue = new ConcurrentQueue<TrapType>();
		trapHandler = TrapHandler();
		if (Archipelago.Instance.apConfig.TrapLinkEnabled)
		{
			EnableTrapLink();
			Archipelago.Instance.Session.Socket.PacketReceived += ReceiveTrapLink;
		}
		else
		{
			DisableTrapLink();
			Archipelago.Instance.Session.Socket.PacketReceived -= ReceiveTrapLink;
		}
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChanged;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChanged;
	}

	private IEnumerator TrapHandler()
	{
		while (Archipelago.Instance.IsConnected())
		{
			if (!trapQueue.TryPeek(out TrapType trap))
			{
				yield return true;
				continue;
			}

			yield return new WaitWhile(PlayerGlobal.instance.InputPaused);

			switch (trap)
			{
				case TrapType.Rotation:
					RotationTrap(); break;
				case TrapType.PlayerInvisibility:
					PlayerInvisibilityTrap(); break;
				case TrapType.EnemyInvisibility:
					bool startRoutine = enemyInvisTimer <= 0f;
					enemyInvisTimer += enemyInvisTime;

					if (startRoutine)
					{
						enemyInvisCoroutine = StartCoroutine(EnemyInvisibilityTrap());
					}

					break;
				case TrapType.Knockback:
					KnockbackTrap(); break;
			}
			trapQueue.TryDequeue(out _);
			// Add delay between each trap
			float timePreDelay = Time.time;
			while (Time.time - timePreDelay < trapDelay)
			{
				yield return null;
			}
		}
	}

	private void OnSceneChanged(Scene _, Scene __)
	{
		// Stop enemy invis coroutine
		if (enemyInvisCoroutine != null)
		{
			StopCoroutine(enemyInvisCoroutine);
			enemyInvisTimer = 0f;
		}
	}

	private float accumulatedAngle = 0f;
	private void RotationTrap()
	{
		accumulatedAngle += 90f;
		CameraRotationControl.instance.Rotate(accumulatedAngle);
	}

	private void PlayerInvisibilityTrap()
	{
		string basePath = "PlayerContents/VISUALS/crow_player (fbx)/";

		PathUtil.GetByPath("_PLAYER", basePath + "body").SetActive(false);
		PathUtil.GetByPath("_PLAYER", basePath + "eyes").SetActive(false);
		PathUtil.GetByPath("_PLAYER", basePath + "tail").SetActive(false);
		switch (GameSave.GetSaveData().GetWeaponId())
		{
			case "sword":
				PathUtil.GetByPath("_PLAYER", basePath + "WEAPON_Sword(Clone)/Model").SetActive(false); break;
			case "hammer":
				PathUtil.GetByPath("_PLAYER", basePath + "WEAPON_Hammer(Clone)/Hammer").SetActive(false); break;
			case "daggers":
				PathUtil.GetByPath("_PLAYER", basePath + "WEAPON_Daggers(Clone)/Model1").SetActive(false);
				PathUtil.GetByPath("_PLAYER", basePath + "WEAPON_Daggers(Clone)/Model2").SetActive(false); break;
			case "sword_heavy":
				PathUtil.GetByPath("_PLAYER", basePath + "WEAPON_Greatsword(Clone)/Model").SetActive(false); break;
			case "umbrella":
				PathUtil.GetByPath("_PLAYER", basePath + "WEAPON_Umbrella(Clone)/Model").SetActive(false); break;
		}
	}

	private IEnumerator EnemyInvisibilityTrap()
	{
		int enemyLayer = LayerMask.NameToLayer("Enemy");

		// Get all active enemy meshes
		List<SkinnedMeshRenderer> allEnemyMeshes = Resources.FindObjectsOfTypeAll<AI_Brain>()
			.Where(ai => ai.gameObject.layer == enemyLayer) // Checks for enemy layer
			.SelectMany(ai => ai.GetComponentsInChildren<SkinnedMeshRenderer>()) // Gets all meshes (there can be many children with meshes)
			.Where(mesh => mesh.gameObject.activeSelf) // Gets only the active meshes
			.ToList();

		foreach (SkinnedMeshRenderer mesh in allEnemyMeshes)
		{
			mesh.gameObject.SetActive(false);
		}

		// Lower invis timer
		while (enemyInvisTimer > 0)
		{
			enemyInvisTimer -= Time.deltaTime;
			yield return null;
		}

		// At this point, invis timer has ran out

		enemyInvisTimer = 0f;

		// Reset all modified enemy meshes. When an enemy is killed, the object gets destroyed.
		// So we only reset meshes that still exist to avoid null ref.
		foreach (SkinnedMeshRenderer mesh in allEnemyMeshes.Where(m => m != null))
		{
			mesh.gameObject.SetActive(true);
		}
	}

	private void KnockbackTrap()
	{
		GameObject player = PlayerGlobal.instance.gameObject;

		// This ensures player always gets knocked backwards
		Vector3 originPos = player.transform.position - player.transform.forward;

		// Get random knockback force
		float force = UnityEngine.Random.Range(1, 5);

		player.GetComponent<DamageablePlayer>().fallOver(originPos, force);

		Logger.Log($"Player received knockback with force of {force}");
	}

	// TrapLink Implementation borrowed from Tunic (SilentDestroyer and ScipioWright)
	public void SendTrapLink(string trapName)
	{
		BouncePacket bouncePacket = new BouncePacket
		{
			Tags = ["TrapLink"],
			Data = new Dictionary<string, JToken>
				{
					{ "time", (float)DateTime.Now.ToUnixTimeStamp() },
					{ "source", Archipelago.Instance.CurrentPlayer.Name },
					{ "trap_name", trapName}
				}
		};
		Archipelago.Instance.Session.Socket.SendPacketAsync(bouncePacket);
	}

	public void ReceiveTrapLink(ArchipelagoPacketBase packet)
	{
		if (Archipelago.Instance.apConfig.TrapLinkEnabled && packet is BouncedPacket bouncedPacket && bouncedPacket.Tags.Contains("TrapLink"))
		{
			// we don't want to receive own trap links, since the other slot will have already received a trap on its own
			// note: if two people are connected to the same slot, both players will likely send their own trap links
			// idk if we can actually fix this? (Note from Silent from Tunic's implementation)
			if (bouncedPacket.Data["source"].ToString() == Archipelago.Instance.CurrentPlayer.Name)
			{
				return;
			}
			string trapName = bouncedPacket.Data["trap_name"].ToString();
			string source = bouncedPacket.Data["source"].ToString();
			if (TrapNameToType.ContainsKey(trapName))
			{
				Logger.Log($"Received TrapLink {trapName} from {source}");
				IC.CountableInventoryItem item = new IC.CountableInventoryItem
				{
					UniqueId = trapName,
					CountId = trapName,
					DisplayName = trapName,
					Icon = "Unknown"
				};
				ItemRandomizer.Instance.itemNotifications.Enqueue(new ItemRandomizer.ItemNotification(trapName, "TrapLink", 0, item));
				trapQueue.Enqueue(TrapNameToType[trapName]);
			}
			else
			{
				return;
			}
		}
	}

	public void EnableTrapLink()
	{
		if (!Archipelago.Instance.Session.ConnectionInfo.Tags.Contains("TrapLink"))
		{
			Archipelago.Instance.Session.ConnectionInfo.UpdateConnectionOptions([.. Archipelago.Instance.Session.ConnectionInfo.Tags, .. new string[1] { "TrapLink" }]);
		}
	}

	public void DisableTrapLink()
	{
		if (Archipelago.Instance.Session.ConnectionInfo.Tags.Contains("TrapLink"))
		{
			Archipelago.Instance.Session.ConnectionInfo.UpdateConnectionOptions([.. Archipelago.Instance.Session.ConnectionInfo.Tags.Where(tag => tag != "TrapLink")]);
		}
	}



	public enum TrapType
	{
		Rotation,
		PlayerInvisibility,
		EnemyInvisibility,
		Knockback,
	}
}