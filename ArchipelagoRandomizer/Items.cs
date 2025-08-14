using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;

namespace DDoor.ArchipelagoRandomizer;

public static class Items
{
    static Items()
    {
        PopulateItemsFromJson();
    }
    public readonly struct Item(
            string itemChangerName,
            long apItemId
        )
    {
        public readonly string itemChangerName = itemChangerName;
        public readonly long apItemId = apItemId;
    }

    private static List<Item> itemData;

    private static void PopulateItemsFromJson()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("DDoor.ArchipelagoRandomizer.Data.Items.json"));
        itemData = JsonConvert.DeserializeObject<List<Item>>(reader.ReadToEnd());
    }
    
    public static string APItemInfoToDDItemName(ItemInfo itemInfo)
	{
        if (itemInfo.ItemGame == "Death's Door")
        {
            try
            {
                return itemData.First(entry => entry.apItemId == itemInfo.ItemId).itemChangerName;
            }
            catch (InvalidOperationException)
            {
                return $"Unknown Item: {itemInfo.ItemDisplayName}";
            }
		}
        else
        {
            return itemInfo.ItemDisplayName;
        }
	}
}