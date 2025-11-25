using TMPro;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Player Data Asset")]
    [SerializeField] private PlayerData playerData;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI diamondText;

    private void Start()
    {
        UpdateUI();
    }
    public bool CheckIsSufficientCoin(int price)
    {
        return playerData != null && playerData.HasEnoughCoin(price);
    }

    public int GetPlayerCoin()
    {
        UpdateCoinUI();
        return playerData != null ? playerData.coin : 0;
    }

    public void ReducePlayerCoin(int amount)
    {
        if (playerData == null) return;
        playerData.ReduceCoin(amount);
        UpdateCoinUI();
    }

    public void AddPlayerCoin(int amount)
    {
        if (playerData == null) return;
        playerData.AddCoin(amount);
        UpdateCoinUI();
    }

    public void SetPlayerCoin(int coinSet)
    {
        if (playerData == null) return;
        playerData.SetCoin(coinSet);
        UpdateCoinUI();
    }

    void UpdateCoinUI()
    {
        if (coinText != null && playerData != null)
            coinText.text = playerData.coin.ToString("N0");
    }

    // ------------------- DIAMOND -------------------

    public bool CheckIsSufficientDiamond(int price)
    {
        return playerData != null && playerData.HasEnoughDiamond(price);
    }

    public int GetPlayerDiamond()
    {
        UpdateDiamondUI();
        return playerData != null ? playerData.diamond : 0;
    }

    public void ReducePlayerDiamond(int amount)
    {
        if (playerData == null) return;
        playerData.ReduceDiamond(amount);
        UpdateDiamondUI();
    }

    public void AddPlayerDiamond(int amount)
    {
        if (playerData == null) return;
        playerData.AddDiamond(amount);
        UpdateDiamondUI();
    }

    public void SetPlayerDiamond(int diamondSet)
    {
        if (playerData == null) return;
        playerData.SetDiamond(diamondSet);
        UpdateDiamondUI();
    }

    void UpdateDiamondUI()
    {
        if (diamondText != null && playerData != null)
            diamondText.text = playerData.diamond.ToString("N0");
    }

    public void UpdateUI()
    {
        UpdateCoinUI();
        UpdateDiamondUI();
    }

    public void AddInventory(GachaReward.RewardResult result)
    {
            playerData.AddItem(
                result.itemId,
                result.itemName,
                result.rarity,
                result.prefab,
                result.quantity
            );
        Debug.Log($"Added to inventory {result.itemName}");
    }
}
