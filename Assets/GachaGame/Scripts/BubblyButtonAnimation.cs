using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BubblyButtonAnimation : MonoBehaviour
{
    private Button button;

    [Header("Punch Settings")]
    [SerializeField] private float punchStrength = 0.15f;
    [SerializeField] private float punchDuration = 0.35f;
    [SerializeField] private int vibrato = 6;
    [SerializeField] private float elasticity = 0.8f;

    [Header("Timing")]
    [SerializeField] private float interval = 0.7f; 

    private Sequence punchSeq;

    private void Start()
    {
        PlayBubblyLoop();
    }

    public void PlayBubblyLoop()
    {
        punchSeq?.Kill();

        punchSeq = DOTween.Sequence();

        punchSeq
            .Append(this.transform.DOPunchScale(
                new Vector3(punchStrength, punchStrength, 0),
                punchDuration,
                vibrato,
                elasticity
            ))
            .AppendInterval(interval)     
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.OutQuad);
    }
}
