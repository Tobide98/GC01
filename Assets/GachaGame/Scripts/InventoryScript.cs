using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryScript : MonoBehaviour
{
    [Header("Data")]
    public PlayerData playerData;

    [Header("Grid")]
    public Transform gridParent;
    public GameObject itemUIPrefab;

    [Header("Filter Buttons")]
    public Button filterAllButton;
    public Button filterGacha1Button;
    public Button filterGacha2Button;
    public Button filterGacha3Button;
    public Button filterGacha4Button;

    [Header("Sort Buttons")]
    public Button sortAscButton;   // rarity ascending (Normal -> UltraRare)
    public Button sortDescButton;  // rarity descending (UltraRare -> Normal)

    [Header("Preview Handler")]
    public InventoryModelPreview previewHandler;

    [Header("Misc")]
    [SerializeField] private GameObject emptyInfo;

    private enum FilterCategory
    {
        All = 0,
        Gacha1 = 1,
        Gacha2 = 2,
        Gacha3 = 3,
        Gacha4 = 4
    }

    private enum SortOrder
    {
        None,
        RarityAscending,
        RarityDescending
    }

    private FilterCategory currentFilter = FilterCategory.All;
    private SortOrder currentSort = SortOrder.None;

    private void Awake()
    {
        // Filter listeners
        if (filterAllButton != null) filterAllButton.onClick.AddListener(() => SetFilter(FilterCategory.All));
        if (filterGacha1Button != null) filterGacha1Button.onClick.AddListener(() => SetFilter(FilterCategory.Gacha1));
        if (filterGacha2Button != null) filterGacha2Button.onClick.AddListener(() => SetFilter(FilterCategory.Gacha2));
        if (filterGacha3Button != null) filterGacha3Button.onClick.AddListener(() => SetFilter(FilterCategory.Gacha3));
        if (filterGacha4Button != null) filterGacha4Button.onClick.AddListener(() => SetFilter(FilterCategory.Gacha4));

        // Sort listeners
        if (sortAscButton != null) sortAscButton.onClick.AddListener(() =>
        {
            // toggle: if already asc, clear sort; otherwise set asc
            if (currentSort == SortOrder.RarityAscending) ClearSort();
            else SetSortAscending();
        });

        if (sortDescButton != null) sortDescButton.onClick.AddListener(() =>
        {
            if (currentSort == SortOrder.RarityDescending) ClearSort();
            else SetSortDescending();
        });
    }

    private void Start()
    {
        SetFilter(FilterCategory.All);
    }

    private void OnEnable()
    {
        Refresh();
    }

    // --- Filter API ---
    private void SetFilter(FilterCategory filter)
    {
        currentFilter = filter;
        Refresh();
    }

    public void SetFilterAll() => SetFilter(FilterCategory.All);
    public void SetFilterGacha1() => SetFilter(FilterCategory.Gacha1);
    public void SetFilterGacha2() => SetFilter(FilterCategory.Gacha2);
    public void SetFilterGacha3() => SetFilter(FilterCategory.Gacha3);
    public void SetFilterGacha4() => SetFilter(FilterCategory.Gacha4);

    // --- Sort API ---
    private void SetSortAscending()
    {
        currentSort = SortOrder.RarityAscending;
        Refresh();
        UpdateSortButtonVisuals();
    }

    private void SetSortDescending()
    {
        currentSort = SortOrder.RarityDescending;
        Refresh();
        UpdateSortButtonVisuals();
    }

    private void ClearSort()
    {
        currentSort = SortOrder.None;
        Refresh();
        UpdateSortButtonVisuals();
    }

    private void UpdateSortButtonVisuals()
    {
        // simple visual feedback: toggle interactable state so user can see active button
        // (designer may want to replace with color/selected state)
        if (sortAscButton != null) sortAscButton.interactable = currentSort != SortOrder.RarityAscending;
        if (sortDescButton != null) sortDescButton.interactable = currentSort != SortOrder.RarityDescending;
    }

    public void Refresh()
    {
        emptyInfo.SetActive(false);

        if (gridParent == null || itemUIPrefab == null)
        {
            Debug.LogWarning("InventoryScript missing gridParent or itemUIPrefab reference.");
            return;
        }

        ClearGrid();

        if (playerData == null || playerData.inventory == null || playerData.inventory.Count <= 0)
        {
            emptyInfo.SetActive(true);
            return;
        }

        // 1) Filter
        IEnumerable<PlayerData.InventoryEntry> query = playerData.inventory;

        if (currentFilter != FilterCategory.All)
        {
            int filterLeading = (int)currentFilter;
            query = query.Where(entry => GetLeadingDigit(entry.itemId) == filterLeading);
        }

        // 2) Sort
        switch (currentSort)
        {
            case SortOrder.RarityAscending:
                // Rarity enum order assumed: Normal=0, Rare=1, SuperRare=2, UltraRare=3
                query = query.OrderBy(e => (int)e.rarity).ThenBy(e => e.itemId);
                break;
            case SortOrder.RarityDescending:
                query = query.OrderByDescending(e => (int)e.rarity).ThenBy(e => e.itemId);
                break;
            case SortOrder.None:
            default:
                // no extra sorting beyond original insertion order
                break;
        }

        // 3) Instantiate UI for final list
        foreach (var entry in query)
        {
            if (entry == null) continue;

            GameObject go = Instantiate(itemUIPrefab, gridParent);
            var ui = go.GetComponent<InventoryItemUI>();
            if (ui != null)
            {
                ui.Setup(entry);

                // make the item clickable — show 3D model in preview
                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (previewHandler != null)
                        {
                            previewHandler.gameObject.SetActive(true);
                            previewHandler.Display(entry);
                        }
                        else
                        {
                            Debug.LogWarning("InventoryModelPreview not assigned!");
                        }
                    });
                }
            }
        }

        // ensure sort button visuals are updated (in case Refresh called externally)
        UpdateSortButtonVisuals();
    }

    private int GetLeadingDigit(int value)
    {
        value = Mathf.Abs(value);
        if (value == 0) return 0;
        while (value >= 10) value /= 10;
        return value;
    }

    private void ClearGrid()
    {
        if (gridParent == null) return;

        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            var child = gridParent.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }
    }

    //void PlayAnimation()
    //{
    //    var canvasGroup = this.GetComponent<CanvasGroup>();
    //    canvasGroup.DOKill();
    //    transform.DOKill();

    //    canvasGroup.alpha = 0f;
    //    this.transform.localScale = Vector3.one * 0.7f;
        
    //    canvasGroup.DOFade(1f, 0.3f);
    //    this.transform
    //            .DOScale(1f, 0.3f)
    //            .SetEase(Ease.OutBack, 1.2f);
    //}
}
