using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public class NavigationButtonGroup : MonoBehaviour
{
    [Header("Buttons (order matters)")]
    public List<Button> buttons = new List<Button>();

    [Header("Tween Settings")]
    public float liftAmount = 20f;
    public float duration = 0.25f;
    public Ease ease = Ease.OutBack;

    [Header("Events")]
    public UnityEvent<int> OnSelectedIndexChanged;

    // internals
    private List<RectTransform> rects = new List<RectTransform>();
    private Dictionary<RectTransform, Vector3> originalLocalPos = new Dictionary<RectTransform, Vector3>();
    private int selectedIndex = -1;

    void Start()
    {
        // build rect list and listeners first
        rects.Clear();
        originalLocalPos.Clear();

        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;

            RectTransform rt = btn.transform as RectTransform;
            rects.Add(rt);

            // safe listener registration
            int idx = i;
            btn.onClick.AddListener(() => SelectIndex(idx));
        }

        // cache the CURRENT positions (important if layout or other logic ran before Start)
        CacheCurrentPositions();

        // optional: select first by snapping instantly (comment out if undesired)
        if (rects.Count > 0)
            SelectIndex(1, true);
    }

    /// <summary>
    /// Cache the current localPosition of each RectTransform into originalLocalPos.
    /// Call this if the UI layout changes at runtime and you want to re-base the 'original' positions.
    /// </summary>
    public void CacheCurrentPositions()
    {
        originalLocalPos.Clear();

        for (int i = 0; i < rects.Count; i++)
        {
            RectTransform rt = rects[i];
            if (rt == null) continue;

            // store the actual current localPosition as the "original" baseline
            originalLocalPos[rt] = rt.localPosition;
        }
    }

    /// <summary>
    /// Public wrapper so other scripts can request a re-cache.
    /// </summary>
    public void RecachePositions()
    {
        CacheCurrentPositions();
    }

    /// <summary> Select index. If instant==true it snaps instead of tweening. </summary>
    public void SelectIndex(int index, bool instant = false)
    {
        if (index < 0 || index >= rects.Count) return;

        // if already selected, do nothing
        if (selectedIndex == index) return;

        // reset all buttons back to their own original local Y using cached positions
        for (int i = 0; i < rects.Count; i++)
        {
            RectTransform rt = rects[i];
            if (rt == null) continue;

            // kill any running tweens on this transform
            rt.DOKill();

            // ensure we have an entry (safety)
            if (!originalLocalPos.ContainsKey(rt))
                originalLocalPos[rt] = rt.localPosition;

            Vector3 orig = originalLocalPos[rt];
            Vector3 target = new Vector3(rt.localPosition.x, orig.y, rt.localPosition.z);

            if (instant)
                rt.localPosition = target;
            else
                rt.DOLocalMove(target, duration).SetEase(ease);
        }

        // lift the selected one (only modify Y)
        RectTransform selected = rects[index];
        if (selected != null)
        {
            selected.DOKill();

            // ensure we have an entry for the selected as well
            if (!originalLocalPos.ContainsKey(selected))
                originalLocalPos[selected] = selected.localPosition;

            Vector3 orig = originalLocalPos[selected];
            Vector3 lifted = new Vector3(selected.localPosition.x, orig.y + liftAmount, selected.localPosition.z);

            if (instant)
                selected.localPosition = lifted;
            else
                selected.DOLocalMove(lifted, duration).SetEase(ease);
        }

        selectedIndex = index;
        OnSelectedIndexChanged?.Invoke(selectedIndex);
    }

    /// <summary>Public wrappers</summary>
    public void SelectNext(bool instant = false)
    {
        if (rects.Count == 0) return;
        int next = (selectedIndex + 1) % rects.Count;
        SelectIndex(next, instant);
    }

    public void SelectPrevious(bool instant = false)
    {
        if (rects.Count == 0) return;
        int prev = selectedIndex - 1;
        if (prev < 0) prev = rects.Count - 1;
        SelectIndex(prev, instant);
    }

    /// <summary>Reset everything instantly</summary>
    public void ResetAllInstant()
    {
        for (int i = 0; i < rects.Count; i++)
        {
            RectTransform rt = rects[i];
            if (rt == null) continue;
            rt.DOKill();

            // if cached, restore cached local pos; otherwise use current
            if (originalLocalPos.ContainsKey(rt))
                rt.localPosition = originalLocalPos[rt];
            else
                rt.localPosition = rt.localPosition;
        }
        selectedIndex = -1;
    }
}
