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
    [SerializeField] private Button startTenButton;
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
        startTenButton.onClick.AddListener(StartGacha10);
        gachaRewardScript.OnRewardClosed += HandleRewardClosed;
        currGachaMachine = gachaMachineSelector.GetCurrentSelectedMachine();
    }

    public void StartGacha()
    {
        ModalManager.Show(
            "Start Gacha",
            $"Are you sure you want to pick this gacha for <color=#FF2A00>{currGachaMachine.GetGachaPrice()}</color> tokens?",
            new[]
            {
                new ModalButton() { Text = "NO"},
                new ModalButton() { Text = "YES", Callback = CheckTokenBalance}
            });
    }

    // ---- 10 Pull Entry ----
    public void StartGacha10()
    {
        ModalManager.Show(
            "Start 10x Gacha",
            $"Are you sure you want to do 10 pulls on this gacha for <color=#FF2A00>{currGachaMachine.GetGachaPriceTen()}</color> tokens?",
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
            isTenPull = false;
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

    // Updated: 10 Pull Price + Mode Switch
    private void CheckTokenBalance10()
    {
        var gachaMachine = gachaMachineSelector.GetCurrentSelectedMachine();

        // Uses GetGachaPriceTen() from your script
        int totalPrice = gachaMachine.GetGachaPriceTen();

        if (playerScirpt.CheckIsSufficientCoin(totalPrice))
        {
            currGachaMachine = gachaMachine;
            isTenPull = true;
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
        // ---- SINGLE PULL ----
        if (!isTenPull)
        {
            var reward = gachaRewardScript.Roll(currGachaMachine.GetMachineDatabase());
            gachaRewardScript.SetReward(reward);
            gachaRewardScript.ShowReward();
        }
        else
        {
            // ---- 10 PULL ----
            List<GachaReward.RewardResult> results = gachaRewardScript.Roll10(currGachaMachine.GetMachineDatabase());

            gachaRewardScript.SetReward(results); // now sends full reward list
            gachaRewardScript.ShowReward();

            isTenPull = false; // reset mode
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

    void StartFOVRoutine(float from, float to, float time)
    {
        if (fovRoutine != null)
            StopCoroutine(fovRoutine);

        fovRoutine = StartCoroutine(FOVRoutine(from, to, time));
    }

    IEnumerator FOVRoutine(float from, float to, float time)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

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
            machineStartInfo.SetActive(true);
            canvasGroup.alpha = 0f;
            machineStartInfo.transform.localScale = Vector3.one * 0.7f;

            fadeTween = canvasGroup.DOFade(1f, 0.3f);
            scaleTween = machineStartInfo.transform
                .DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack, 1.2f);
        }
        else
        {
            fadeTween = canvasGroup.DOFade(0f, 0.3f);
            scaleTween = machineStartInfo.transform
                .DOScale(1.2f, 0.3f)
                .SetEase(Ease.InBack, 1.2f)
                .OnComplete(() => machineStartInfo.SetActive(false));
        }
    }

    public PlayerScript GetPlayerData()
    {
        return playerScirpt;
    }

    public void ForceEnableSwipe(bool enable)
    {
        if (enable)
        {
            gachaMachineSelector.EnableSwipe();
        }
        else
        {
            gachaMachineSelector.DisableSwipe();
        }
    }

    public void ChangeFOVCamera(bool isZoom)
    {
        if (isZoom)
        {
            StartFOVRoutine(normalFOV, focusFOV, duration);
        }
        else
        {
            StartFOVRoutine(focusFOV, normalFOV, duration);
        }
    }
}
