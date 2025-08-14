using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;

namespace DDoor.ArchipelagoRandomizer;

public static class Locations
{
    static Locations()
    {
        PopulateLocationsFromJson();
    }
    public readonly struct Location(
        string itemChangerName,
        long apLocationId
    )
    {
        public readonly string itemChangerName = itemChangerName;
        public readonly long apLocationId = apLocationId;
    }
    private static List<Location> locationData;

    private static void PopulateLocationsFromJson()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("DDoor.ArchipelagoRandomizer.Data.Locations.json"));
        locationData = JsonConvert.DeserializeObject<List<Location>>(reader.ReadToEnd());
    }

    public static string APItemInfoToDDLocationName(ItemInfo itemInfo)
    {
        if (itemInfo.ItemGame == "Death's Door")
        {
            try
            {
                return locationData.First(entry => entry.apLocationId == itemInfo.LocationId).itemChangerName;
            }
            catch (InvalidOperationException)
            {
                return $"Unknown Location: {itemInfo.LocationDisplayName}";
            }
        }
        else
        {
            return itemInfo.LocationDisplayName;
        }
    }

    public static long ItemChangerLocationToAPLocationID(string itemChangerLocation)
    {
        try
        {
            return locationData.First(entry => entry.itemChangerName == itemChangerLocation).apLocationId;
        }
        catch (InvalidOperationException)
        {
            throw new KeyNotFoundException($"Location with ItemChanger name {itemChangerLocation} was not found. Report this to the developers.");
        }
        
    }
}
