using DDoor.AddUIToOptionsMenu;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

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
    internal IEnumerator trapHandler;
    internal ConcurrentQueue<TrapType> trapQueue;
    private readonly float trapDelay = 3f;
	private Coroutine enemyInvisCoroutine;
	private float enemyInvisTime = 15f;
	private float enemyInvisTimer = 0f;

    private void Awake()
    {
        instance = this;
        trapQueue = new ConcurrentQueue<TrapType>();
        trapHandler = TrapHandler();
    }

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneChanged;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneChanged;
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

	private void OnSceneChanged(Scene _, LoadSceneMode __)
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
        Transform playerVisuals = PlayerGlobal.instance.playerVisuals;

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
		List<SkinnedMeshRenderer> allEnemyMeshes = SceneManager.GetActiveScene()
			.GetRootGameObjects()
			.SelectMany(root => root.GetComponentsInChildren<AI_Brain>(true))
			.Where(ai => ai.gameObject.layer == enemyLayer) // Checks for enemy layer
			.SelectMany(ai => ai.GetComponentsInChildren<SkinnedMeshRenderer>(true)) // Gets all meshes (there can be many children with meshes)
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
		float force = Random.Range(1, 5);

		player.GetComponent<DamageablePlayer>().fallOver(originPos, force);

		Logger.Log($"Player received knockback with force of {force}");
	}



    public enum TrapType
    {
        Rotation,
        PlayerInvisibility,
		EnemyInvisibility,
		Knockback,
	}
}