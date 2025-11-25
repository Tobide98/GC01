using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaMachineSelector : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI priceTagText;
    public TextMeshProUGUI priceTagTenText;
    public Image bannerImage;
    public TextMeshProUGUI PityLeftText;
    public GameObject gachaDropRateUI;
    public GameObject gachaHistoryUI;

    [Header("Modules")]
    [SerializeField] private GachaProbabilityInfo gachaProbabilityInfo;
    [SerializeField] private GachaHistoryInfo gachaHistoryInfo;

    [Header("Hierarchy")]
    public Transform machinesParent;
    public List<Transform> machines = new List<Transform>();

    [Header("Focus Movement")]
    public Transform focusPoint;
    public float forwardOffset = 0.3f;
    public Vector3 forwardDirection = new Vector3(0, 0, -1);
    public float moveSpeed = 5f;

    [Header("Swipe")]
    public float swipeThreshold = 50f;
    public bool swipeEnabled = true;

    [Header("UI Buttons (assign these)")]
    public Button nextButton;
    public Button prevButton;
    public Button dropRateInfoButton;
    public Button historyInfoButton;

    private int currentIndex = 0;
    private Vector2 swipeStartPos;
    private bool swiping = false;

    private Vector3 parentTargetPos;

    void Start()
    {
        if (machinesParent == null)
            machinesParent = transform;

        if (machines.Count == 0 || focusPoint == null)
        {
            Debug.LogWarning("Assign machines, machinesParent, and focusPoint.");
            enabled = false;
            return;
        }

        // --- AUTO BUTTON LISTENER SETUP ---
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonPressed);

        if (prevButton != null)
            prevButton.onClick.AddListener(OnPrevButtonPressed);

        if (dropRateInfoButton != null) 
            dropRateInfoButton.onClick.AddListener(ShowDropRateInfo);

        if (historyInfoButton != null)
            historyInfoButton.onClick.AddListener(ShowHistoryInfo);

        parentTargetPos = machinesParent.position;
        RecalculateParentTarget(true);
        UpdateMachineInfo();
    }

    void Update()
    {
        HandleSwipeInput();

        machinesParent.position = Vector3.Lerp(
            machinesParent.position,
            parentTargetPos,
            Time.deltaTime * moveSpeed
        );
    }

    void HandleSwipeInput()
    {
        if (!swipeEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPos = Input.mousePosition;
            swiping = true;
        }

        if (Input.GetMouseButtonUp(0) && swiping)
        {
            swiping = false;
            Vector2 delta = (Vector2)Input.mousePosition - swipeStartPos;

            if (Mathf.Abs(delta.x) > swipeThreshold &&
                Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                if (delta.x > 0f) SelectPrevious();
                else SelectNext();
            }
        }
    }

    // BUTTON FUNCTIONS YOU CAN CALL FROM UI OR FROM SCRIPT

    public void OnNextButtonPressed()
    {
        SelectNext();
    }

    public void OnPrevButtonPressed()
    {
        SelectPrevious();
    }

    void SelectNext()
    {
        currentIndex = (currentIndex + 1) % machines.Count;
        RecalculateParentTarget(false);
        UpdateMachineInfo();
    }

    void SelectPrevious()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = machines.Count - 1;

        RecalculateParentTarget(false);
        UpdateMachineInfo();
    }

    void RecalculateParentTarget(bool instant)
    {
        Transform selected = machines[currentIndex];

        Vector3 desiredPos =
            focusPoint.position + forwardDirection.normalized * forwardOffset;

        Vector3 delta = desiredPos - selected.position;

        parentTargetPos = machinesParent.position + delta;

        if (instant)
            machinesParent.position = parentTargetPos;
    }

    // SWIPE ENABLE / DISABLE
    public void EnableSwipe()
    {
        swipeEnabled = true;
        prevButton.gameObject.SetActive(swipeEnabled);
        nextButton.gameObject.SetActive(swipeEnabled);
    }

    public void DisableSwipe()
    {
        swipeEnabled = false;
        swiping = false;
        prevButton.gameObject.SetActive(swipeEnabled);
        nextButton.gameObject.SetActive(swipeEnabled);
    }

    // GET CURRENT SELECTED MACHINE
    public GachaController GetCurrentSelectedMachine()
    {
        return machines[currentIndex].gameObject.GetComponent<GachaController>();
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    public void UpdateMachineInfo()
    {
        var machine = GetCurrentSelectedMachine();
        var database = machine.GetMachineDatabase();
        priceTagText.text = $"{machine.GetGachaPrice():N0}";
        priceTagTenText.text = $"{machine.GetGachaPriceTen():N0}";
        bannerImage.sprite = database.bannerImage;
        int pityLeft = machine.GetURPityLeft();
        PityLeftText.text = $"<color=#FFA500>{pityLeft}</color> pulls left until";
        gachaProbabilityInfo.SetDatabse(database);
        gachaHistoryInfo.SetDatabse(database);

    }

    void ShowDropRateInfo()
    {
        gachaDropRateUI.SetActive(true);
    }

    void ShowHistoryInfo()
    {
        gachaHistoryUI.SetActive(true);
    }
}
