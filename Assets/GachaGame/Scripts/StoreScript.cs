using Gravitons.UI.Modal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoreScript : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private StoreDatabase storeDatabse;

    [Header("Prefabs / Containers")]
    [SerializeField] private GameObject tokenItemPrefab;
    [SerializeField] private GameObject diamondItemPrefab;
    [SerializeField] private Transform coinStoreContainer;
    [SerializeField] private Transform diamondStoreContainer;

    [Header("Tab CanvasGroups")]
    [SerializeField] private CanvasGroup tokenTabCanvasGroup;
    [SerializeField] private CanvasGroup diamondTabCanvasGroup;

    [Header("Tab Buttons")]
    [SerializeField] private Button tokenTabButton;    // coin tab button
    [SerializeField] private Button diamondTabButton;  // diamond tab button

    // internal selection for purchase flow
    private int currSelectedReward;
    private int currPrice;

    private void Awake()
    {
        // Hook up tab buttons
        if (tokenTabButton != null) tokenTabButton.onClick.AddListener(() => ShowTokenTab());
        if (diamondTabButton != null) diamondTabButton.onClick.AddListener(() => ShowDiamondTab());
    }

    private void OnEnable()
    {
        // Reset to coin tab every time this screen is enabled
        ShowTokenTab();

        // Populate items (clear + build)
        PopulateItemObjects();
    }

    private void Start()
    {
        // If you want to populate once at Start only, you can call PopulateItemObjects() here instead.
        // PopulateItemObjects();
    }

    void PopulateItemObjects()
    {
        if (storeDatabse == null || diamondItemPrefab == null)
        {
            Debug.LogWarning("StoreScript: Missing database or prefab");
            return;
        }

        // Clear existing children to avoid duplicates
        if (coinStoreContainer != null)
        {
            for (int i = coinStoreContainer.childCount - 1; i >= 0; i--)
                DestroyImmediate(coinStoreContainer.GetChild(i).gameObject);
        }

        if (diamondStoreContainer != null)
        {
            for (int i = diamondStoreContainer.childCount - 1; i >= 0; i--)
                DestroyImmediate(diamondStoreContainer.GetChild(i).gameObject);
        }

        // Populate coin store (items that give tokens / coin)
        var coinStoreList = storeDatabse.coinStoreItems;
        if (coinStoreList != null)
        {
            foreach (var item in coinStoreList)
            {
                if (coinStoreContainer == null) break;

                var obj = Instantiate(tokenItemPrefab, coinStoreContainer);
                var storeItemScript = obj.GetComponent<StoreItemScript>();

                if (storeItemScript != null)
                {
                    // When clicking this store item, it should attempt to buy with diamonds (example)
                    storeItemScript.SetItemInfo(
                        item,
                        () => OnBuyCoin(item)
                    );
                }
            }
        }

        // Populate diamond store (items that give diamonds)
        var diamondStoreList = storeDatabse.diamondStoreItems;
        if (diamondStoreList != null)
        {
            foreach (var item in diamondStoreList)
            {
                if (diamondStoreContainer == null) break;

                var obj = Instantiate(diamondItemPrefab, diamondStoreContainer);
                var storeItemScript = obj.GetComponent<StoreItemScript>();

                if (storeItemScript != null)
                {
                    // When clicking this store item, it should attempt to buy with tokens (example)
                    storeItemScript.SetItemInfo(
                        item,
                        () => OnBuyDiamonds(item)
                    );
                }
            }
        }
    }

    // ---------------------------
    // TAB SWITCHING
    // ---------------------------

    private void ShowTokenTab()
    {
        // show token canvas
        SetCanvasGroupVisible(tokenTabCanvasGroup, true);
        SetCanvasGroupVisible(diamondTabCanvasGroup, false);

        // disable token tab button until the other button is clicked
        if (tokenTabButton != null) tokenTabButton.interactable = false;
        if (diamondTabButton != null) diamondTabButton.interactable = true;
    }

    private void ShowDiamondTab()
    {
        SetCanvasGroupVisible(tokenTabCanvasGroup, false);
        SetCanvasGroupVisible(diamondTabCanvasGroup, true);

        // disable diamond tab button until other clicked
        if (diamondTabButton != null) diamondTabButton.interactable = false;
        if (tokenTabButton != null) tokenTabButton.interactable = true;
    }

    private void SetCanvasGroupVisible(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    // ---------------------------
    // PURCHASE LOGIC
    // ---------------------------

    // Buying a token (coin) package — typically purchased using diamonds
    private void OnBuyCoin(StoreObject item)
    {
        if (item == null) return;

        // Set selection data BEFORE opening modal
        currSelectedReward = item.value; // the coin amount to grant
        currPrice = item.price;         // cost in diamonds (assumption)

        ModalManager.Show(
            "Buy Item",
            $"Are you sure you want to purchase {item.title} for {item.price} diamonds?",
            new[]
            {
                new ModalButton() { Text = "NO" },
                new ModalButton() { Text = "YES", Callback = CheckPlayerDiamondBalance }
            }
        );
    }

    // Buying a diamond package — typically purchased using coins (tokens)
    private void OnBuyDiamonds(StoreObject item)
    {
        if (item == null) return;

        currSelectedReward = item.value; // diamond amount to grant
        currPrice = item.price;         // cost in tokens (assumption)

        ModalManager.Show(
            "Buy Item",
            $"Are you sure you want to purchase {item.title} for {item.price} dollar?",
            new[]
            {
                new ModalButton() { Text = "NO" },
                new ModalButton() { Text = "YES", Callback = CheckPlayerCoinBalance }
            }
        );
    }

    private void CheckPlayerDiamondBalance()
    {
        var playerData = GachaManager.Instance.GetPlayerData();
        if (playerData == null) return;

        if (playerData.CheckIsSufficientDiamond(currPrice))
        {
            playerData.ReducePlayerDiamond(currPrice);
            OnSuccess_GiveCoins();
        }
        else
        {
            ModalManager.Show(
                "Buy Item",
                "Purchase failed — your diamond balance is insufficient.",
                new[] { new ModalButton() { Text = "OK" } }
            );
        }
    }

    private void CheckPlayerCoinBalance()
    {
        var playerData = GachaManager.Instance.GetPlayerData();
        if (playerData == null) return;


        OnSuccess_GiveDiamonds();

        //if (playerData.CheckIsSufficientCoin(currPrice))
        //{
        //    playerData.ReducePlayerCoin(currPrice);
        //    OnSuccess_GiveDiamonds();
        //}
        //else
        //{
        //    ModalManager.Show(
        //        "Buy Item",
        //        "Purchase failed — your token balance is insufficient.",
        //        new[] { new ModalButton() { Text = "OK" } }
        //    );
        //}
    }

    private void OnSuccess_GiveCoins()
    {
        var playerData = GachaManager.Instance.GetPlayerData();
        if (playerData == null) return;

        playerData.AddPlayerCoin(currSelectedReward);

        ModalManager.Show(
            "Buy Item",
            "Purchase successful!",
            new[] { new ModalButton() { Text = "OK" } }
        );
    }

    private void OnSuccess_GiveDiamonds()
    {
        var playerData = GachaManager.Instance.GetPlayerData();
        if (playerData == null) return;

        playerData.AddPlayerDiamond(currSelectedReward);

        ModalManager.Show(
            "Buy Item",
            "Purchase successful!",
            new[] { new ModalButton() { Text = "OK" } }
        );
    }
}
