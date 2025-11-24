using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween

/// <summary>
/// GachaReward handles rolling a reward (from a passed GachaMachineDatabase),
/// showing the reward UI, and animating itemInfoObject (RectTransform + CanvasGroup)
/// together with the active rarity UI using DOTween.
/// Includes pity logic:
///  - SuperRare pity every database.superRarePity pulls
///  - UltraRare pity every database.ultraRarePity pulls
/// Pity is tracked on the database (currentSuperRareRolls/currentUltraRareRolls)
/// so other systems can display "pity left".
/// </summary>
public class GachaReward : MonoBehaviour
{
    [Header("References")]
    public GameObject rewardScreen;      // root reward screen
    public GameObject rewardItem;        // parent for instantiated reward prefab (visual)
    public GameObject rewardUIInfo;      // UI container shown when opening capsule
    public Animator capsuleAnim;         // capsule open animation
    public ParticleSystem rainbowVFX;
    public List<GameObject> capsuleColors = new List<GameObject>();

    private GameObject currRewardPrefab;

    // Fired when the reward screen is closed
    public event System.Action OnRewardClosed;

    private RewardResult currReward = new RewardResult();

    [Header("Reward UI Info")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI itemInfo;         // text component inside itemInfoObject
    [SerializeField] private GameObject itemInfoObject;        // object (RectTransform + CanvasGroup) to animate
    [SerializeField] private List<GameObject> rarityUI;        // indexed by (int)Rarity: Normal=0, Rare=1,...

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

    private void Start()
    {
        if (openButton != null) openButton.onClick.AddListener(OnOpenGacha);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseReward);

        if (openButton != null) openButton.gameObject.SetActive(false);
        if (rewardScreen != null) rewardScreen.SetActive(false);
        if (rewardUIInfo != null) rewardUIInfo.SetActive(false);
    }

    /// <summary>
    /// Do a weighted roll on the given database and return a RewardResult containing rarity, prefab, etc.
    /// Includes pity logic based on GachaMachineDatabase.superRarePity and ultraRarePity.
    /// Updates database.currentSuperRareRolls/currentUltraRareRolls so UI can show pity.
    /// </summary>
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

        // --- current pity counters are stored directly on the database ---
        int srCount = database.currentSuperRareRolls;
        int urCount = database.currentUltraRareRolls;

        int nextSrCount = srCount + 1; // as if we do this roll
        int nextUrCount = urCount + 1;

        bool srPityEnabled = database.superRarePity > 0;
        bool urPityEnabled = database.ultraRarePity > 0;

        bool triggerURPity = urPityEnabled && nextUrCount >= database.ultraRarePity;
        bool triggerSRPity = srPityEnabled && nextSrCount >= database.superRarePity;

        // UR pity has priority over SR pity if both hit on the same roll
        GachaMachineDatabase.Rarity? forcedRarity = null;
        if (triggerURPity)
            forcedRarity = GachaMachineDatabase.Rarity.UltraRare;
        else if (triggerSRPity)
            forcedRarity = GachaMachineDatabase.Rarity.SuperRare;

        GachaMachineDatabase.LootEntry chosen = null;

        // ---------------------------------------------------------------------
        // PITY ROLL: only among entries of forcedRarity (still weighted)
        // ---------------------------------------------------------------------
        if (forcedRarity.HasValue)
        {
            List<GachaMachineDatabase.LootEntry> pityPool = new List<GachaMachineDatabase.LootEntry>();
            int pityWeightTotal = 0;

            foreach (var loot in database.lootTable)
            {
                if (loot.rarity == forcedRarity.Value && loot.weight > 0)
                {
                    pityPool.Add(loot);
                    pityWeightTotal += loot.weight;
                }
            }

            if (pityPool.Count > 0 && pityWeightTotal > 0)
            {
                int roll = Random.Range(0, pityWeightTotal);
                foreach (var loot in pityPool)
                {
                    if (loot.weight <= 0) continue;

                    if (roll < loot.weight)
                    {
                        chosen = loot;
                        break;
                    }

                    roll -= loot.weight;
                }

                if (chosen == null)
                    chosen = pityPool[pityPool.Count - 1];
            }
            else
            {
                // if no valid pool (e.g. misconfigured DB), fallback to normal roll
                forcedRarity = null;
            }
        }

        // -------------------------------------------------------------------------
        // NORMAL ROLL (no pity forced, or pity pool misconfigured)
        // -------------------------------------------------------------------------
        if (chosen == null)
        {
            int totalWeight = 0;
            foreach (var loot in database.lootTable)
            {
                if (loot.weight > 0)
                    totalWeight += loot.weight;
            }

            if (totalWeight <= 0)
            {
                Debug.LogError($"[GachaReward] Machine {database.machineId} totalWeight <= 0.");
                return default;
            }

            int roll = Random.Range(0, totalWeight); // [0, totalWeight)

            foreach (var loot in database.lootTable)
            {
                if (loot.weight <= 0) continue;

                if (roll < loot.weight)
                {
                    chosen = loot;
                    break;
                }

                roll -= loot.weight;
            }

            if (chosen == null)
            {
                chosen = database.lootTable[database.lootTable.Count - 1];
            }
        }

