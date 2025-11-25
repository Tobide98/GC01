using System.Text;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

public class GachaHistoryInfo : MonoBehaviour
{
    [Header("History Reference")]
    public GachaPullHistory historyData;

    [Header("UI Reference")]
    public TextMeshProUGUI historyText;

    [Header("Formatting")]
    public bool showIndexNumbers = true;
    public int maxShownEntries = 100;
    public int fontSize = 32;
    public int decimalPlaces = 0; // currently unused but kept for future extension

    [Header("Typing Effect")]
    public bool useTypingEffect = true;
    public float charsPerSecond = 80f;

    [Header("Rarity Colors (Same as Drop Rate UI)")]
    public Color normalColor = Color.white;
    public Color rareColor = new Color(0.3f, 0.6f, 1f);
    public Color superRareColor = new Color(1f, 0.5f, 0f);
    public Color ultraRareColor = new Color(1f, 0.2f, 0.2f);

    private Dictionary<GachaMachineDatabase.Rarity, string> rarityHex;
    private Coroutine typingRoutine;

    private void Awake()
    {
        // Build colors as early as possible so Refresh() is always safe
        BuildColorLookup();
    }

    private void Start()
    {
        if (historyText != null)
            historyText.fontSize = fontSize;

        Refresh();
    }

    private void OnEnable()
    {
        PlayAnimation();
        Refresh();
    }

    private void BuildColorLookup()
    {
        rarityHex = new Dictionary<GachaMachineDatabase.Rarity, string>
        {
            { GachaMachineDatabase.Rarity.Normal,     ColorUtility.ToHtmlStringRGB(normalColor) },
            { GachaMachineDatabase.Rarity.Rare,       ColorUtility.ToHtmlStringRGB(rareColor) },
            { GachaMachineDatabase.Rarity.SuperRare,  ColorUtility.ToHtmlStringRGB(superRareColor) },
            { GachaMachineDatabase.Rarity.UltraRare,  ColorUtility.ToHtmlStringRGB(ultraRareColor) }
        };
    }

    public void SetDatabse(GachaMachineDatabase databaseSelected)
    {
        historyData = databaseSelected.historyData;
        Refresh();
    }

    public void Refresh()
    {
        if (historyText == null)
        {
            Debug.LogWarning("GachaHistoryInfo: No Text assigned!");
            return;
        }

        if (historyData == null || historyData.pullHistory == null || historyData.pullHistory.Count == 0)
        {
            SetInstantText("No pulls yet.");
            return;
        }

        // Extra safety: if for some reason colors weren't built yet
        if (rarityHex == null)
            BuildColorLookup();

        StringBuilder sb = new StringBuilder();
        int count = Mathf.Min(maxShownEntries, historyData.pullHistory.Count);

        for (int i = 0; i < count; i++)
        {
            var entry = historyData.pullHistory[i];
            if (entry == null) continue; // safety

            string rarityName = FormatRarity(entry.rarity);

            if (!rarityHex.TryGetValue(entry.rarity, out string colorHex))
                colorHex = "FFFFFF"; // fallback to white

            if (showIndexNumbers)
                sb.Append($"{i + 1}. ");

            // use the saved timestampString directly
            string dateString = string.IsNullOrEmpty(entry.timestampString)
                ? "-"
                : entry.timestampString;

            sb.Append(
                $"{entry.itemName} <color=#{colorHex}>({rarityName})</color> - {dateString}"
            );

            if (i < count - 1)
                sb.AppendLine();
        }

        string finalText = sb.ToString();

        if (useTypingEffect)
            StartTyping(finalText);
        else
            SetInstantText(finalText);
    }

    private string FormatRarity(GachaMachineDatabase.Rarity rarity)
    {
        return rarity switch
        {
            GachaMachineDatabase.Rarity.SuperRare => "Super Rare",
            GachaMachineDatabase.Rarity.UltraRare => "Ultra Rare",
            _ => rarity.ToString()
        };
    }

    private void StartTyping(string text)
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeRoutine(text));
    }

    private IEnumerator TypeRoutine(string text)
    {
        historyText.text = text;
        historyText.ForceMeshUpdate();

        int totalChars = historyText.textInfo.characterCount;
        historyText.maxVisibleCharacters = 0;

        float visible = 0;

        while (visible < totalChars)
        {
            visible += charsPerSecond * Time.unscaledDeltaTime;
            historyText.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(visible), 0, totalChars);
            yield return null;
        }

        historyText.maxVisibleCharacters = totalChars;
        typingRoutine = null;
    }

    private void SetInstantText(string text)
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        historyText.text = text;
        historyText.maxVisibleCharacters = int.MaxValue;
    }

    void PlayAnimation()
    {
        var canvasGroup = this.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.7f;

        canvasGroup.DOFade(1f, 0.3f);
        transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack, 1.2f);
    }
}
