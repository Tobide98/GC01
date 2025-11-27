using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GachaMachineSelector : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI priceTagText;
    public TextMeshProUGUI priceTagTenText;
    public Image bannerImage;
    public TextMeshProUGUI PityLeftText;
    public GameObject gachaDropRateUI;
    public GameObject gachaHistoryUI;

    [Header("Home Background")]
    public Image scrollingBg;
    public Image staticBg;

    [Header("Banner Animation (optional)")]
    public CanvasGroup gachaBannerCanvasGroup;
    public float bannerMoveOffset = 30f;
    public float bannerFadeHalfDuration = 0.5f;
    public float bannerMoveEaseDuration = 0.5f;

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

    // Banner internals
    public RectTransform bannerRect;
    private Vector3 bannerOriginalLocalPos;
    private bool lastWasNext = true; // true = last move was Next, false = Prev

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

        // Try auto-find CanvasGroup if not assigned
        if (gachaBannerCanvasGroup == null && bannerImage != null)
            gachaBannerCanvasGroup = bannerImage.GetComponent<CanvasGroup>();

        // Cache rect transform & original pos (safe)
        if (bannerRect == null && bannerImage != null)
            bannerRect = bannerImage.rectTransform;

        bannerOriginalLocalPos = bannerRect != null ? bannerRect.localPosition : Vector3.zero;

        parentTargetPos = machinesParent.position;
        RecalculateParentTarget(true);
        UpdateMachineInfoInstant(); // show initial machine info immediately
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
        lastWasNext = true;
        UpdateMachineInfoWithBanner();
    }

    void SelectPrevious()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = machines.Count - 1;

        RecalculateParentTarget(false);
        lastWasNext = false;
        UpdateMachineInfoWithBanner();
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
        if (prevButton != null) prevButton.gameObject.SetActive(swipeEnabled);
        if (nextButton != null) nextButton.gameObject.SetActive(swipeEnabled);
    }

    public void DisableSwipe()
    {
        swipeEnabled = false;
        swiping = false;
        if (prevButton != null) prevButton.gameObject.SetActive(swipeEnabled);
        if (nextButton != null) nextButton.gameObject.SetActive(swipeEnabled);
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

    // -----------------------
    // MACHINE INFO UPDATES
    // -----------------------
    public void UpdateMachineInfoInstant()
    {
        var machine = GetCurrentSelectedMachine();
        if (machine == null) return;

        var database = machine.GetMachineDatabase();
        if (priceTagText != null) priceTagText.text = $"{machine.GetGachaPrice():N0}";
        if (priceTagTenText != null) priceTagTenText.text = $"{machine.GetGachaPriceTen():N0}";
        if (bannerImage != null && database != null) bannerImage.sprite = database.bannerImage;

        int pityLeft = machine.GetURPityLeft();
        if (PityLeftText != null) PityLeftText.text = $"<color=#FFA500>{pityLeft}</color> pulls left until";

        if (gachaProbabilityInfo != null && database != null) gachaProbabilityInfo.SetDatabse(database);
        if (gachaHistoryInfo != null && database != null) gachaHistoryInfo.SetDatabse(database);
    }

    /// <summary>
    /// Update machine info but animate the banner fading out/in while sliding left/right depending on direction.
    /// Sequence (NEXT):
    ///  - fadeOut + move normal -> LEFT
    ///  - callback: update sprite/text & instantly put banner at RIGHT
    ///  - fadeIn  + move RIGHT -> normal
    /// Sequence (PREV) is mirrored (RIGHT then LEFT -> normal)
    /// </summary>
    public void UpdateMachineInfoWithBanner()
    {
        // Fallback to instant if no banner components
        if (gachaBannerCanvasGroup == null || bannerRect == null)
        {
            UpdateMachineInfoInstant();
            return;
        }

        // Kill any overlapping tweens
        gachaBannerCanvasGroup.DOKill();
        bannerRect.DOKill();

        float half = Mathf.Max(0.01f, bannerFadeHalfDuration);
        float moveDur = Mathf.Max(0.01f, bannerMoveEaseDuration);
        float offset = Mathf.Abs(bannerMoveOffset);

        // For NEXT: outOffset = -offset (left), inOffset = +offset (right)
        // For PREV: outOffset = +offset (right), inOffset = -offset (left)
        float outOffset = lastWasNext ? -offset : offset;
        float inOffset = -outOffset;

        Vector3 outPos = bannerOriginalLocalPos + new Vector3(outOffset, 0f, 0f);
        Vector3 inStartPos = bannerOriginalLocalPos + new Vector3(inOffset, 0f, 0f);

        Sequence seq = DOTween.Sequence();

        // fade out while moving from normal -> outPos
        seq.Append(gachaBannerCanvasGroup.DOFade(0f, half).SetEase(Ease.InOutQuad));
        seq.Join(bannerRect.DOLocalMove(outPos, moveDur).SetEase(Ease.InOutQuad));

        // after faded out and moved, update content and snap to opposite side (inStartPos)
        seq.AppendCallback(() =>
        {
            var machine = GetCurrentSelectedMachine();
            if (machine == null) return;

            var database = machine.GetMachineDatabase();
            if (priceTagText != null) priceTagText.text = $"{machine.GetGachaPrice():N0}";
            if (priceTagTenText != null) priceTagTenText.text = $"{machine.GetGachaPriceTen():N0}";
            if (bannerImage != null && database != null) bannerImage.sprite = database.bannerImage;

            int pityLeft = machine.GetURPityLeft();
            if (PityLeftText != null) PityLeftText.text = $"<color=#FFA500>{pityLeft}</color> pulls left until";

            if (gachaProbabilityInfo != null && database != null) gachaProbabilityInfo.SetDatabse(database);
            if (gachaHistoryInfo != null && database != null) gachaHistoryInfo.SetDatabse(database);

            // instantly place banner at opposite side so the fade-in movement goes towards center
            bannerRect.localPosition = inStartPos;

            SetBackgroundColor(database.machineColor);
        });

        // fade in while moving from inStartPos -> normal
        seq.Append(gachaBannerCanvasGroup.DOFade(1f, half).SetEase(Ease.InOutQuad));
        seq.Join(bannerRect.DOLocalMove(bannerOriginalLocalPos, moveDur).SetEase(Ease.InOutQuad));

        seq.Play();
    }

    void ShowDropRateInfo()
    {
        if (gachaDropRateUI != null) gachaDropRateUI.SetActive(true);
    }

    void ShowHistoryInfo()
    {
        if (gachaHistoryUI != null) gachaHistoryUI.SetActive(true);
    }

    void SetBackgroundColor(Color targetColor)
    {
        if (scrollingBg != null)
        {
            scrollingBg.DOKill(); // stop previous tweens safely
            scrollingBg.DOColor(targetColor, 0.4f)   // ← change duration as needed
                        .SetEase(Ease.OutQuad);
        }

        if (staticBg != null)
        {
            staticBg.DOKill();
            staticBg.DOColor(targetColor, 0.4f)
                    .SetEase(Ease.OutQuad);
        }
    }

}
