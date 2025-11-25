using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryModelPreview : MonoBehaviour
{
    public Transform previewSpawnPoint;
    private GameObject activeModel;
    [SerializeField] private Button closeButton;

    private void OnEnable()
    {
        closeButton.onClick.AddListener(OnFinishDisplay);
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveAllListeners();
    }

    public void Display(PlayerData.InventoryEntry entry)
    {
        GachaManager.Instance.ShowMachineInfo(false);
        GachaManager.Instance.ForceEnableSwipe(false);
        GachaManager.Instance.ChangeFOVCamera(true);
        closeButton.gameObject.SetActive(true);
        if (entry.prefab == null)
        {
            Debug.LogWarning("No prefab linked for item: " + entry.itemName);
            return;
        }

        // Destroy current preview
        if (activeModel != null)
            Destroy(activeModel);

        // Instantiate new one
        activeModel = Instantiate(entry.prefab, previewSpawnPoint);

        Debug.Log("Previewing: " + entry.itemName);
    }

    public void OnFinishDisplay()
    {
        GachaManager.Instance.ShowMachineInfo(true);
        GachaManager.Instance.ForceEnableSwipe(true);
        GachaManager.Instance.ChangeFOVCamera(false);
        closeButton.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
    }
}
