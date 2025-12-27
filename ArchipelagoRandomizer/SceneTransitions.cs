using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DDoor.ArchipelagoRandomizer;


public static class SceneTransitions
{
    static SceneTransitions()
    {
        PopulateTransitionFromJson();
    }
    public readonly struct SceneTransition(
            string startingRegion,
            string endingRegion,
            string loadingZoneId,
            string toSceneName,
            string originSceneName
        )
    {
        public readonly string startingRegion = startingRegion;
        public readonly string endingRegion = endingRegion;
        public readonly string loadingZoneId = loadingZoneId;
        public readonly string toSceneName = toSceneName;
        public readonly string originSceneName = originSceneName;
    }

    private static List<SceneTransition> transitionData;

    private static void PopulateTransitionFromJson()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("DDoor.ArchipelagoRandomizer.Data.SceneTransitions.json"));
        transitionData = JsonConvert.DeserializeObject<List<SceneTransition>>(reader.ReadToEnd());
    }

    internal static SceneTransition? GetThisSceneTransition(string loadingZoneId, string sceneName)
    {
        try
        {
            return transitionData.First(scene => scene.loadingZoneId.Equals(loadingZoneId.Replace("avarice_", ""),
                StringComparison.OrdinalIgnoreCase)
                && scene.toSceneName.Equals(sceneName, StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    internal static SceneTransition? GetOriginalSceneTransition(string loadingZoneId, string sceneToLoad)
    {
        try
        {
            SceneTransition endingPoint = transitionData.First(scene => scene.loadingZoneId.Equals(loadingZoneId.Replace("avarice_", ""),
                StringComparison.OrdinalIgnoreCase)
                && scene.toSceneName.Equals(sceneToLoad, StringComparison.OrdinalIgnoreCase));
            string exitName = endingPoint.endingRegion;
            foreach (KeyValuePair<string, string> kvp in EntranceRandomizer.Instance.GetEntrancePairings())
            {
                if (kvp.Value == exitName)
                {
                    return transitionData.First(scene => scene.startingRegion == kvp.Key);
                }
            }
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    internal static SceneTransition? GetConnectedSceneTransition(string loadingZoneId, string sceneName)
    {
        SceneTransition? sceneTransition = GetThisSceneTransition(loadingZoneId, sceneName);
        if (sceneTransition != null)
        {
            string entranceName = sceneTransition?.startingRegion;
            if (EntranceRandomizer.Instance.GetEntrancePairings().TryGetValue(entranceName, out string exitName))
            {
                return transitionData.First(scene => scene.endingRegion == exitName);
            }
        }
        return null;
    }

    internal static bool IsSceneInRandomizedTransitions(string sceneName) =>
        transitionData.Any(scene => scene.toSceneName == sceneName);

}
#nullable disable
