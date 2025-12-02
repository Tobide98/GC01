using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GachaReward : MonoBehaviour
{
    [Header("References")]
    public GameObject rewardScreen;
    public GameObject rewardItem;
    public GameObject rewardUIInfo;
    public GameObject resultUIInfo;
    public TextMeshProUGUI resultUIText;
    public Animator capsuleAnim;
    public ParticleSystem rainbowVFX;
    public List<GameObject> capsuleColors = new List<GameObject>();

    private GameObject currRewardPrefab;

    public event System.Action OnRewardClosed;

    // ---- reward queue ----
    private List<RewardResult> rewardQueue = new List<RewardResult>();
    private bool showingMultiple = false;

    // ---- session summary (all rewards for this roll batch) ----
    private List<RewardResult> sessionRewards = new List<RewardResult>();
    private bool showingResult = false;

    [Header("Reward UI Info")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button skipButton;      // skip button
    [SerializeField] private TextMeshProUGUI itemInfo;
    [SerializeField] private GameObject itemInfoObject;
    [SerializeField] private List<GameObject> rarityUI;

    [Header("Rarity Colors")]
    public Color normalColor = Color.white;
    public Color rareColor = new Color(0.3f, 0.6f, 1f);
    public Color superRareColor = new Color(1f, 0.5f, 0f);
    public Color ultraRareColor = new Color(1f, 0.2f, 0.2f);

    private Dictionary<GachaMachineDatabase.Rarity, string> rarityHex;

    [System.Serializable]
    public struct RewardResult
    {
        public int itemId;
        public string itemName;
        public int quantity;
        public GameObject prefab;
        public GachaMachineDatabase.Rarity rarity;

        public RewardResult(int itemId, string itemName, int quantity, GameObject prefab, GachaMachineDatabase.Rarity rarity)
        {
            this.itemId = itemId;
            this.itemName = itemName;
            this.quantity = quantity;
            this.prefab = prefab;
            this.rarity = rarity;
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 120;
    }

    private void Start()
    {
        BuildColorLookup();

        if (openButton != null) openButton.onClick.AddListener(OnOpenGacha);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseReward);
        if (skipButton != null) skipButton.onClick.AddListener(SkipAll);   // hook skip

        openButton?.gameObject.SetActive(false);
        closeButton?.gameObject.SetActive(false);
        skipButton?.gameObject.SetActive(false);
        rewardScreen?.SetActive(false);
        rewardUIInfo?.SetActive(false);
        resultUIInfo?.SetActive(false);
    }

    // -------- Roll Logic (unchanged) --------
    public RewardResult Roll(GachaMachineDatabase database)
    {
        if (database == null)
        {
            Debug.LogError("[GachaReward] No database assigned.");
            return default;
        }

        if (database.lootTable == null || database.lootTable.Count == 0)
        {
            Debug.LogError($"[GachaReward] Machine {database.machineId} lootTable is empty.");
            return default;
        }

        int srCount = database.currentSuperRareRolls;
        int urCount = database.currentUltraRareRolls;

        int nextSrCount = srCount + 1;
        int nextUrCount = urCount + 1;

        bool srPityEnabled = database.superRarePity > 0;
        bool urPityEnabled = database.ultraRarePity > 0;

        bool triggerURPity = urPityEnabled && nextUrCount >= database.ultraRarePity;
        bool triggerSRPity = srPityEnabled && nextSrCount >= database.superRarePity;

        GachaMachineDatabase.Rarity? forcedRarity = null;
        if (triggerURPity) forcedRarity = GachaMachineDatabase.Rarity.UltraRare;
        else if (triggerSRPity) forcedRarity = GachaMachineDatabase.Rarity.SuperRare;

        GachaMachineDatabase.LootEntry chosen = null;

        if (forcedRarity.HasValue)
        {
            List<GachaMachineDatabase.LootEntry> pityPool = new();
            int pityTotal = 0;

            foreach (var loot in database.lootTable)
            {
                if (loot.rarity == forcedRarity.Value && loot.weight > 0)
                {
                    pityPool.Add(loot);
                    pityTotal += loot.weight;
                }
            }

            if (pityPool.Count > 0)
            {
                int roll = Random.Range(0, pityTotal);
                foreach (var loot in pityPool)
                {
                    if (roll < loot.weight)
                    {
                        chosen = loot;
                        break;
                    }
                    roll -= loot.weight;
                }
            }
            else
            {
                forcedRarity = null;
            }
        }

        if (chosen == null)
        {
            int totalWeight = 0;
            foreach (var loot in database.lootTable)
                totalWeight += Mathf.Max(0, loot.weight);

            int roll = Random.Range(0, totalWeight);

            foreach (var loot in database.lootTable)
            {
                if (roll < loot.weight)
                {
                    chosen = loot;
                    break;
                }
                roll -= loot.weight;
            }
        }

        int min = Mathf.Max(1, chosen.quantityMin);
        int max = Mathf.Max(min, chosen.quantityMax);

        int finalQty = (min == max)
            ? Mathf.Max(1, chosen.baseQuantity)
            : Mathf.Max(1, chosen.baseQuantity) * Random.Range(min, max + 1);

        if (chosen.guaranteedMinimum > 0)
            finalQty = Mathf.Max(finalQty, chosen.guaranteedMinimum);

        bool gotSR = chosen.rarity == GachaMachineDatabase.Rarity.SuperRare ||
                     chosen.rarity == GachaMachineDatabase.Rarity.UltraRare;
        bool gotUR = chosen.rarity == GachaMachineDatabase.Rarity.UltraRare;

        if (srPityEnabled)
            database.currentSuperRareRolls = gotSR ? 0 : nextSrCount;

        if (urPityEnabled)
            database.currentUltraRareRolls = gotUR ? 0 : nextUrCount;

        var result = new RewardResult(chosen.rewardItemId, chosen.rewardName, finalQty, chosen.rewardPrefab, chosen.rarity);
        database.historyData.AddPull(result.itemName, result.rarity, result.quantity);
        GachaManager.Instance.GetPlayerData().AddInventory(result);

        return result;
    }

    // -------- QUEUE SUPPORT --------

    public void SetReward(RewardResult result)
    {
        sessionRewards.Clear();
        sessionRewards.Add(result);

        rewardQueue.Clear();
        rewardQueue.Add(result);

        showingMultiple = false;
        showingResult = false;
    }

    public void SetReward(List<RewardResult> results)
    {
        sessionRewards = new List<RewardResult>(results);
        rewardQueue = new List<RewardResult>(results);

        showingMultiple = results != null && results.Count > 1;
        showingResult = false;
    }

    public void ShowReward()
    {
        if (rewardQueue.Count == 0) return;

        var curr = rewardQueue[0];

        // choose capsule based on rarity (fallbacks to random)
        SetCapsuleByRarity(curr.rarity);

        closeButton?.gameObject.SetActive(false);
        itemInfoObject?.SetActive(true);
        resultUIInfo?.SetActive(false);

        if (itemInfo != null)
            itemInfo.text = curr.itemName;

        if (currRewardPrefab != null)
            Destroy(currRewardPrefab);

        if (rewardItem != null && curr.prefab != null)
            currRewardPrefab = Instantiate(curr.prefab, rewardItem.transform);

        ShowRarityUI(curr.rarity);

        // show skip only if more than one reward in this session
        if (skipButton != null)
            skipButton.gameObject.SetActive(sessionRewards.Count > 1);

        rewardScreen?.SetActive(true);
        StartCoroutine(ShowRewardCouroutine());
    }

    IEnumerator ShowRewardCouroutine()
    {
        yield return new WaitForSeconds(1f);
        openButton?.gameObject.SetActive(true);
    }

    void OnOpenGacha()
    {
        capsuleAnim?.SetTrigger("Open");
        openButton?.gameObject.SetActive(false);
        StartCoroutine(OpenGachaCouroutine());
    }

    IEnumerator OpenGachaCouroutine()
    {
        yield return new WaitForSeconds(1.5f);
        rewardUIInfo?.SetActive(true);
        closeButton?.gameObject.SetActive(true);
        AnimateRewardUI();
    }

    void OnCloseReward()
    {
        // If we're on the result screen, closing means fully exit
        if (showingResult)
        {
            showingResult = false;
            sessionRewards.Clear();

            if (currRewardPrefab != null)
                Destroy(currRewardPrefab);

            openButton?.gameObject.SetActive(false);
            closeButton?.gameObject.SetActive(false);
            skipButton?.gameObject.SetActive(false);
            rewardScreen?.SetActive(false);
            rewardUIInfo?.SetActive(false);
            resultUIInfo?.SetActive(false);
            rainbowVFX?.gameObject.SetActive(false);

            OnRewardClosed?.Invoke();
            return;
        }

        // We are closing an individual reward view
        if (currRewardPrefab != null)
            Destroy(currRewardPrefab);

        // More rewards left → go to next
        if (showingMultiple && rewardQueue.Count > 1)
        {
            rewardQueue.RemoveAt(0);
            capsuleAnim?.SetTrigger("Reset");

            openButton?.gameObject.SetActive(false);
            rewardUIInfo?.SetActive(false);

            ShowReward(); // show next reward
            return;
        }

        // This was the last reward in the queue → show result summary
        rewardQueue.Clear();
        showingMultiple = false;

        BuildResultSummary();
    }

    // ---- SKIP BUTTON: show ALL Ultra Rares sequentially, then result screen ----
    public void SkipAll()
    {
        // Already on result screen? Do nothing (user can just close)
        if (showingResult)
            return;

        // If no session rewards at all, just close like normal
        if (sessionRewards.Count == 0)
        {
            OnCloseReward();
            return;
        }

        // Clear current prefab + per-item UI
        if (currRewardPrefab != null)
        {
            Destroy(currRewardPrefab);
            currRewardPrefab = null;
        }

        // Find all UltraRare items in sessionRewards (preserve order)
        List<RewardResult> urList = new List<RewardResult>();
        foreach (var r in sessionRewards)
        {
            if (r.rarity == GachaMachineDatabase.Rarity.UltraRare)
                urList.Add(r);
        }

        if (urList.Count > 0)
        {
            // Show all URs sequentially (rewardQueue will contain only URs)
            rewardQueue.Clear();
            rewardQueue.AddRange(urList);
            showingMultiple = rewardQueue.Count > 1;
            showingResult = false;

            // hide per-item UI and show first UR now
            openButton?.gameObject.SetActive(false);
            rewardUIInfo?.SetActive(false);

            ShowReward();
            skipButton.gameObject.SetActive(false);
            return;
        }

        // No UltraRare found → go straight to result summary
        rewardQueue.Clear();
        showingMultiple = false;

        openButton?.gameObject.SetActive(false);
        rewardUIInfo?.SetActive(false);

        BuildResultSummary();
    }

    void BuildResultSummary()
    {
        rewardUIInfo?.SetActive(false);

        if (resultUIInfo != null && resultUIText != null && sessionRewards.Count > 0)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < sessionRewards.Count; i++)
            {
                var r = sessionRewards[i];
                string rarityName = FormatRarity(r.rarity);

                // pick rarity color
                string hex = rarityHex.ContainsKey(r.rarity) ? rarityHex[r.rarity] : "FFFFFF";
                string coloredRarity = $"<color=#{hex}>({rarityName})</color>";

                sb.AppendLine($"{i + 1}. {r.itemName} {coloredRarity}");
            }

            resultUIText.text = sb.ToString();
            resultUIInfo.SetActive(true);
            showingResult = true;

            // On result screen, no need skip anymore
            skipButton?.gameObject.SetActive(false);
            closeButton?.gameObject.SetActive(true);
        }
        else
        {
            showingResult = false;
            sessionRewards.Clear();
            rewardScreen?.SetActive(false);
            OnRewardClosed?.Invoke();
        }
    }

    IEnumerator NextRewardRoutine()
    {
        openButton?.gameObject.SetActive(false);
        rewardUIInfo?.SetActive(false);
        yield return new WaitForSeconds(1f);
        ShowReward();
    }

    void ShowRarityUI(GachaMachineDatabase.Rarity rarity)
    {
        rainbowVFX?.gameObject.SetActive(false);

        if (rarityUI != null)
        {
            for (int i = 0; i < rarityUI.Count; i++)
                rarityUI[i]?.SetActive(false);

            int idx = (int)rarity;
            if (idx >= 0 && idx < rarityUI.Count && rarityUI[idx] != null)
                rarityUI[idx].SetActive(true);
        }

        if (rarity == GachaMachineDatabase.Rarity.UltraRare)
            rainbowVFX?.gameObject.SetActive(true);
    }

    private void AnimateRewardUI()
    {
        if (itemInfoObject == null) return;

        RectTransform itemRect = itemInfoObject.GetComponent<RectTransform>();
        CanvasGroup itemCg = itemInfoObject.GetComponent<CanvasGroup>() ?? itemInfoObject.AddComponent<CanvasGroup>();

        itemRect.DOKill();
        itemCg.DOKill();

        itemRect.localScale = Vector3.one * 1.5f;
        itemCg.alpha = 0f;

        Sequence seq = DOTween.Sequence()
            .Append(itemRect.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack))
            .Join(itemCg.DOFade(1f, 0.25f))
            .AppendCallback(() => itemRect.DOPunchScale(Vector3.one * 0.15f, 0.3f, 2));

        seq.Play();
    }

    public List<RewardResult> Roll10(GachaMachineDatabase database)
    {
        List<RewardResult> results = new();

        for (int i = 0; i < 10; i++)
            results.Add(Roll(database));

        return results;
    }

    /// <summary>
    /// Activates a capsule color GameObject that corresponds to the given rarity.
    /// Mapping: Normal = index 0, Rare = 1, SuperRare = 2, UltraRare = 3
    /// If the capsuleColors list doesn't contain an entry for that rarity, falls back to random.
    /// </summary>
    void SetCapsuleByRarity(GachaMachineDatabase.Rarity rarity)
    {
        // deactivate all first
        foreach (var item in capsuleColors)
            item?.SetActive(false);

        int idx = (int)rarity; // expects ordering as described above

        if (idx >= 0 && idx < capsuleColors.Count && capsuleColors[idx] != null)
        {
            capsuleColors[idx].SetActive(true);
            return;
        }

        // fallback: random if exact mapping not found
        RandomizeCapsuleColors();
    }

    void RandomizeCapsuleColors()
    {
        foreach (var item in capsuleColors)
            item?.SetActive(false);

        if (capsuleColors.Count > 0)
        {
            int idx = Random.Range(0, capsuleColors.Count);
            if (capsuleColors[idx] != null)
                capsuleColors[idx].SetActive(true);
        }
    }

    private string FormatRarity(GachaMachineDatabase.Rarity rarity)
    {
        switch (rarity)
        {
            case GachaMachineDatabase.Rarity.SuperRare: return "Super Rare";
            case GachaMachineDatabase.Rarity.UltraRare: return "Ultra Rare";
            default: return rarity.ToString();
        }
    }

    private void BuildColorLookup()
    {
        rarityHex = new Dictionary<GachaMachineDatabase.Rarity, string>
        {
            { GachaMachineDatabase.Rarity.Normal,     ColorUtility.ToHtmlStringRGB(normalColor) },
            { GachaMachineDatabase.Rarity.Rare,       ColorUtility.ToHtmlStringRGB(rareColor) },
            { GachaMachineDatabase.Rarity.SuperRare,  ColorUtility.ToHtmlStringRGB(superRareColor) },
            { GachaMachineDatabase.Rarity.UltraRare,  ColorUtility.ToHtmlStringRGB(ultraRareColor) },
        };
    }
}
