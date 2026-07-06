using DDoor.AddUIToOptionsMenu;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DDoor.ArchipelagoRandomizer;

public class MapManager : MonoBehaviour
{
    private static MapManager instance;

    public static MapManager Instance => instance;
    public readonly struct PlayerCoords(float x, float y, float z)
    {
        public float X { get; } = x;
        public float Y { get; } = y;
        public float Z { get; } = z;
    }

    private PlayerCoords lastSentCoords;
    private float lastSentTime;

    private const float timeDelta = 5.0f;
    private const float distanceDelta = 10.0f;

    private readonly Dictionary<string, string> sceneToMap = new(){
        {"lvl_HallOfDoors", "hall_of_doors"},
        {"lvl_Tutorial", "grove_of_spirits"},
        { "lvl_Graveyard", "lost_cemetery"},
        {"lvlConnect_Graveyard_Gardens", "crypt"},
        {"lvl_GrandmaGardens", "estate_of_the_urn_witch"},
        {"lvl_GrandmaMansion", "ceramic_manor"},
        {"lvlconnect_Mansion_Basement", "furnace_observation_rooms"},
        {"lvl_GrandmaBasement", "inner_furnace"},
        {"boss_Grandma", "the_urn_witchs_laboratory"},
        {"lvl_Forest", "overgrown_ruins"},
        {"lvl_Swamp", "flooded_fortress"},
        {"boss_Frog", "throne_of_the_frog_king"},
        {"lvlConnect_Graveyard_Sailor", "stranded_sailor_caves"},
        {"lvl_SailorMountain", "stranded_sailor"},
        {"lvl_FrozenFortress", "castle_lockstone"},
        {"lvlConnect_Fortress_Mountaintops", "camp_of_the_free_crows"},
        {"lvl_mountaintops", "old_watchtowers"},
        {"boss_betty", "bettys_lair"},
    };

    private void Awake()
    {
        instance = this;
        SceneManager.sceneLoaded += UpdateMap;
    }

    private void Update()
    {
        UpdateCoordsIfNeeded();
    }

    internal void UpdateMap(Scene scene, LoadSceneMode _)
    {

        if (sceneToMap.ContainsKey(scene.name))
        {
            Archipelago.Instance.StoreMap(sceneToMap[scene.name]);
        }
    }

    private void UpdateCoordsIfNeeded()
    {
		if (Preloader.IsPreloading)
			return;

        float currentTime = Time.time;
        if (PlayerGlobal.instance)
        {
            PlayerCoords currentCoords = CurrentCoords();
            if (currentTime >= lastSentTime + timeDelta)
            {
                UpdateCoordinates(currentCoords);
            }
            else if (Vector3.Distance(new(lastSentCoords.X, lastSentCoords.Y, lastSentCoords.Z), new(currentCoords.X, currentCoords.Y, currentCoords.Z)) > distanceDelta)
            {
                UpdateCoordinates(currentCoords);
            }
        }
    }

    private PlayerCoords CurrentCoords()
    {
        return new(PlayerGlobal.instance.transform.position.x, PlayerGlobal.instance.transform.position.y, PlayerGlobal.instance.transform.position.z);
    }

    private void UpdateCoordinates(PlayerCoords currentCoords)
    {
        float currentTime = Time.time;
        Archipelago.Instance.StorePosition(currentCoords);
        lastSentCoords = currentCoords;
        lastSentTime = currentTime;
    }
}