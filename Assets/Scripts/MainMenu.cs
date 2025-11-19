using UnityEngine;
using DG.Tweening;

public class MainMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float scaleDuration = 0.45f;
    [SerializeField] private float scaleUpAmount = 1.15f; // scale up to 115%

    private Vector3 startingScale;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        startingScale = transform.localScale;
    }

    public void StartGame()
    {
        // Kill previous tweens
        transform.DOKill();
        canvasGroup.DOKill();

        Sequence seq = DOTween.Sequence();

        seq.Append(canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(startingScale * scaleUpAmount, scaleDuration).SetEase(Ease.OutBack));

        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            transform.localScale = startingScale; // reset for next time
        });
    }
}

