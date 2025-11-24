using DG.Tweening;
using Gravitons.UI.Modal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject machineStartInfo;
    [SerializeField] private Button startButton;
    [SerializeField] private MainMenu mainMenu;
    private Tween fadeTween;
    private Tween scaleTween;

    [Header("Module Reference")]
    [SerializeField] private GachaMachineSelector gachaMachineSelector;
    [SerializeField] private PlayerScript playerScirpt;
    [SerializeField] private ModalManager modalManager;
    [SerializeField] private GachaReward gachaRewardScript;
    [SerializeField] private PlayerScript playerScript;

    private GachaController currGachaMachine;

    public static GachaManager Instance;

    private float normalFOV = 80f;
    private float focusFOV = 65f;
    private float duration = 0.3f;
    private Coroutine fovRoutine;

    // --- Gacha mode (single vs 10-pull) ---
    private bool isTenPull = false;

    private void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {
        mainMenu.gameObject.SetActive(true);
        startButton.onClick.AddListener(StartGacha);
        gachaRewardScript.OnRewardClosed += HandleRewardClosed;
    }

    public void StartGacha()
    {
        ModalManager.Show(
            "Start Gacha",
            "Are you sure you want to pick this gacha?",
            new[]
            {
                new ModalButton() { Text = "NO"},
                new ModalButton() { Text = "YES", Callback = CheckTokenBalance}
            });
    }

    // 10-pull entry point (hook this to a 10x button)
    public void StartGacha10()
    {
        ModalManager.Show(
            "Start 10x Gacha",
            "Are you sure you want to do 10 pulls on this gacha?",
            new[]
            {
                new ModalButton() { Text = "NO"},
                new ModalButton() { Text = "YES", Callback = CheckTokenBalance10}
            });
    }

    public void CheckTokenBalance()
    {
        var gachaMachine = gachaMachineSelector.GetCurrentSelectedMachine();
        if (playerScirpt.CheckIsSufficientCoin(gachaMachine.GetGachaPrice()))
        {
            currGachaMachine = gachaMachine;
            isTenPull = false; // single pull mode
            playerScirpt.ReducePlayerCoin(currGachaMachine.GetGachaPrice());
            SelectGacha(currGachaMachine);
            currGachaMachine.OnMaxTurnsReached.AddListener(OnMaxTurns);
        }
        else
        {
            ModalManager.Show(
                "Insufficient Coin",
                "Your coin is insufficient please recharge your coin first before selecting this gacha.",
                new[] { new ModalButton() { Text = "OK" } });
        }
    }

    // NEW: Balance check for 10-pull (cost = price * 10)
    private void CheckTokenBalance10()
    {
        var gachaMachine = gachaMachineSelector.GetCurrentSelectedMachine();
        int totalPrice = gachaMachine.GetGachaPrice() * 10;

        if (playerScirpt.CheckIsSufficientCoin(totalPrice))
        {
            currGachaMachine = gachaMachine;
            isTenPull = true; // 10-pull mode
            playerScirpt.ReducePlayerCoin(totalPrice);
            SelectGacha(currGachaMachine);
            currGachaMachine.OnMaxTurnsReached.AddListener(OnMaxTurns);
        }
        else
        {
            ModalManager.Show(
                "Insufficient Coin",
                "Your coin is insufficient for 10 pulls. Please recharge your coin first.",
                new[] { new ModalButton() { Text = "OK" } });
        }
    }

    private void OnMaxTurns()
    {
        // Single pull → 1 roll
        if (!isTenPull)
        {
            var reward = gachaRewardScript.Roll(currGachaMachine.GetMachineDatabase());
            gachaRewardScript.SetReward(reward);
            gachaRewardScript.ShowReward();
        }
        else
        {
            // 10-pull → 10 rolls; show the last one in the existing reward UI
            GachaReward.RewardResult lastReward = default;

            for (int i = 0; i < 10; i++)
            {
                lastReward = gachaRewardScript.Roll(currGachaMachine.GetMachineDatabase());
            }

            gachaRewardScript.SetReward(lastReward);
            gachaRewardScript.ShowReward();

            // reset mode back to single after finishing this 10-pull
            isTenPull = false;
        }

        currGachaMachine.isAvaliable = false;
    }

    public void SelectGacha(GachaController gachaMachine)
    {
        StartMachineCouroutine();
        gachaMachineSelector.DisableSwipe();
        gachaMachineSelector.UpdateMachineInfo();
        ShowMachineInfo(false);
    }

    public void StartMachineCouroutine()
    {
        StartFOVRoutine(normalFOV, focusFOV, duration);
        StartCoroutine(StartMachine());
    }
    void HandleRewardClosed()
    {
        OnFinishGacha();
    }

    public void OnFinishGacha()
    {
        gachaMachineSelector.EnableSwipe();
        gachaMachineSelector.UpdateMachineInfo();
        ResetMachine();
        currGachaMachine.OnMaxTurnsReached.RemoveListener(OnMaxTurns);
    }

    public void ResetMachine()
    {
        StartFOVRoutine(focusFOV, normalFOV, duration);
        ShowMachineInfo(true);
    }

    // --- Camera Focus ---
    void StartFOVRoutine(float from, float to, float time)
    {
        if (fovRoutine != null)
            StopCoroutine(fovRoutine);

        fovRoutine = StartCoroutine(FOVRoutine(from, to, time));
    }

    IEnumerator FOVRoutine(float from, float to, float time)
    {
        Camera cam = Camera.main;
        if (cam == null)
            yield break;

        float t = 0f;
        cam.fieldOfView = from;

        while (t < time)
        {
            t += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(from, to, t / time);
            yield return null;
        }

        cam.fieldOfView = to;
    }

    IEnumerator StartMachine()
    {
        currGachaMachine.GetMachineAnim().SetTrigger("Start");
        yield return new WaitForSeconds(1.5f);
        currGachaMachine.spinUI.SetActive(true);
        currGachaMachine.isAvaliable = true;
    }

    public void ShowMachineInfo(bool fadeIn)
    {
        var canvasGroup = machineStartInfo.GetComponent<CanvasGroup>();
        fadeTween?.Kill();
        scaleTween?.Kill();
        if (fadeIn)
        {
            // Make sure the object is active
            machineStartInfo.SetActive(true);

            // Prepare for fade in
            canvasGroup.alpha = 0f;
            machineStartInfo.transform.localScale = Vector3.one * 0.7f;

            fadeTween = canvasGroup.DOFade(1f, 0.3f);
            scaleTween = machineStartInfo.transform
                .DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack, 1.2f);
        }
        else
        {
            // Prepare for fade out
            canvasGroup.alpha = 1f;
            machineStartInfo.transform.localScale = Vector3.one * 1f;

            fadeTween = canvasGroup
                .DOFade(0f, 0.3f);

            scaleTween = machineStartInfo.transform
                .DOScale(1.2f, 0.3f)
                .SetEase(Ease.InBack, 1.2f)
                .OnComplete(() =>
                {
                    // Deactivate object only after animation is fully done
                    machineStartInfo.SetActive(false);
                });
        }
    }

    public PlayerScript GetPlayerData()
    {
        return playerScirpt;
    }
}
