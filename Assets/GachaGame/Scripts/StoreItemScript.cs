using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class StoreItemScript : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI tokenValueText;
    [SerializeField] private Button buyButton;

    // Optional: keep the data if you need it later
    private StoreObject currentData;

    /// <summary>
    /// Set item info from separate values + assign buy button callback.
    /// </summary>
    public void SetItemInfo(
        string title,
        Sprite icon,
        string description,
        int tokenValue,
        int price,
        UnityAction onBuyCallback)
    {
        currentData = null; // not using StoreObject in this overload

        if (iconImage != null)
            iconImage.sprite = icon;

        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        if (tokenValueText != null)
            tokenValueText.text = price.ToString();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();

            if (onBuyCallback != null)
                buyButton.onClick.AddListener(onBuyCallback);
        }
    }

    /// <summary>
    /// Convenience overload: directly pass StoreObject.
    /// </summary>
    public void SetItemInfo(StoreObject data, UnityAction onBuyCallback)
    {
        currentData = data;

        SetItemInfo(
            data.title,
            data.icon,
            data.description,
            data.tokenValue,
            data.price,
            onBuyCallback
        );
    }

    /// <summary>
    /// Optional getter if you need the data from outside.
    /// </summary>
    public StoreObject GetData()
    {
        return currentData;
    }
}
