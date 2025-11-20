using Gravitons.UI.Modal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreScript : MonoBehaviour
{
    [SerializeField] private StoreDatabase storeDatabse;
    [SerializeField] private GameObject storeItemPrefab;
    [SerializeField] private Transform itemContainer;

    private int currSelectedReward;
    private int currPrice;
    private void Start()
    {
        PopulateItemObjects();
    }

    void PopulateItemObjects()
    {
        var itemList = storeDatabse.storeItems;
        foreach (var item in itemList)
        {
            var obj = Instantiate(storeItemPrefab, itemContainer);
            var storeItemScript = obj.GetComponent<StoreItemScript>();

            storeItemScript.SetItemInfo(
                item,
                () => OnBuyItem(item)   // your buy logic here
            );
        }
    }

    private void OnBuyItem(StoreObject item)
    {
        //Debug.Log("Buying: " + item.title + " for " + item.tokenValue + " tokens");
        ModalManager.Show("Buy Item", "Are you sure you want to pick this item?",
      new[] { new ModalButton() { Text = "NO" }, new ModalButton() { Text = "YES", Callback = CheckPlayerBalance } });
        currSelectedReward = item.tokenValue;
        currPrice = item.price;
    }

    private void CheckPlayerBalance()
    {
        var playerData = GachaManager.Instance.GetPlayerData();
      if (playerData.CheckIsSufficientDiamond(currPrice))
        {
            playerData.ReducePlayerDiamond(currPrice);
            OnSuccess();
        }
        else
        {
            ModalManager.Show("Buy Item", "Purchase failed your balance in insufficient",
   new[] { new ModalButton() { Text = "OK" } });
        }
    }

    private void OnSuccess()
    {
        GachaManager.Instance.GetPlayerData().AddPlayerCoin(currSelectedReward); 
        ModalManager.Show("Buy Item","Purchase successful!",
      new[] { new ModalButton() { Text = "OK" }});
    }
}
