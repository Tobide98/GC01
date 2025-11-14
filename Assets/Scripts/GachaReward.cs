using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GachaReward : MonoBehaviour
{
    public GameObject rewardScreen;
    public GameObject rewardItem;
    public GameObject rewardUIInfo;
    public Animator capsuleAnim;

    private GameObject currRewardPrefab;

    public event System.Action OnRewardClosed; 

    private RewardResult currReward = new RewardResult();

    [Header("Reward UI Info")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI itemInfo;

    [System.Serializable]
    public struct RewardResult
    {
        public int itemId;
        public string itemName;
        public int quantity;
        public GameObject prefab;

        public RewardResult(int itemId, string itemName,int quantity, GameObject prefab)
        {
            this.itemId = itemId;
            this.itemName = itemName;
            this.quantity = quantity;
            this.prefab = prefab;
        }
    }

    private void Start()
    {
        openButton.onClick.AddListener(OnOpenGacha);
        closeButton.onClick.AddListener(OnCloseReward);
        openButton.gameObject.SetActive(false);
        rewardScreen.SetActive(false);
        rewardUIInfo.SetActive(false);
    }

    /// <summary>
    /// Roll one reward from the assigned machine database using weights.
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

        // 1) Sum all weights
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

        // 2) Weighted roll
        int roll = Random.Range(0, totalWeight); // [0, totalWeight)
        GachaMachineDatabase.LootEntry chosen = null;

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
            // Fallback (shouldn't happen)
            chosen = database.lootTable[database.lootTable.Count - 1];
        }

        // 3) Decide final quantity
        int finalQty;

        // If range is basically 1–1, just use baseQuantity
        int min = Mathf.Max(1, chosen.quantityMin);
        int max = Mathf.Max(min, chosen.quantityMax);

        if (min == 1 && max == 1)
        {
            // Only base quantity
            finalQty = Mathf.Max(1, chosen.baseQuantity);
        }
        else
        {
            // Random multiplier between min and max
            int multiplier = Random.Range(min, max + 1);
            finalQty = Mathf.Max(1, chosen.baseQuantity) * multiplier;
        }

        return new RewardResult(chosen.rewardItemId, chosen.rewardName,finalQty, chosen.rewardPrefab);
    }

    [ContextMenu("Test Roll")]
    private void TestRoll(GachaMachineDatabase database)
    {
        var result = Roll(database);
        Debug.Log($"[GachaReward] Machine {database?.machineId} -> item {result.itemId} x{result.quantity}");
    }

    public void SetReward(RewardResult result)
    {
        currReward = result;
    }

    public void ShowReward()
    {
        StartCoroutine(ShowRewardCouroutine());
        itemInfo.text = $"{currReward.itemName} x{currReward.quantity}";
        currRewardPrefab = Instantiate(currReward.prefab, rewardItem.transform);
        rewardScreen.SetActive(true);
    }

    IEnumerator ShowRewardCouroutine()
    {
        yield return new WaitForSeconds(1f);
        openButton.gameObject.SetActive(true);
    }

    void OnOpenGacha()
    {
        rewardUIInfo.SetActive(true);
        capsuleAnim.SetTrigger("Open");
        openButton.gameObject.SetActive(false);
    }

    void OnCloseReward()
    {
        Destroy(currRewardPrefab);
        currReward = new RewardResult(); 
        openButton.gameObject.SetActive(false);
        rewardScreen.SetActive(false);
        rewardUIInfo.SetActive(false);
        OnRewardClosed?.Invoke();
    }
}
