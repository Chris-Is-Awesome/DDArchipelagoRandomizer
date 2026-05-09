using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DDoor.ArchipelagoRandomizer;

public static class ComponentUtil
{
    public static List<T> FindAllComponentsOfTypeInScene<T>(string parentScene)
    {
        Scene activeScene = SceneManager.GetSceneByName(parentScene);
        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        List<T> allComponents = [];
        for (int i = 0; i < rootObjects.Count(); i++)
        {
            allComponents.AddRange([.. rootObjects[i].GetComponentsInChildren<T>(true)]);
        }
        return allComponents;
    }
}