using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Gacha Machine Database",
    menuName = "Gacha Machine/Database",
    order = 0)]
public class GachaMachineDatabase : ScriptableObject
{
    [Serializable]
    public class LootEntry
    {
        [Header("Loot config (itemId / base qty / weight)")]
        public GameObject rewardPrefab;
        public int rewardItemId;          // from sheet, e.g. 1001
        public string rewardName;
        [Min(1)] public int baseQuantity = 1;  // base quantity if you don't use range
        [Min(0)] public int weight = 1;        // probability weight

        [Header("Quantity range (optional, overrides baseQuantity)")]
        [Min(1)] public int quantityMin = 1;   // 数量下限
        [Min(1)] public int quantityMax = 1;   // 数量上限

        [Header("Pity / guarantee (optional)")]
        public int highestTrigger = 0;         // 最高触发
        public int guaranteedMinimum = 0;      // 保底
    }

    [Header("Machine Info")]
    public int machineId;          // e.g. 1001, 2001 ...
    public string displayName;     // machine name (from sheet)
    public int machinePrice;

    [Header("Weighted Loot Table")]
    public List<LootEntry> lootTable = new List<LootEntry>();
}
