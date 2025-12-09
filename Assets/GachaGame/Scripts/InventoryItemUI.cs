using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI quantityText;
    public Image iconImage;
    public Image basePanel;

    [Header("Rarity Objects")]
    public GameObject normalRarityObject;
    public GameObject rareRarityObject;
    public GameObject superRareRarityObject;
    public GameObject ultraRareRarityObject;

    public Color normalColor;
    public Color rareColor;
    public Color superRareColor;
    public Color ultraRareColor;

    public void Setup(PlayerData.InventoryEntry entry)
    {
        if (nameText != null)
            nameText.text = entry.itemName;

        if (quantityText != null)
            quantityText.text = "x" + entry.quantity.ToString();

        if (iconImage != null && entry.icon != null)
            iconImage.sprite = entry.icon;

        SetRarity(entry.rarity);
    }

    private void SetRarity(GachaMachineDatabase.Rarity rarity)
    {
        // turn all off first
        if (normalRarityObject != null) normalRarityObject.SetActive(false);
        if (rareRarityObject != null) rareRarityObject.SetActive(false);
        if (superRareRarityObject != null) superRareRarityObject.SetActive(false);
        if (ultraRareRarityObject != null) ultraRareRarityObject.SetActive(false);

        switch (rarity)
        {
            case GachaMachineDatabase.Rarity.Normal:
                if (normalRarityObject != null) normalRarityObject.SetActive(true);
                basePanel.color = normalColor;
                break;
            case GachaMachineDatabase.Rarity.Rare:
                if (rareRarityObject != null) rareRarityObject.SetActive(true);
                basePanel.color = rareColor;
                break;
            case GachaMachineDatabase.Rarity.SuperRare:
                if (superRareRarityObject != null) superRareRarityObject.SetActive(true);
                basePanel.color = superRareColor;
                break;
            case GachaMachineDatabase.Rarity.UltraRare:
                if (ultraRareRarityObject != null) ultraRareRarityObject.SetActive(true);
                basePanel.color = ultraRareColor;
                break;
        }
    }
}
