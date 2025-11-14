using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GachaMachineSelector : MonoBehaviour
{
    [Header("")]
    public TextMeshProUGUI priceTagText;

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

        parentTargetPos = machinesParent.position;
        RecalculateParentTarget(true);
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

    void SelectNext()
    {
        currentIndex = (currentIndex + 1) % machines.Count;
        RecalculateParentTarget(false);
        UpdatePriceTag();
    }

    void SelectPrevious()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = machines.Count - 1;

        RecalculateParentTarget(false);
        UpdatePriceTag();
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

    // ---------------------------------------------------
    // SWIPE ENABLE / DISABLE
    // ---------------------------------------------------
    public void EnableSwipe() => swipeEnabled = true;

    public void DisableSwipe()
    {
        swipeEnabled = false;
        swiping = false;
    }

    // ---------------------------------------------------
    // GET CURRENT SELECTED MACHINE
    // ---------------------------------------------------
    public GachaController GetCurrentSelectedMachine()
    {
        return machines[currentIndex].gameObject.GetComponent<GachaController>();
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    void UpdatePriceTag()
    {
        priceTagText.text = $"x{GetCurrentSelectedMachine().GetGachaPrice().ToString("F0")}";
    }
}
