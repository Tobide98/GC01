using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Gacha Machine Database",
    menuName = "Gacha Machine/Database",
    order = 0)]
public class GachaMachineDatabase : ScriptableObject
{
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
        public Rarity rarity = Rarity.Normal;

        [Header("Quantity range (optional, overrides baseQuantity)")]
        [Min(1)] public int quantityMin = 1;
        [Min(1)] public int quantityMax = 1;

        [Header("Pity / guarantee (optional, per-entry)")]
        public int highestTrigger = 0;
        public int guaranteedMinimum = 0;
    }

    [Header("Machine Info")]
    public int machineId;
    public string displayName;
    public int machinePrice;
    public int machinePriceTen;
    public Sprite bannerImage;
    public Color machineColor;
    public GachaPullHistory historyData;

    [Header("Weighted Loot Table")]
    public List<LootEntry> lootTable = new List<LootEntry>();

    [Header("Pity Settings (per machine)")]
    [Tooltip("Number of pulls until guaranteed SuperRare. 0 = disabled.")]
    public int superRarePity = 10;

    [Tooltip("Number of pulls until guaranteed UltraRare. 0 = disabled.")]
    public int ultraRarePity = 50;

    [Header("Pity Runtime State (for UI display)")]
    [Tooltip("How many rolls since last SuperRare or higher for this machine.")]
    public int currentSuperRareRolls = 0;

    [Tooltip("How many rolls since last UltraRare for this machine.")]
    public int currentUltraRareRolls = 0;
}
