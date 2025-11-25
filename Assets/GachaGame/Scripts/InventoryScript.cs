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

    [Header("Preview Handler")]
    public InventoryModelPreview previewHandler;
    // ← This script will display the 3D model (you’ll create it)

    private enum FilterCategory
    {
        All = 0,
        Gacha1 = 1,
        Gacha2 = 2,
        Gacha3 = 3,
        Gacha4 = 4
    }

    private FilterCategory currentFilter = FilterCategory.All;

    private void Awake()
    {
        if (filterAllButton != null) filterAllButton.onClick.AddListener(() => SetFilter(FilterCategory.All));
        if (filterGacha1Button != null) filterGacha1Button.onClick.AddListener(() => SetFilter(FilterCategory.Gacha1));
        if (filterGacha2Button != null) filterGacha2Button.onClick.AddListener(() => SetFilter(FilterCategory.Gacha2));
        if (filterGacha3Button != null) filterGacha3Button.onClick.AddListener(() => SetFilter(FilterCategory.Gacha3));
        if (filterGacha4Button != null) filterGacha4Button.onClick.AddListener(() => SetFilter(FilterCategory.Gacha4));
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void SetFilter(FilterCategory filter)
    {
        currentFilter = filter;
        Refresh();
    }

    public void Refresh()
    {
        if (gridParent == null || itemUIPrefab == null)
        {
            Debug.LogWarning("InventoryScript missing grid or prefab reference.");
            return;
        }

        ClearGrid();

        if (playerData == null || playerData.inventory == null) return;

        foreach (var entry in playerData.inventory)
        {
            if (!MatchesFilter(entry)) continue;

            GameObject go = Instantiate(itemUIPrefab, gridParent);
            var ui = go.GetComponent<InventoryItemUI>();
            if (ui != null)
            {
                ui.Setup(entry);

                // 🔥 Make the item clickable
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
                            Debug.LogWarning("InventoryModelPreview not assigned!");
                    });
                }
            }
        }
    }

    private bool MatchesFilter(PlayerData.InventoryEntry entry)
    {
        if (currentFilter == FilterCategory.All)
            return true;

        return GetLeadingDigit(entry.itemId) == (int)currentFilter;
    }

    private int GetLeadingDigit(int value)
    {
        value = Mathf.Abs(value);
        while (value >= 10) value /= 10;
        return value;
    }

    private void ClearGrid()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);
    }
}
