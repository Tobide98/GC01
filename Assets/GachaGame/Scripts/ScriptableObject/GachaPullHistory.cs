using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Gacha Pull History",
    menuName = "Gacha System/Player Pull History",
    order = 1)]
public class GachaPullHistory : ScriptableObject
{
    [System.Serializable]
    public class PullEntry
    {
        public string itemName;
        public GachaMachineDatabase.Rarity rarity;
        public int quantity;
        public string timestampString; // stored as formatted string

        public PullEntry(string name, GachaMachineDatabase.Rarity rarity, int qty)
        {
            itemName = name;
            this.rarity = rarity;
            quantity = qty;

            // Format: 14:35 - 26/11/25
            timestampString = System.DateTime.Now.ToString("HH:mm - dd/MM/yy");
        }
    }

    [Header("Settings")]
    public int maxEntries = 30;

    [Header("Stored History (Runtime Only)")]
    public List<PullEntry> pullHistory = new();

    // ---------- API ----------
    public void AddPull(string itemName, GachaMachineDatabase.Rarity rarity, int qty = 1)
    {
        PullEntry newEntry = new PullEntry(itemName, rarity, qty);

        // newest first
        pullHistory.Insert(0, newEntry);

        // enforce cap
        if (pullHistory.Count > maxEntries)
            pullHistory.RemoveAt(pullHistory.Count - 1);
    }

    public void ClearHistory()
    {
        pullHistory.Clear();
    }

    public PullEntry GetLatest() =>
        pullHistory.Count > 0 ? pullHistory[0] : null;
}
