using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Coin")]
    [SerializeField] private int coin;
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Diamond")]
    [SerializeField] private int diamond;
    [SerializeField] private TextMeshProUGUI diamondText;

    private void Start()
    {
        SetPlayerCoin(coin);
        SetPlayerDiamond(diamond);
    }

    // ------------------- COIN -------------------

    public bool CheckIsSufficientCoin(int price)
    {
        return coin >= price;
    }

    public int GetPlayerCoin()
    {
        UpdatePlayerCoin();
        return coin;
    }

    public void ReducePlayerCoin(int amount)
    {
        coin -= amount;
        UpdatePlayerCoin();
    }

    public void AddPlayerCoin(int amount)
    {
        coin += amount;
        UpdatePlayerCoin();
    }

    public void SetPlayerCoin(int coinSet)
    {
        coin = coinSet;
        UpdatePlayerCoin();
    }

    void UpdatePlayerCoin()
    {
        if (coinText != null)
            coinText.text = coin.ToString("N0");
    }

    // ------------------- DIAMOND -------------------

    public bool CheckIsSufficientDiamond(int price)
    {
        return diamond >= price;
    }

    public int GetPlayerDiamond()
    {
        UpdatePlayerDiamond();
        return diamond;
    }

    public void ReducePlayerDiamond(int amount)
    {
        diamond -= amount;
        UpdatePlayerDiamond();
    }

    public void AddPlayerDiamond(int amount)
    {
        diamond += amount;
        UpdatePlayerDiamond();
    }

    public void SetPlayerDiamond(int diamondSet)
    {
        diamond = diamondSet;
        UpdatePlayerDiamond();
    }

    void UpdatePlayerDiamond()
    {
        if (diamondText != null)
            diamondText.text = diamond.ToString("N0");
    }
}
