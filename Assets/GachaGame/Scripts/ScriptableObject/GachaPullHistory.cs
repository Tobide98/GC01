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
        public string timestampString; // ⭐ stored as human-formatted string

        public PullEntry(string name, GachaMachineDatabase.Rarity rarity, int qty)
        {
            itemName = name;
            this.rarity = rarity;
            quantity = qty;
            timestampString = System.DateTime.Now.ToString("dd/MM/yyyy");
        }
    }

    [Header("Settings")]
    public int maxEntries = 30;

    [Header("Runtime Memory")]
    public List<PullEntry> pullHistory = new();

    // ---------- PUBLIC API ----------
    public void AddPull(string itemName, GachaMachineDatabase.Rarity rarity, int qty = 1)
    {
        PullEntry newEntry = new PullEntry(itemName, rarity, qty);

        pullHistory.Insert(0, newEntry);

        if (pullHistory.Count > maxEntries)
            pullHistory.RemoveAt(pullHistory.Count - 1);

        SaveToPrefs();
    }

    public void ClearHistory()
    {
        pullHistory.Clear();
        SaveToPrefs();
    }

    public PullEntry GetLatest() =>
        pullHistory.Count > 0 ? pullHistory[0] : null;


    // ---------- SAVE / LOAD USING PLAYER PREFS ----------
    private const string SAVE_KEY = "GACHA_HISTORY_DATA";

    public void SaveToPrefs()
    {
        // Convert history into one serialized string
        List<string> rows = new List<string>();

        foreach (var entry in pullHistory)
        {
            string row = $"{entry.itemName}|{entry.rarity}|{entry.quantity}|{entry.timestampString}";
            rows.Add(row);
        }

        PlayerPrefs.SetString(SAVE_KEY, string.Join("\n", rows));
        PlayerPrefs.Save();
    }

    public void LoadFromPrefs()
    {
        pullHistory.Clear();

        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        string raw = PlayerPrefs.GetString(SAVE_KEY);
        string[] rows = raw.Split('\n');

        foreach (string line in rows)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split('|');
            if (parts.Length < 4) continue;

            PullEntry entry = new PullEntry(
                parts[0],
                (GachaMachineDatabase.Rarity)System.Enum.Parse(typeof(GachaMachineDatabase.Rarity), parts[1]),
                int.Parse(parts[2])
            );

            entry.timestampString = parts[3]; // ⭐ restored as string
            pullHistory.Add(entry);
        }
    }
}
