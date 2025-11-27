using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UINavigationScript : MonoBehaviour
{
    [Header("Menu References (assign key + GameObject)")]
    [Tooltip("List of menus. Key is the menu name used by OpenMenu(string).")]
    [SerializeField] private List<MenuEntry> menuEntries = new List<MenuEntry>();

    [Header("Navigation Buttons (optional)")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button storeButton;
    [SerializeField] private Button inspectButton;
    [SerializeField] private Button topupButton;

    // Runtime dictionary for fast lookup (key -> GameObject)
    private Dictionary<string, GameObject> menus = new Dictionary<string, GameObject>();

    [Header("Menu Navigation")]
    [SerializeField] private Menu currSelectedMenu;
    [SerializeField] private NavigationButtonGroup navButtonGroup;

    public enum Menu
    {
        Home,
        Store,
        Inspect
    }

    [System.Serializable]
    public class MenuEntry
    {
        [Tooltip("Unique menu name used by OpenMenu(string).")]
        public string key;
        [Tooltip("The GameObject that represents the menu (assign root GameObject).")]
        public GameObject menuObject;
    }

    private void Awake()
    {
        BuildMenuDictionary();

        // Optional: wire buttons if assigned (you can still call OpenMenu manually)
    }

    private void Start()
    {
        // Ensure there's a valid starting menu (use enum)
        if (homeButton != null) homeButton.onClick.AddListener(() => OpenMenu(Menu.Home));
        if (storeButton != null) storeButton.onClick.AddListener(() => OpenMenu(Menu.Store));
        if (inspectButton != null) inspectButton.onClick.AddListener(() => OpenMenu(Menu.Inspect));
        if (topupButton != null) topupButton.onClick.AddListener(() => TopUpButton());
        OpenMenu(currSelectedMenu);
    }

    /// <summary>
    /// (Re)builds the runtime dictionary from the serialized list.
    /// Call this if you change the menuEntries at runtime.
    /// </summary>
    public void BuildMenuDictionary()
    {
        menus.Clear();

        foreach (var entry in menuEntries)
        {
            if (entry == null || entry.menuObject == null || string.IsNullOrEmpty(entry.key))
                continue;

            string key = entry.key.Trim();

            if (menus.ContainsKey(key.ToLower()))
            {
                Debug.LogWarning($"UINavigationScript: Duplicate menu key detected: '{key}'. Skipping duplicate.");
                continue;
            }

            menus.Add(key.ToLower(), entry.menuObject);
        }
    }

    /// <summary>
    /// Open menu by string name (case-insensitive). Deactivates all menus then activates selected.
    /// </summary>
    public void OpenMenu(string menuName)
    {
        if (string.IsNullOrEmpty(menuName))
        {
            Debug.LogWarning("UINavigationScript.OpenMenu called with null/empty name.");
            return;
        }

        string key = menuName.Trim().ToLower();

        // deactivate all first
        foreach (var kv in menus)
        {
            if (kv.Value != null)
                kv.Value.SetActive(false);
        }

        if (!menus.TryGetValue(key, out GameObject menuObj) || menuObj == null)
        {
            Debug.LogWarning($"UINavigationScript: Menu '{menuName}' not found in dictionary.");
            return;
        }

        menuObj.SetActive(true);
        PlayAnimation(menuObj);

        // keep enum in sync when possible
        if (System.Enum.TryParse(typeof(Menu), menuName, true, out var parsed))
        {
            currSelectedMenu = (Menu)parsed;
        }

        if (currSelectedMenu.Equals(Menu.Home))
        {
            GachaManager.Instance.ForceEnableSwipe(true);
        }
        else
        {
            GachaManager.Instance.ForceEnableSwipe(false);
        }
    }

    /// <summary>
    /// Convenience overload using the enum values (maps enum name to string).
    /// </summary>
    public void OpenMenu(Menu menu)
    {
        OpenMenu(menu.ToString());
    }

    /// <summary>
    /// Deactivate all menus.
    /// </summary>
    public void CloseAllMenus()
    {
        foreach (var kv in menus)
            if (kv.Value != null) kv.Value.SetActive(false);
    }

    /// <summary>
    /// Get the currently active menu GameObject (if any).
    /// </summary>
    public GameObject GetActiveMenu()
    {
        foreach (var kv in menus)
        {
            if (kv.Value != null && kv.Value.activeSelf)
                return kv.Value;
        }
        return null;
    }

    public void TopUpButton()
    {
        OpenMenu(Menu.Store);
        navButtonGroup.SelectIndex(0);
        storeButton.Select();
    }

    void PlayAnimation(GameObject mennuObject)
    {
        var canvasGroup = mennuObject.GetComponent<CanvasGroup>();
        canvasGroup.DOKill();
        mennuObject.transform.DOKill();

        canvasGroup.alpha = 0f;
        mennuObject.transform.localScale = Vector3.one * 0.7f;

        canvasGroup.DOFade(1f, 0.3f);
        mennuObject.transform
                .DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack, 1.2f);
    }
}
