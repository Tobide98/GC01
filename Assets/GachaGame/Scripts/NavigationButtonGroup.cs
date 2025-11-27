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
        rects.Clear();
        originalLocalPos.Clear();

        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;

            RectTransform rt = btn.transform as RectTransform;
            rects.Add(rt);

            int idx = i;
            btn.onClick.AddListener(() => SelectIndex(idx));
        }

        CacheCurrentPositions();

        if (rects.Count > 0)
            SelectIndex(1, true);
    }

    public void CacheCurrentPositions()
    {
        originalLocalPos.Clear();

        for (int i = 0; i < rects.Count; i++)
        {
            RectTransform rt = rects[i];
            if (rt == null) continue;
            originalLocalPos[rt] = rt.localPosition;
        }
    }

    public void RecachePositions()
    {
        CacheCurrentPositions();
    }

    public void SelectIndex(int index, bool instant = false)
    {
        if (index < 0 || index >= rects.Count) return;

        // if already selected, do nothing
        if (selectedIndex == index) return;

        // --- ENABLE/DISABLE BUTTONS ---
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                // Enable all, disable selected
                buttons[i].interactable = (i != index);
            }
        }

        // --- Reset all positions ---
        for (int i = 0; i < rects.Count; i++)
        {
            var rt = rects[i];
            if (rt == null) continue;

            rt.DOKill();
            Vector3 orig = originalLocalPos[rt];
            Vector3 target = new Vector3(rt.localPosition.x, orig.y, rt.localPosition.z);

            if (instant)
                rt.localPosition = target;
            else
                rt.DOLocalMove(target, duration).SetEase(ease);
        }

        // --- Lift selected ---
        RectTransform selected = rects[index];
        if (selected != null)
        {
            selected.DOKill();

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

    public void ResetAllInstant()
    {
        for (int i = 0; i < rects.Count; i++)
        {
            RectTransform rt = rects[i];
            if (rt == null) continue;

            rt.DOKill();
            rt.localPosition = originalLocalPos.ContainsKey(rt)
                ? originalLocalPos[rt]
                : rt.localPosition;

            if (buttons[i] != null)
                buttons[i].interactable = true;
        }

        selectedIndex = -1;
    }
}
