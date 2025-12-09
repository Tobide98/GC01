using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerData",
    menuName = "Gacha System/Player Data",
    order = 0)]
public class PlayerData : ScriptableObject
{
    [Header("Currency")]
    public int coin;
    public int diamond;

    [System.Serializable]
    public class InventoryEntry
    {
        public int itemId;
        public string itemName;
        public GachaMachineDatabase.Rarity rarity;
        public GameObject prefab;
        public int quantity;
        public Sprite icon;
    }

    [Header("Inventory")]
    public List<InventoryEntry> inventory = new List<InventoryEntry>();

    // ------------------- CURRENCY API -------------------

    // Coin
    public bool HasEnoughCoin(int amount) => coin >= amount;

    public void AddCoin(int amount) => coin += Mathf.Max(0, amount);

    public void ReduceCoin(int amount) => coin = Mathf.Max(0, coin - Mathf.Max(0, amount));

    public void SetCoin(int amount) => coin = Mathf.Max(0, amount);

    // Diamond
    public bool HasEnoughDiamond(int amount) => diamond >= amount;

    public void AddDiamond(int amount) => diamond += Mathf.Max(0, amount);

    public void ReduceDiamond(int amount) => diamond = Mathf.Max(0, diamond - Mathf.Max(0, amount));

    public void SetDiamond(int amount) => diamond = Mathf.Max(0, amount);

    // ------------------- INVENTORY API -------------------

    /// <summary>
    /// Add or merge an item into inventory using raw info.
    /// If same itemId already exists, only increase quantity.
    /// </summary>
    public void AddItem(int itemId, string itemName, GachaMachineDatabase.Rarity rarity,
                        GameObject prefab, int quantityToAdd, Sprite itemIcon)
    {
        if (quantityToAdd <= 0) return;

        // Search existing entry by itemId
        InventoryEntry existing = inventory.Find(e => e.itemId == itemId);

        if (existing != null)
        {
            existing.quantity += quantityToAdd;
        }
        else
        {
            InventoryEntry newEntry = new InventoryEntry
            {
                itemId = itemId,
                itemName = itemName,
                rarity = rarity,
                prefab = prefab,
                quantity = quantityToAdd,
                icon = itemIcon
            };
            inventory.Add(newEntry);
        }
    }

    /// <summary>
    /// Convenience: add item from a loot data + computed qty.
    /// </summary>
    public void AddItemFromLoot(GachaMachineDatabase.LootEntry loot, int quantityToAdd)
    {
        if (loot == null) return;
        AddItem(loot.rewardItemId, loot.rewardName, loot.rarity, loot.rewardPrefab, quantityToAdd, loot.icon);
    }

    /// <summary>
    /// Get inventory entry by itemId (or null if not found).
    /// </summary>
    public InventoryEntry GetEntryById(int itemId)
    {
        return inventory.Find(e => e.itemId == itemId);
    }
}
