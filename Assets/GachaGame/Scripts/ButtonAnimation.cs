using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class ButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private RectTransform target;

    [Header("Idle Bubble Settings")]
    [SerializeField] bool isUsingIdle = false;
    [SerializeField] private float idlePunchStrength = 0.15f;
    [SerializeField] private float idlePunchDuration = 1f;
    [SerializeField] private int idleVibrato = 2;
    [SerializeField] private float idleElasticity = 0.5f;
    [SerializeField] private float idleInterval = 1f;

    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverDuration = 0.15f;

    [Header("Click Settings")]
    [SerializeField] private float clickPunch = 0.2f;
    [SerializeField] private float clickDuration = 0.20f;

    private Sequence idleSeq;
    private Vector3 baseScale;

    private void Awake()
    {
        if (target == null) target = GetComponent<RectTransform>();
        baseScale = target.localScale;
    }

    private void Start()
    {
        if (isUsingIdle)
        {
            PlayIdleLoop();
        }
    }

    private void PlayIdleLoop()
    {
        if (isUsingIdle)
        {
            idleSeq?.Kill();
            idleSeq = DOTween.Sequence();

            idleSeq.Append(target.DOPunchScale(
                new Vector3(idlePunchStrength, idlePunchStrength, 0),
                idlePunchDuration,
                idleVibrato,
                idleElasticity
            ));
            idleSeq.AppendInterval(idleInterval);
            idleSeq.SetLoops(-1, LoopType.Restart);
        }
    }

    private void StopIdleLoop()
    {
        idleSeq?.Kill();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopIdleLoop();

        target.DOKill();
        target.DOScale(baseScale * hoverScale, hoverDuration)
              .SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        target.DOKill();
        target.DOScale(baseScale, hoverDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(PlayIdleLoop);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        target.DOKill();
        target.DOPunchScale(
            new Vector3(clickPunch, clickPunch, 0),
            clickDuration,
            8,
            0.9f
        );
    }
}
