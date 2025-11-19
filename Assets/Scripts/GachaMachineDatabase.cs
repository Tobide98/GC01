using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Gacha Machine Database",
    menuName = "Gacha Machine/Database",
    order = 0)]
public class GachaMachineDatabase : ScriptableObject
{
    // ⭐ NEW RARITY ENUM
    public enum Rarity
    {
        Normal,
        Rare,
        SuperRare,
        UltraRare
    }

    [Serializable]
    public class LootEntry
    {
        [Header("Loot config (itemId / base qty / weight)")]
        public GameObject rewardPrefab;
        public int rewardItemId;
        public string rewardName;

        [Min(1)] public int baseQuantity = 1;
        [Min(0)] public int weight = 1;

        [Header("Item Rarity")]
        public Rarity rarity = Rarity.Normal;   // ⭐ NEW FIELD

        [Header("Quantity range (optional, overrides baseQuantity)")]
        [Min(1)] public int quantityMin = 1;
        [Min(1)] public int quantityMax = 1;

        [Header("Pity / guarantee (optional)")]
        public int highestTrigger = 0;
        public int guaranteedMinimum = 0;
    }

    [Header("Machine Info")]
    public int machineId;
    public string displayName;
    public int machinePrice;
    public Sprite bannerImage;

    [Header("Weighted Loot Table")]
    public List<LootEntry> lootTable = new List<LootEntry>();
}