        // -------------------------------------------------------------------------
        // Decide final quantity
        // -------------------------------------------------------------------------
        int finalQty;
        int min = Mathf.Max(1, chosen.quantityMin);
        int max = Mathf.Max(min, chosen.quantityMax);

        if (min == 1 && max == 1)
        {
            finalQty = Mathf.Max(1, chosen.baseQuantity);
        }
        else
        {
            int multiplier = Random.Range(min, max + 1);
            finalQty = Mathf.Max(1, chosen.baseQuantity) * multiplier;
        }

        // Respect entry-specific guaranteedMinimum if set
        if (chosen.guaranteedMinimum > 0)
        {
            finalQty = Mathf.Max(finalQty, chosen.guaranteedMinimum);
        }

        // -------------------------------------------------------------------------
        // UPDATE PITY COUNTERS ON DATABASE
        //
        // Rule:
        //  - SuperRare pity counter:
        //      reset when we get SuperRare OR UltraRare
        //      otherwise increment
        //  - UltraRare pity counter:
        //      reset when we get UltraRare
        //      otherwise increment
        // -------------------------------------------------------------------------
        bool gotSR = (chosen.rarity == GachaMachineDatabase.Rarity.SuperRare ||
                      chosen.rarity == GachaMachineDatabase.Rarity.UltraRare);
        bool gotUR = (chosen.rarity == GachaMachineDatabase.Rarity.UltraRare);

        if (srPityEnabled)
        {
            if (gotSR)
                database.currentSuperRareRolls = 0;
            else
                database.currentSuperRareRolls = nextSrCount;
        }

        if (urPityEnabled)
        {
            if (gotUR)
                database.currentUltraRareRolls = 0;
            else
                database.currentUltraRareRolls = nextUrCount;
        }

        // -------------------------------------------------------------------------
        // Return result including rarity
        // -------------------------------------------------------------------------
        var result = new RewardResult(
            chosen.rewardItemId,
            chosen.rewardName,
            finalQty,
            chosen.rewardPrefab,
            chosen.rarity
            );

        database.historyData.AddPull(result.itemName, result.rarity, result.quantity);

