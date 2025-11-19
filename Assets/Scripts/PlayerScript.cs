using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] private int coin;
    [SerializeField] private TextMeshProUGUI coinText;

    private void Start()
    {
        SetPlayerCoin(coin);
    }

    public bool CheckIssufficient(int price)
    {
        if(coin < price) return false;
        else return true;
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
        coinText.text = coin.ToString("N0");
    }
}
