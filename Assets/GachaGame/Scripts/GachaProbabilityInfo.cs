using System.Text;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class GachaProbabilityInfo : MonoBehaviour
{
    [Header("Database Reference")]
    public GachaMachineDatabase database;

    [Header("UI Reference")]
    public TextMeshProUGUI probabilityText;

    [Header("Formatting Options")]
    public bool showTitle = true;
    public string titleFormat = "Drop Rates - {0}";
    public int decimalPlaces = 2;
    public bool sortByHighestChance = true;

    [Header("Rarity Colors")]
    public Color normalColor = Color.white;
    public Color rareColor = new Color(0.3f, 0.6f, 1f);
    public Color superRareColor = new Color(1f, 0.5f, 0f);
    public Color ultraRareColor = new Color(1f, 0.2f, 0.2f);

    [Header("Typing Effect")]
    public bool useTypingEffect = true;
    [Tooltip("Characters revealed per second when typing.")]
    public float charsPerSecond = 60f;

    [Header("Rarity Listing")]
    [Tooltip("Show a summary list of item names grouped by rarity at the bottom.")]
    public bool showRarityNameLists = true;

    private Dictionary<GachaMachineDatabase.Rarity, string> rarityColorHex;
    private Coroutine typingCoroutine;

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        PlayAnimation();
    }

    public void SetDatabse(GachaMachineDatabase databaseSelected)
    {
        database = databaseSelected;
        Refresh();
    }

    /// <summary>
    /// Call this whenever the loot table or colors change.
    /// </summary>
    public void Refresh()
    {
        if (probabilityText == null)
        {
            Debug.LogWarning("GachaProbabilityInfo: No TextMeshProUGUI assigned.");
            return;
        }

        if (database == null || database.lootTable == null || database.lootTable.Count == 0)
        {
            SetTextInstant("No loot data found.");
            return;
        }

        BuildColorLookup();

        // Calculate total weight
        long totalWeight = 0;
        foreach (var entry in database.lootTable)
        {
            if (entry == null) continue;
            if (entry.weight > 0)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0)
        {
            SetTextInstant("Invalid loot weights.");
            return;
        }

        StringBuilder sb = new StringBuilder();

        // -------- Title --------
        if (showTitle)
        {
            string machineName = string.IsNullOrWhiteSpace(database.displayName)
                ? $"Machine #{database.machineId}"
                : database.displayName;

            sb.AppendLine($"<b>{string.Format(titleFormat, machineName)}</b>");
            sb.AppendLine();
        }

        // -------- Per-item probabilities --------
        List<GachaMachineDatabase.LootEntry> sorted =
            new List<GachaMachineDatabase.LootEntry>(database.lootTable);

        if (sortByHighestChance)
            sorted.Sort((a, b) => b.weight.CompareTo(a.weight));

        string percentFormat = "F" + Mathf.Clamp(decimalPlaces, 0, 4);

        // For rarity summary
        Dictionary<GachaMachineDatabase.Rarity, List<string>> groupedNames =
            new Dictionary<GachaMachineDatabase.Rarity, List<string>>();

        foreach (GachaMachineDatabase.Rarity r in System.Enum.GetValues(typeof(GachaMachineDatabase.Rarity)))
            groupedNames[r] = new List<string>();

        foreach (var entry in sorted)
        {
            if (entry == null) continue;
            if (entry.weight <= 0) continue;

            float percent = (float)entry.weight / totalWeight * 100f;

            string name = string.IsNullOrWhiteSpace(entry.rewardName)
                ? (entry.rewardPrefab != null ? entry.rewardPrefab.name : "Unnamed")
                : entry.rewardName;

            groupedNames[entry.rarity].Add(name);

            string colorHex = rarityColorHex[entry.rarity];
            string rarityDisplay = GetRarityDisplayName(entry.rarity);   // UltraRare -> Ultra Rare

            sb.AppendLine(
                $"{name} <color=#{colorHex}>({rarityDisplay})</color>  -  {percent.ToString(percentFormat)}%");
        }

        // -------- Rarity summary lists --------
        if (showRarityNameLists)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Items by Rarity</b>");

            foreach (var kvp in groupedNames)
            {
                var rarity = kvp.Key;
                var names = kvp.Value;
                if (names.Count == 0) continue;

                string color = rarityColorHex[rarity];
                string rarityDisplay = GetRarityDisplayName(rarity);

                sb.AppendLine(
                    $"<color=#{color}><b>{rarityDisplay}:</b></color> {string.Join(", ", names)}");
            }
        }

        string finalText = sb.ToString();

        if (useTypingEffect)
            StartTyping(finalText);
        else
            SetTextInstant(finalText);
    }

    // ----------------- helpers -----------------

    private void BuildColorLookup()
    {
        rarityColorHex = new Dictionary<GachaMachineDatabase.Rarity, string>
        {
            { GachaMachineDatabase.Rarity.Normal, ColorUtility.ToHtmlStringRGB(normalColor) },
            { GachaMachineDatabase.Rarity.Rare, ColorUtility.ToHtmlStringRGB(rareColor) },
            { GachaMachineDatabase.Rarity.SuperRare, ColorUtility.ToHtmlStringRGB(superRareColor) },
            { GachaMachineDatabase.Rarity.UltraRare, ColorUtility.ToHtmlStringRGB(ultraRareColor) }
        };
    }

    private string GetRarityDisplayName(GachaMachineDatabase.Rarity rarity)
    {
        // Custom spacing for your enum names
        switch (rarity)
        {
            case GachaMachineDatabase.Rarity.Normal: return "Normal";
            case GachaMachineDatabase.Rarity.Rare: return "Rare";
            case GachaMachineDatabase.Rarity.SuperRare: return "Super Rare";
            case GachaMachineDatabase.Rarity.UltraRare: return "Ultra Rare";
            default: return rarity.ToString();
        }
    }

    private void SetTextInstant(string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        probabilityText.text = text;
        probabilityText.maxVisibleCharacters = int.MaxValue;
    }

    private void StartTyping(string fullText)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeRoutine(fullText));
    }

    private IEnumerator TypeRoutine(string fullText)
    {
        probabilityText.text = fullText;
        probabilityText.ForceMeshUpdate();   // ensure textInfo is up to date

        int totalChars = probabilityText.textInfo.characterCount;
        probabilityText.maxVisibleCharacters = 0;

        if (totalChars == 0)
        {
            typingCoroutine = null;
            yield break;
        }

        float visibleChars = 0f;

        while (visibleChars < totalChars)
        {
            visibleChars += charsPerSecond * Time.unscaledDeltaTime;
            int v = Mathf.Clamp(Mathf.FloorToInt(visibleChars), 0, totalChars);
            probabilityText.maxVisibleCharacters = v;
            yield return null;
        }

        probabilityText.maxVisibleCharacters = totalChars;
        typingCoroutine = null;
    }

    void PlayAnimation()
    {
        var canvasGroup = this.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        this.transform.localScale = Vector3.one * 0.7f;

        canvasGroup.DOFade(1f, 0.3f);
        this.transform
            .DOScale(1f, 0.3f)
            .SetEase(Ease.OutBack, 1.2f);
    }
}