        return result;
    }

    [ContextMenu("Test Roll")]
    private void TestRoll_Debug()
    {
        Debug.Log("[GachaReward] TestRoll invoked - requires real database to return meaningful result.");
    }

    /// <summary>
    /// Set the reward manually (useful if you already rolled elsewhere).
    /// </summary>
    public void SetReward(RewardResult result)
    {
        currReward = result;
    }

    /// <summary>
    /// Show the reward screen using the current currReward (must be set by SetReward or Roll).
    /// Animates itemInfoObject (RectTransform + CanvasGroup) together with active rarity UI using DOTween.
    /// Sequence: scale big -> normal, fade in, then punch (vibrato=2).
    /// </summary>
    public void ShowReward()
    {
        RandomizeCapsuleColors();

        if (currReward.prefab == null)
            Debug.LogWarning("[GachaReward] Showing reward without a prefab assigned in currReward.");

        if (itemInfoObject != null)
            itemInfoObject.SetActive(true);

        // Prepare UI text
        if (itemInfo != null)
            itemInfo.text = $"{currReward.itemName}";

        // instantiate reward visual prefab under rewardItem parent
        if (rewardItem != null && currReward.prefab != null)
            currRewardPrefab = Instantiate(currReward.prefab, rewardItem.transform);

        // enable rarity UI
        ShowRarityUI(currReward.rarity);

        // show screen and start coroutine that enables open button after a short delay
        if (rewardScreen != null) rewardScreen.SetActive(true);
        StartCoroutine(ShowRewardCouroutine());
    }

    IEnumerator ShowRewardCouroutine()
    {
        yield return new WaitForSeconds(1f);
        if (openButton != null) openButton.gameObject.SetActive(true);
    }

    void OnOpenGacha()
    {
        if (capsuleAnim != null) capsuleAnim.SetTrigger("Open");
        if (openButton != null) openButton.gameObject.SetActive(false);
        StartCoroutine(OpenGachaCouroutine());
    }

    IEnumerator OpenGachaCouroutine()
    {
        yield return new WaitForSeconds(1.5f);
        if (rewardUIInfo != null) rewardUIInfo.SetActive(true);
        AnimateRewardUI();
    }

    void OnCloseReward()
    {
        if (currRewardPrefab != null) Destroy(currRewardPrefab);
        currReward = new RewardResult();
        if (openButton != null) openButton.gameObject.SetActive(false);
        if (rewardScreen != null) rewardScreen.SetActive(false);
        if (rewardUIInfo != null) rewardUIInfo.SetActive(false);
        if (rainbowVFX != null) rainbowVFX.gameObject.SetActive(false);

        OnRewardClosed?.Invoke();
    }

    /// <summary>
    /// Activates the rarity UI object corresponding to the rolled rarity and deactivates others.
    /// Rarity enum values must match the order of rarityUI list (Normal=0, Rare=1, SuperRare=2, UltraRare=3).
    /// </summary>
    void ShowRarityUI(GachaMachineDatabase.Rarity rarity)
    {
        if (rainbowVFX != null)
            rainbowVFX.gameObject.SetActive(false);

        if (rarityUI == null || rarityUI.Count == 0) return;

        for (int i = 0; i < rarityUI.Count; i++)
            rarityUI[i].SetActive(false);

        int index = (int)rarity;
        if (index >= 0 && index < rarityUI.Count)
            rarityUI[index].SetActive(true);

        if (rarity == GachaMachineDatabase.Rarity.UltraRare && rainbowVFX != null)
        {
            rainbowVFX.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Minimal hard-coded animation sequence using DOTween:
    /// - itemInfoObject & active rarity start big + invisible
    /// - scale down to normal while fading in (via CanvasGroup)
    /// - then DOPunchScale with vibrato = 2
    /// </summary>
    private void AnimateRewardUI()
    {
        // ITEM INFO OBJECT (RectTransform + CanvasGroup)
        RectTransform itemRect = null;
        CanvasGroup itemCg = null;

        if (itemInfoObject != null)
        {
            itemRect = itemInfoObject.GetComponent<RectTransform>();
            itemCg = itemInfoObject.GetComponent<CanvasGroup>();
            if (itemCg == null) itemCg = itemInfoObject.AddComponent<CanvasGroup>();

            // Kill any previous tweens on these objects
            itemRect.DOKill();
            itemCg.DOKill();
            itemInfoObject.transform.DOKill();

            // initial setup: big + invisible
            itemRect.localScale = Vector3.one * 1.5f;
            itemCg.alpha = 0f;
        }

        // Find active rarity object
        GameObject activeRarity = null;
        if (rarityUI != null && rarityUI.Count > 0)
        {
            int idx = (int)currReward.rarity;
            if (idx >= 0 && idx < rarityUI.Count)
                activeRarity = rarityUI[idx];
        }

        RectTransform rarityRect = null;
        CanvasGroup rarityCg = null;
        if (activeRarity != null)
        {
            rarityRect = activeRarity.GetComponent<RectTransform>();
            rarityCg = activeRarity.GetComponent<CanvasGroup>();
            if (rarityCg == null) rarityCg = activeRarity.AddComponent<CanvasGroup>();

            // Kill any existing tweens
            rarityRect?.DOKill();
            rarityCg.DOKill();
            activeRarity.transform.DOKill();

            // initial setup: big + invisible
            if (rarityRect != null) rarityRect.localScale = Vector3.one * 1.5f;
            rarityCg.alpha = 0f;
        }

        // Build sequence
        Sequence seq = DOTween.Sequence();

        // Scale down + fade in (parallel)
        if (itemRect != null && itemCg != null)
        {
            seq.Append(itemRect.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
            seq.Join(itemCg.DOFade(1f, 0.25f));
        }

        if (rarityRect != null && rarityCg != null)
        {
            // join rarity scale/fade so they play alongside item name
            seq.Join(rarityRect.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
            seq.Join(rarityCg.DOFade(1f, 0.25f));
        }

        // After appear, punch scale with vibrato = 2
        seq.AppendCallback(() =>
        {
            if (itemRect != null)
                itemRect.DOPunchScale(Vector3.one * 0.15f, 0.3f, 2, 1f);

            if (rarityRect != null)
                rarityRect.DOPunchScale(Vector3.one * 0.15f, 0.3f, 2, 1f);
        });

        seq.Play();
    }

    public List<RewardResult> Roll10(GachaMachineDatabase database)
    {
        List<RewardResult> results = new List<RewardResult>();

        if (database == null)
        {
            Debug.LogError("[GachaReward] Roll10 called with NULL database.");
            return results;
        }

        for (int i = 0; i < 10; i++)
        {
            RewardResult r = Roll(database);
            results.Add(r);
        }

        //// Notify listeners (UI, sound, animations, save system)
        //OnTenPullComplete?.Invoke(results);

        return results;
    }

    void RandomizeCapsuleColors()
    {
        if (capsuleColors == null || capsuleColors.Count == 0) return;

        foreach (var item in capsuleColors)
        {
            if (item != null)
                item.SetActive(false);
        }

        int randomIndex = Random.Range(0, capsuleColors.Count); // [0, count)
        if (capsuleColors[randomIndex] != null)
            capsuleColors[randomIndex].SetActive(true);
    }
}
